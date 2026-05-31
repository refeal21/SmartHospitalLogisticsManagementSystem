using SmartHospitalLogistics.Domain;

namespace SmartHospitalLogistics.Application;

public interface IAssetMaintenanceService
{
    AssetMaintenanceBoard GetMaintenanceBoard();

    AssetMaintenanceDetail? GetAssetMaintenanceDetail(string assetCode);

    MaintenanceTaskOperationResult CompleteTask(string taskNo, CompleteMaintenanceTaskCommand command);
}

public sealed class AssetMaintenanceService : IAssetMaintenanceService
{
    private static readonly DateTimeOffset SeedTime = new(2026, 5, 30, 9, 30, 0, TimeSpan.FromHours(8));

    private readonly Dictionary<string, AssetLedgerItem> _assets;

    private readonly Dictionary<string, MaintenancePlan> _plans;

    private readonly Dictionary<string, MaintenanceTask> _tasks;

    private readonly List<AssetLifecycleEvent> _lifecycleEvents;

    private readonly IAssetMaintenancePersistence? _persistence;

    private readonly IWorkOrderIntakeService? _workOrderIntake;

    public AssetMaintenanceService(IAssetMaintenancePersistence? persistence = null, IWorkOrderIntakeService? workOrderIntake = null)
    {
        _persistence = persistence;
        _workOrderIntake = workOrderIntake;
        var persisted = _persistence?.Load();
        if (persisted is not null && (persisted.Assets.Count > 0 || persisted.Plans.Count > 0 || persisted.Tasks.Count > 0))
        {
            _assets = persisted.Assets.ToDictionary(asset => asset.AssetCode, StringComparer.OrdinalIgnoreCase);
            _plans = persisted.Plans.ToDictionary(plan => plan.PlanCode, StringComparer.OrdinalIgnoreCase);
            _tasks = persisted.Tasks.ToDictionary(task => task.TaskNo, StringComparer.OrdinalIgnoreCase);
            _lifecycleEvents = persisted.LifecycleEvents.ToList();
        }
        else
        {
            _assets = BuildAssets().ToDictionary(asset => asset.AssetCode, StringComparer.OrdinalIgnoreCase);
            _plans = BuildPlans().ToDictionary(plan => plan.PlanCode, StringComparer.OrdinalIgnoreCase);
            _tasks = BuildTasks().ToDictionary(task => task.TaskNo, StringComparer.OrdinalIgnoreCase);
            _lifecycleEvents = BuildLifecycleEvents().ToList();
            PersistSeedData();
        }
    }

    public AssetMaintenanceBoard GetMaintenanceBoard()
    {
        var assets = _assets.Values
            .OrderBy(asset => asset.System)
            .ThenBy(asset => asset.AssetCode)
            .ToArray();
        var plans = _plans.Values
            .OrderBy(plan => plan.AssetCode)
            .ThenBy(plan => plan.NextDueAt)
            .ToArray();
        var dueTasks = GetDueTasks();
        var kpis = BuildKpis(assets, dueTasks);

        return new AssetMaintenanceBoard(
            SeedTime,
            assets,
            plans,
            dueTasks,
            _lifecycleEvents.OrderByDescending(item => item.OccurredAt).Take(8).ToArray(),
            BuildSourceEvidence(),
            kpis);
    }

    public AssetMaintenanceDetail? GetAssetMaintenanceDetail(string assetCode)
    {
        if (!_assets.TryGetValue(assetCode, out var asset))
        {
            return null;
        }

        var plans = _plans.Values
            .Where(plan => string.Equals(plan.AssetCode, asset.AssetCode, StringComparison.OrdinalIgnoreCase))
            .OrderBy(plan => plan.NextDueAt)
            .ToArray();
        var tasks = _tasks.Values
            .Where(task => string.Equals(task.AssetCode, asset.AssetCode, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(task => task.ScheduledAt)
            .ToArray();
        var lifecycle = _lifecycleEvents
            .Where(item => string.Equals(item.AssetCode, asset.AssetCode, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(item => item.OccurredAt)
            .ToArray();

        return new AssetMaintenanceDetail(asset, plans, tasks, lifecycle, BuildSourceEvidence());
    }

    public MaintenanceTaskOperationResult CompleteTask(string taskNo, CompleteMaintenanceTaskCommand command)
    {
        if (!_tasks.TryGetValue(taskNo, out var task))
        {
            return new MaintenanceTaskOperationResult(false, $"Maintenance task {taskNo} was not found.", null, NotFound: true);
        }

        if (task.Status is MaintenanceTaskStatus.Completed or MaintenanceTaskStatus.ConvertedToWorkOrder)
        {
            return new MaintenanceTaskOperationResult(false, $"Maintenance task {taskNo} is already closed.", task);
        }

        var closedStatus = command.Outcome switch
        {
            MaintenanceOutcome.Abnormal when command.ConvertToWorkOrder => MaintenanceTaskStatus.ConvertedToWorkOrder,
            MaintenanceOutcome.Abnormal => MaintenanceTaskStatus.RequiresRepair,
            _ => MaintenanceTaskStatus.Completed
        };
        var completedAt = SeedTime.AddMinutes(30 + _lifecycleEvents.Count);
        var generatedWorkOrder = command.Outcome == MaintenanceOutcome.Abnormal && command.ConvertToWorkOrder
            ? BuildGeneratedWorkOrder(task, completedAt)
            : null;
        var lifecycleEventType = command.Outcome == MaintenanceOutcome.Abnormal
            ? "异常转工单"
            : "完成巡检";

        var updatedTask = task with
        {
            Status = closedStatus,
            ChecklistResults = command.ChecklistResults,
            Outcome = command.Outcome,
            CompletedAt = completedAt,
            CompletedBy = command.Operator,
            WorkOrderNo = generatedWorkOrder?.WorkOrderNo
        };
        var lifecycleEvent = new AssetLifecycleEvent(
            completedAt,
            task.AssetCode,
            lifecycleEventType,
            command.Operator,
            command.Remark);

        if (generatedWorkOrder is not null && _workOrderIntake is not null)
        {
            var creation = _workOrderIntake.CreateExternalWorkOrder(new CreateExternalWorkOrderCommand(
                generatedWorkOrder.WorkOrderNo,
                generatedWorkOrder.Title,
                generatedWorkOrder.ServiceType,
                generatedWorkOrder.Priority,
                generatedWorkOrder.Location,
                generatedWorkOrder.ResponsibleTeam,
                generatedWorkOrder.CreatedAt,
                generatedWorkOrder.CreatedAt.AddHours(generatedWorkOrder.Priority == Priority.Critical ? 1 : 2),
                command.Operator,
                command.Remark));

            if (!creation.Succeeded || creation.Detail is null)
            {
                return new MaintenanceTaskOperationResult(false, creation.ErrorMessage ?? "Failed to create follow-up work order.", task);
            }
        }

        _persistence?.SaveTaskCompletion(updatedTask, lifecycleEvent);
        _tasks[task.TaskNo] = updatedTask;
        _lifecycleEvents.Add(lifecycleEvent);

        return new MaintenanceTaskOperationResult(true, null, updatedTask, generatedWorkOrder);
    }

    private MaintenanceTask[] GetDueTasks() =>
        _tasks.Values
            .Where(task => task.Status is MaintenanceTaskStatus.Planned or MaintenanceTaskStatus.Due or MaintenanceTaskStatus.Overdue)
            .OrderByDescending(task => task.Status == MaintenanceTaskStatus.Overdue)
            .ThenByDescending(task => task.Priority)
            .ThenBy(task => task.DueAt)
            .ToArray();

    private static AssetMaintenanceKpi BuildKpis(IReadOnlyCollection<AssetLedgerItem> assets, IReadOnlyCollection<MaintenanceTask> dueTasks)
    {
        var allTasks = dueTasks.Count;
        var completedTasks = 4m;

        return new AssetMaintenanceKpi(
            assets.Count,
            assets.Count(asset => asset.Status is FacilityStatus.Warning or FacilityStatus.Fault or FacilityStatus.Maintenance),
            dueTasks.Count,
            dueTasks.Count(task => task.Status == MaintenanceTaskStatus.Overdue),
            allTasks == 0 ? 100m : decimal.Round(completedTasks / (completedTasks + allTasks) * 100m, 1),
            (int)assets.Average(asset => asset.HealthScore));
    }

    private MaintenanceGeneratedWorkOrder BuildGeneratedWorkOrder(MaintenanceTask task, DateTimeOffset createdAt)
    {
        var workOrderNo = $"WO-MT-{task.TaskNo.Replace("MT-", string.Empty, StringComparison.OrdinalIgnoreCase)}";
        var location = _assets.TryGetValue(task.AssetCode, out var asset)
            ? asset.Location
            : BuildAssets().Single(asset => asset.AssetCode == task.AssetCode).Location;

        return new MaintenanceGeneratedWorkOrder(
            workOrderNo,
            $"{task.Title}异常处置",
            "设备巡检异常",
            task.Priority,
            WorkOrderStatus.New,
            location,
            task.ResponsibleTeam,
            createdAt);
    }

    private static AssetLedgerItem[] BuildAssets()
    {
        var outpatientLobby = new SpatialLocation("同仁亦庄院区", "门诊医技楼", "F1", "共享大厅", "BIM-OPD-F1-LOBBY");
        var inpatientWard = new SpatialLocation("同仁亦庄院区", "住院楼", "F8", "眼科病区", "BIM-IPD-F8-WARD");
        var energyRoom = new SpatialLocation("同仁亦庄院区", "能源中心", "B1", "冷站机房", "BIM-ENE-B1-CHILLER");
        var wasteRoom = new SpatialLocation("同仁亦庄院区", "后勤楼", "F1", "医废暂存间", "BIM-LOG-F1-WASTE");

        return
        [
            new AssetLedgerItem(
                "WASTE-F1-01",
                "医废暂存间负压设备",
                "医疗废物",
                AssetCriticality.LifeSafety,
                wasteRoom,
                FacilityStatus.Fault,
                "环境监管班组",
                "Mock厂商",
                "NP-200",
                "2023-08-18",
                "每日巡检 + 异常转工单",
                52,
                "负压低于阈值，需要联动医废处置工单",
                ["中科医信", "PPT"]),
            new AssetLedgerItem(
                "MEDGAS-IPD-8F",
                "住院 8F 医用气体分区阀箱",
                "医用气体",
                AssetCriticality.LifeSafety,
                inpatientWard,
                FacilityStatus.Maintenance,
                "医气维保人员",
                "Mock厂商",
                "MGV-8F",
                "2022-05-09",
                "周巡检 + 压力异常闭环",
                68,
                "计划保养中，需关注氧气压力波动",
                ["北建院", "中科医信", "PPT"]),
            new AssetLedgerItem(
                "CHW-B1-02",
                "冷站 2 号冷冻泵",
                "暖通空调",
                AssetCriticality.High,
                energyRoom,
                FacilityStatus.Normal,
                "暖通班工作人员",
                "Mock厂商",
                "CHW-P-02",
                "2021-11-12",
                "月度保养 + 能耗趋势复核",
                91,
                "运行稳定，维持计划保养",
                ["北建院", "中科医信", "PPT"]),
            new AssetLedgerItem(
                "ELV-OPD-01",
                "门诊楼 1 号医梯",
                "电梯管理",
                AssetCriticality.High,
                outpatientLobby,
                FacilityStatus.Warning,
                "电梯维保组",
                "Mock厂商",
                "ELV-MED-01",
                "2020-06-21",
                "月度巡检 + 年检档案",
                74,
                "运行频次突增，建议提前巡检曳引系统",
                ["中科医信", "PPT"])
        ];
    }

    private static MaintenancePlan[] BuildPlans()
    {
        var sourceEvidence = BuildSourceEvidence();

        return
        [
            new MaintenancePlan(
                "MP-WASTE-NEG-PRESSURE",
                "WASTE-F1-01",
                "医废暂存间负压安全巡检",
                MaintenanceTaskType.SafetyCheck,
                1,
                SeedTime.AddMinutes(20),
                "环境监管班组",
                [
                    new InspectionChecklistItem("CHK-NEGATIVE-PRESSURE", "负压值", "低于阈值必须转异常工单", true),
                    new InspectionChecklistItem("CHK-DOOR", "门禁与暂存状态", "门禁、暂存状态无异常", true)
                ],
                sourceEvidence),
            new MaintenancePlan(
                "MP-MEDGAS-VALVE",
                "MEDGAS-IPD-8F",
                "医用气体分区阀箱周巡检",
                MaintenanceTaskType.Inspection,
                7,
                SeedTime.AddMinutes(-10),
                "医气维保人员",
                [
                    new InspectionChecklistItem("CHK-PRESSURE", "氧气压力", "压力在上下限范围内", true),
                    new InspectionChecklistItem("CHK-VALVE", "阀门状态", "阀门开闭和标识正常", true)
                ],
                sourceEvidence),
            new MaintenancePlan(
                "MP-CHW-PUMP",
                "CHW-B1-02",
                "冷冻泵运行与能耗巡检",
                MaintenanceTaskType.PreventiveMaintenance,
                30,
                SeedTime.AddHours(2),
                "暖通班工作人员",
                [
                    new InspectionChecklistItem("CHK-PRESSURE", "水系统压差", "压差稳定且无异常波动", true),
                    new InspectionChecklistItem("CHK-CURRENT", "运行电流", "电流在额定范围内", true)
                ],
                sourceEvidence),
            new MaintenancePlan(
                "MP-ELV-MONTHLY",
                "ELV-OPD-01",
                "医梯月度安全巡检",
                MaintenanceTaskType.SafetyCheck,
                30,
                SeedTime.AddHours(4),
                "电梯维保组",
                [
                    new InspectionChecklistItem("CHK-RUN", "运行平稳性", "运行无异响、无抖动", true),
                    new InspectionChecklistItem("CHK-RESCUE", "应急装置", "五方通话和救援装置正常", true)
                ],
                sourceEvidence)
        ];
    }

    private static MaintenanceTask[] BuildTasks() =>
    [
        new MaintenanceTask(
            "MT-20260530-0001",
            "MP-WASTE-NEG-PRESSURE",
            "WASTE-F1-01",
            "医废暂存间负压安全巡检",
            MaintenanceTaskType.SafetyCheck,
            MaintenanceTaskStatus.Due,
            Priority.Critical,
            SeedTime.AddMinutes(-30),
            SeedTime.AddMinutes(20),
            "环境监管班组",
            []),
        new MaintenanceTask(
            "MT-20260530-0002",
            "MP-MEDGAS-VALVE",
            "MEDGAS-IPD-8F",
            "住院 8F 医用气体分区阀箱周巡检",
            MaintenanceTaskType.Inspection,
            MaintenanceTaskStatus.Overdue,
            Priority.Critical,
            SeedTime.AddHours(-1),
            SeedTime.AddMinutes(-10),
            "医气维保人员",
            []),
        new MaintenanceTask(
            "MT-20260530-0003",
            "MP-CHW-PUMP",
            "CHW-B1-02",
            "冷冻泵运行与能耗巡检",
            MaintenanceTaskType.PreventiveMaintenance,
            MaintenanceTaskStatus.Due,
            Priority.High,
            SeedTime.AddMinutes(-20),
            SeedTime.AddHours(2),
            "暖通班工作人员",
            []),
        new MaintenanceTask(
            "MT-20260530-0004",
            "MP-ELV-MONTHLY",
            "ELV-OPD-01",
            "门诊楼 1 号医梯月度安全巡检",
            MaintenanceTaskType.SafetyCheck,
            MaintenanceTaskStatus.Due,
            Priority.High,
            SeedTime.AddMinutes(-15),
            SeedTime.AddHours(4),
            "电梯维保组",
            [])
    ];

    private static AssetLifecycleEvent[] BuildLifecycleEvents() =>
    [
        new AssetLifecycleEvent(SeedTime.AddDays(-6), "MEDGAS-IPD-8F", "计划保养", "医气维保人员", "完成分区阀箱压力表校准"),
        new AssetLifecycleEvent(SeedTime.AddDays(-4), "ELV-OPD-01", "运行预警", "电梯维保组", "运行频次高于日均值，列入提前巡检"),
        new AssetLifecycleEvent(SeedTime.AddDays(-2), "CHW-B1-02", "能耗复核", "暖通班工作人员", "夜间节能策略复核通过"),
        new AssetLifecycleEvent(SeedTime.AddHours(-3), "WASTE-F1-01", "告警联动", "环境监管班组", "医废暂存间负压低于阈值，已进入预警池")
    ];

    private static FeatureEvidence[] BuildSourceEvidence() =>
    [
        new FeatureEvidence(
            "设备设施资产台账",
            ["中科医信", "PPT"],
            "竞品功能树要求资产分类、资产台账、资产维修管理；PPT 要求基础运行设备设施全生命周期管理。"),
        new FeatureEvidence(
            "巡检保养计划",
            ["中科医信", "PPT"],
            "竞品功能树要求工作日历、巡检/保养、计划管理；PPT 要求设备监测预警与运维处置联动。"),
        new FeatureEvidence(
            "医用气体监测",
            ["北建院", "中科医信", "PPT"],
            "客户数据、竞品功能和 PPT 均涉及医用气体压力、阀箱、报警处置和维修闭环。"),
        new FeatureEvidence(
            "可视化空间运维",
            ["中科医信", "PPT"],
            "资产台账必须绑定楼栋、楼层、房间和 BIM 构件，支持从空间定位进入巡检与维修。")
    ];

    private void PersistSeedData()
    {
        if (_persistence is null)
        {
            return;
        }

        foreach (var asset in _assets.Values)
        {
            _persistence.SaveAsset(asset);
        }

        foreach (var plan in _plans.Values)
        {
            _persistence.SavePlan(plan);
        }

        foreach (var task in _tasks.Values)
        {
            _persistence.SaveTask(task);
        }

        foreach (var lifecycleEvent in _lifecycleEvents)
        {
            _persistence.SaveLifecycleEvent(lifecycleEvent);
        }
    }
}
