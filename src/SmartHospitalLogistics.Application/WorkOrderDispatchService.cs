using SmartHospitalLogistics.Domain;

namespace SmartHospitalLogistics.Application;

public interface IWorkOrderDispatchService
{
    DispatchBoard GetDispatchBoard();

    WorkOrderDetail? GetWorkOrderDetail(string workOrderNo);

    DispatchOperationResult Dispatch(string workOrderNo, DispatchWorkOrderCommand command);

    DispatchOperationResult Transition(string workOrderNo, TransitionWorkOrderCommand command);
}

public sealed class WorkOrderDispatchService : IWorkOrderDispatchService
{
    private static readonly DateTimeOffset SeedTime = new(2026, 5, 30, 9, 30, 0, TimeSpan.FromHours(8));
    private readonly object _sync = new();
    private readonly List<WorkOrder> _workOrders;
    private readonly List<TeamLoad> _teamLoads;
    private readonly Dictionary<string, List<WorkOrderTimelineEntry>> _timeline;

    public WorkOrderDispatchService()
    {
        var outpatientLobby = new SpatialLocation("同仁亦庄院区", "门诊医技楼", "F1", "共享大厅", "BIM-OPD-F1-LOBBY");
        var inpatientWard = new SpatialLocation("同仁亦庄院区", "住院楼", "F8", "眼科病区", "BIM-IPD-F8-WARD");
        var energyRoom = new SpatialLocation("同仁亦庄院区", "能源中心", "B1", "冷站机房", "BIM-ENE-B1-CHILLER");
        var wasteRoom = new SpatialLocation("同仁亦庄院区", "后勤楼", "F1", "医废暂存间", "BIM-LOG-F1-WASTE");

        _workOrders =
        [
            new WorkOrder("WO-20260530-0001", "医废暂存间负压异常处置", "环境应急", Priority.Critical, WorkOrderStatus.Escalated, wasteRoom, "未派工", SeedTime.AddMinutes(-42), SeedTime.AddMinutes(18)),
            new WorkOrder("WO-20260530-0002", "门诊大厅医梯运行异响巡检", "设备维修", Priority.High, WorkOrderStatus.Dispatched, outpatientLobby, "电梯维保组", SeedTime.AddMinutes(-26), SeedTime.AddHours(2)),
            new WorkOrder("WO-20260530-0003", "眼科病区被服补给", "后勤配送", Priority.Normal, WorkOrderStatus.Accepted, inpatientWard, "被服配送组", SeedTime.AddMinutes(-18), SeedTime.AddHours(3)),
            new WorkOrder("WO-20260530-0004", "冷站机房夜间节能策略复核", "能耗优化", Priority.Normal, WorkOrderStatus.PendingAcceptance, energyRoom, "能源管理组", SeedTime.AddHours(-4), SeedTime.AddHours(4))
        ];

        _teamLoads =
        [
            new TeamLoad("环境监管班组", "环境监管", 11, 12, "医废负压告警优先派工"),
            new TeamLoad("综合维修班", "一站式服务", 18, 24, "可承接一般维修与巡检工单"),
            new TeamLoad("电梯维保组", "设备设施", 6, 8, "保留困人事件应急余量"),
            new TeamLoad("能源管理组", "基础运行", 9, 16, "适合承接夜间节能策略复核")
        ];

        _timeline = _workOrders.ToDictionary(
            order => order.WorkOrderNo,
            order => new List<WorkOrderTimelineEntry>
            {
                new(order.CreatedAt, "系统", "创建", WorkOrderStatus.New, order.Status, "来自一站式服务或监测告警的模拟工单")
            });
    }

    public DispatchBoard GetDispatchBoard()
    {
        lock (_sync)
        {
            var ordered = _workOrders
                .Where(order => order.Status is not WorkOrderStatus.Closed)
                .OrderByDescending(order => order.Priority)
                .ThenBy(order => SlaMinutesRemaining(order))
                .ToArray();

            return new DispatchBoard(
                ordered,
                _teamLoads.ToArray(),
                ordered.Select(BuildRecommendation).ToArray(),
                BuildSlaRisk(ordered));
        }
    }

    public WorkOrderDetail? GetWorkOrderDetail(string workOrderNo)
    {
        lock (_sync)
        {
            var order = Find(workOrderNo);
            return order is null ? null : BuildDetail(order);
        }
    }

    public DispatchOperationResult Dispatch(string workOrderNo, DispatchWorkOrderCommand command)
    {
        lock (_sync)
        {
            var index = FindIndex(workOrderNo);
            if (index < 0)
            {
                return new DispatchOperationResult(false, $"工单 {workOrderNo} 不存在", null, NotFound: true);
            }

            var current = _workOrders[index];
            if (current.Status is WorkOrderStatus.Closed or WorkOrderStatus.PendingAcceptance)
            {
                return new DispatchOperationResult(false, $"当前状态 {current.Status} 不允许派工", BuildDetail(current));
            }

            var updated = current with
            {
                Status = WorkOrderStatus.Dispatched,
                ResponsibleTeam = command.TeamName
            };
            _workOrders[index] = updated;
            AddTimeline(updated.WorkOrderNo, command.Dispatcher, "派工", current.Status, updated.Status, command.Remark);

            return new DispatchOperationResult(true, null, BuildDetail(updated));
        }
    }

    public DispatchOperationResult Transition(string workOrderNo, TransitionWorkOrderCommand command)
    {
        lock (_sync)
        {
            var index = FindIndex(workOrderNo);
            if (index < 0)
            {
                return new DispatchOperationResult(false, $"工单 {workOrderNo} 不存在", null, NotFound: true);
            }

            var current = _workOrders[index];
            if (!TryTransition(current, command, out var updated, out var actionName, out var errorMessage))
            {
                return new DispatchOperationResult(false, errorMessage, BuildDetail(current));
            }

            _workOrders[index] = updated;
            AddTimeline(updated.WorkOrderNo, command.Operator, actionName, current.Status, updated.Status, command.Remark, command.Rating);

            return new DispatchOperationResult(true, null, BuildDetail(updated));
        }
    }

    private static bool TryTransition(
        WorkOrder current,
        TransitionWorkOrderCommand command,
        out WorkOrder updated,
        out string actionName,
        out string errorMessage)
    {
        updated = current;
        errorMessage = string.Empty;

        (WorkOrderStatus Status, string ActionName)? next = command.Action switch
        {
            WorkOrderTransitionAction.Accept when current.Status is WorkOrderStatus.Dispatched or WorkOrderStatus.Suspended or WorkOrderStatus.Transferred
                => (WorkOrderStatus.Accepted, "接单"),
            WorkOrderTransitionAction.Suspend when current.Status == WorkOrderStatus.Accepted
                => (WorkOrderStatus.Suspended, "挂单"),
            WorkOrderTransitionAction.Transfer when current.Status is WorkOrderStatus.Dispatched or WorkOrderStatus.Accepted or WorkOrderStatus.Suspended or WorkOrderStatus.Transferred
                => (WorkOrderStatus.Transferred, "转单"),
            WorkOrderTransitionAction.Complete when current.Status is WorkOrderStatus.Accepted or WorkOrderStatus.Transferred
                => (WorkOrderStatus.PendingAcceptance, "完工"),
            WorkOrderTransitionAction.AcceptCompletion when current.Status == WorkOrderStatus.PendingAcceptance
                => (WorkOrderStatus.Accepted, "验收"),
            WorkOrderTransitionAction.RejectCompletion when current.Status == WorkOrderStatus.PendingAcceptance
                => (WorkOrderStatus.Accepted, "驳回"),
            WorkOrderTransitionAction.Evaluate when current.Status is WorkOrderStatus.Accepted or WorkOrderStatus.PendingAcceptance
                => (WorkOrderStatus.Closed, "评价"),
            WorkOrderTransitionAction.Escalate when current.Status is not WorkOrderStatus.Closed
                => (WorkOrderStatus.Escalated, "升级"),
            _ => null
        };

        if (next is null)
        {
            actionName = command.Action.ToString();
            errorMessage = $"状态 {current.Status} 不允许执行 {command.Action}";
            return false;
        }

        actionName = next.Value.ActionName;
        updated = current with
        {
            Status = next.Value.Status,
            ResponsibleTeam = command.Action == WorkOrderTransitionAction.Transfer && !string.IsNullOrWhiteSpace(command.TargetTeam)
                ? command.TargetTeam
                : current.ResponsibleTeam
        };

        return true;
    }

    private WorkOrder? Find(string workOrderNo) =>
        _workOrders.FirstOrDefault(order => string.Equals(order.WorkOrderNo, workOrderNo, StringComparison.OrdinalIgnoreCase));

    private int FindIndex(string workOrderNo) =>
        _workOrders.FindIndex(order => string.Equals(order.WorkOrderNo, workOrderNo, StringComparison.OrdinalIgnoreCase));

    private WorkOrderDetail BuildDetail(WorkOrder order) =>
        new(
            order,
            order.Location,
            BuildSourceEvidence(order),
            _timeline[order.WorkOrderNo].ToArray(),
            SlaRiskLevel(order),
            SlaMinutesRemaining(order),
            AllowedActions(order).ToArray());

    private static FeatureEvidence[] BuildSourceEvidence(WorkOrder order)
    {
        var baseEvidence = new FeatureEvidence(
            "工单全流程管理",
            ["中科医信", "PPT"],
            "竞品明确报修、派单、接单、挂单、转单、完工、验收、评价；PPT要求一站式服务闭环。");

        if (order.ServiceType.Contains("环境", StringComparison.Ordinal))
        {
            return
            [
                baseEvidence,
                new FeatureEvidence("医疗废弃物管理", ["中科医信", "PPT"], "医废暂存、异常处置和环境监管闭环来自竞品功能树与PPT环境监管域。")
            ];
        }

        if (order.ServiceType.Contains("能耗", StringComparison.Ordinal))
        {
            return
            [
                baseEvidence,
                new FeatureEvidence("综合能耗监管", ["中科医信", "PPT"], "能耗告警、分析、报表和成本管理来自竞品能耗专项与PPT综合管理域。")
            ];
        }

        return [baseEvidence];
    }

    private static IEnumerable<string> AllowedActions(WorkOrder order) =>
        order.Status switch
        {
            WorkOrderStatus.New or WorkOrderStatus.Escalated => ["Dispatch"],
            WorkOrderStatus.Dispatched or WorkOrderStatus.Transferred => ["Accept", "Transfer", "Escalate"],
            WorkOrderStatus.Accepted => ["Suspend", "Transfer", "Complete", "Escalate"],
            WorkOrderStatus.Suspended => ["Accept", "Transfer", "Escalate"],
            WorkOrderStatus.PendingAcceptance => ["AcceptCompletion", "RejectCompletion", "Evaluate", "Escalate"],
            _ => []
        };

    private DispatchRecommendation BuildRecommendation(WorkOrder order)
    {
        var team = order.ServiceType switch
        {
            var value when value.Contains("环境", StringComparison.Ordinal) => "环境监管班组",
            var value when value.Contains("电梯", StringComparison.Ordinal) || order.Title.Contains("医梯", StringComparison.Ordinal) => "电梯维保组",
            var value when value.Contains("能耗", StringComparison.Ordinal) => "能源管理组",
            _ => "综合维修班"
        };

        return new DispatchRecommendation(
            order.WorkOrderNo,
            team,
            $"{order.ServiceType}按专业班组与SLA风险自动推荐",
            order.Priority,
            SlaMinutesRemaining(order));
    }

    private static SlaRiskSummary BuildSlaRisk(IReadOnlyList<WorkOrder> openOrders)
    {
        var highestRisk = openOrders
            .OrderByDescending(order => order.Priority)
            .ThenBy(order => SlaMinutesRemaining(order))
            .FirstOrDefault();

        return new SlaRiskSummary(
            openOrders.Count,
            openOrders.Count(order => SlaMinutesRemaining(order) < 0),
            openOrders.Count(order => SlaMinutesRemaining(order) is >= 0 and <= 30),
            openOrders.Count(order => order.Status == WorkOrderStatus.Escalated),
            highestRisk is null ? "None" : SlaRiskLevel(highestRisk),
            highestRisk?.WorkOrderNo);
    }

    private static string SlaRiskLevel(WorkOrder order)
    {
        var remaining = SlaMinutesRemaining(order);
        if (order.Status == WorkOrderStatus.Escalated || remaining <= 30)
        {
            return "High";
        }

        return remaining <= 90 ? "Medium" : "Low";
    }

    private static int SlaMinutesRemaining(WorkOrder order) =>
        (int)Math.Round((order.SlaDueAt - SeedTime).TotalMinutes);

    private void AddTimeline(
        string workOrderNo,
        string operatorName,
        string action,
        WorkOrderStatus fromStatus,
        WorkOrderStatus toStatus,
        string remark,
        int? rating = null)
    {
        _timeline[workOrderNo].Add(new WorkOrderTimelineEntry(
            SeedTime.AddMinutes(_timeline[workOrderNo].Count * 3),
            operatorName,
            action,
            fromStatus,
            toStatus,
            remark,
            rating));
    }
}
