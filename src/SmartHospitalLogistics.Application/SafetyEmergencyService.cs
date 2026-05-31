using SmartHospitalLogistics.Domain;

namespace SmartHospitalLogistics.Application;

public interface ISafetyEmergencyService
{
    SafetyEmergencyBoard GetBoard();

    SafetyEmergencyNodeDetail? GetNodeDetail(string nodeCode);
}

public sealed class SafetyEmergencyService : ISafetyEmergencyService
{
    private static readonly DateTimeOffset SeedTime = new(2026, 5, 30, 9, 30, 0, TimeSpan.FromHours(8));

    private readonly ISafetyEmergencyPersistence? _persistence;
    private readonly IIotIntegrationService _iotIntegration;
    private readonly IAssetMaintenanceService _assetMaintenance;
    private readonly IMonitoringAlarmService _monitoringAlarms;
    private readonly IWorkOrderDispatchService _workOrderDispatch;
    private readonly Dictionary<string, SafetyEmergencyNode> _nodes;

    public SafetyEmergencyService(
        ISafetyEmergencyPersistence? persistence = null,
        IIotIntegrationService? iotIntegration = null,
        IAssetMaintenanceService? assetMaintenance = null,
        IMonitoringAlarmService? monitoringAlarms = null,
        IWorkOrderDispatchService? workOrderDispatch = null)
    {
        _persistence = persistence;
        _workOrderDispatch = workOrderDispatch ?? new WorkOrderDispatchService();
        _monitoringAlarms = monitoringAlarms ?? new MonitoringAlarmService(workOrderIntake: _workOrderDispatch as IWorkOrderIntakeService);
        _iotIntegration = iotIntegration ?? new IotIntegrationService(alarmRecorder: _monitoringAlarms);
        _assetMaintenance = assetMaintenance ?? new AssetMaintenanceService(workOrderIntake: _workOrderDispatch as IWorkOrderIntakeService);

        var persisted = _persistence?.Load();
        if (persisted is not null && persisted.Nodes.Count > 0)
        {
            _nodes = persisted.Nodes.ToDictionary(node => node.NodeCode, StringComparer.OrdinalIgnoreCase);
        }
        else
        {
            _nodes = BuildSeedNodes().ToDictionary(node => node.NodeCode, StringComparer.OrdinalIgnoreCase);
            PersistSeedData();
        }
    }

    public SafetyEmergencyBoard GetBoard()
    {
        var catalog = _iotIntegration.GetCatalog();
        var maintenanceBoard = _assetMaintenance.GetMaintenanceBoard();
        var alarmBoard = _monitoringAlarms.GetAlarmBoard();
        var dispatchBoard = _workOrderDispatch.GetDispatchBoard();
        var rawNodes = _nodes.Values.OrderBy(node => node.NodeCode).ToArray();
        var pointCodes = rawNodes.Select(node => node.MonitoringPointCode).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var assetCodes = rawNodes.Select(node => node.AssetCode).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var activeAlarms = alarmBoard.Alarms
            .Where(alarm => pointCodes.Contains(alarm.PointCode) && alarm.Status != MonitoringAlarmStatus.Closed)
            .OrderByDescending(alarm => alarm.RiskLevel)
            .ThenByDescending(alarm => alarm.TriggeredAt)
            .ToArray();
        var openWorkOrders = dispatchBoard.WorkOrders
            .Where(order => rawNodes.Any(node => IsNodeWorkOrder(order, node)))
            .OrderByDescending(order => order.Priority)
            .ThenBy(order => order.SlaDueAt)
            .ToArray();
        var nodes = rawNodes
            .Select(node => node with
            {
                Status = DetermineStatus(
                    node,
                    activeAlarms.Where(alarm => string.Equals(alarm.PointCode, node.MonitoringPointCode, StringComparison.OrdinalIgnoreCase)),
                    openWorkOrders.Where(order => IsNodeWorkOrder(order, node)))
            })
            .ToArray();
        var safetyAssets = maintenanceBoard.Assets
            .Where(asset => assetCodes.Contains(asset.AssetCode) || IsSafetyText(asset.System) || IsSafetyText(asset.AssetCode))
            .Concat(BuildFallbackSafetyAssets().Where(asset => !maintenanceBoard.Assets.Any(existing =>
                string.Equals(existing.AssetCode, asset.AssetCode, StringComparison.OrdinalIgnoreCase))))
            .OrderBy(asset => asset.AssetCode)
            .ToArray();

        return new SafetyEmergencyBoard(
            SeedTime,
            nodes,
            catalog.Points
                .Where(point =>
                    pointCodes.Contains(point.PointCode) ||
                    point.Category is IotSystemCategory.FireSafety or IotSystemCategory.SecurityIntelligence)
                .OrderBy(point => point.PointCode)
                .ToArray(),
            safetyAssets,
            activeAlarms,
            openWorkOrders,
            BuildResponseProcedure(),
            BuildSourceEvidence(),
            new SafetyEmergencyBoardKpi(
                nodes.Length,
                activeAlarms.Length,
                nodes.Count(node => node.Status is SafetyEmergencyNodeStatus.Critical or SafetyEmergencyNodeStatus.Commanding),
                openWorkOrders.Length,
                activeAlarms.Count(alarm => alarm.RiskLevel == TelemetryRiskLevel.Critical) + openWorkOrders.Count(order => order.Priority == Priority.Critical)));
    }

    public SafetyEmergencyNodeDetail? GetNodeDetail(string nodeCode)
    {
        if (!_nodes.TryGetValue(nodeCode, out var node))
        {
            return null;
        }

        var board = GetBoard();
        var enrichedNode = board.Nodes.First(item => string.Equals(item.NodeCode, node.NodeCode, StringComparison.OrdinalIgnoreCase));
        var monitoringPoint = _iotIntegration.GetPointDetail(enrichedNode.MonitoringPointCode);
        var safetyAsset = _assetMaintenance.GetAssetMaintenanceDetail(enrichedNode.AssetCode)
            ?? BuildFallbackAssetDetail(enrichedNode.AssetCode);
        var activeAlarms = board.ActiveAlarms
            .Where(alarm => string.Equals(alarm.PointCode, enrichedNode.MonitoringPointCode, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        var openWorkOrders = board.OpenWorkOrders
            .Where(order => IsNodeWorkOrder(order, enrichedNode))
            .ToArray();

        return new SafetyEmergencyNodeDetail(
            enrichedNode,
            monitoringPoint,
            safetyAsset,
            activeAlarms,
            openWorkOrders,
            BuildResponseProcedure(enrichedNode.EventType),
            BuildSourceEvidence());
    }

    private static SafetyEmergencyNodeStatus DetermineStatus(
        SafetyEmergencyNode node,
        IEnumerable<MonitoringAlarmEvent> activeAlarms,
        IEnumerable<WorkOrder> openWorkOrders)
    {
        if (activeAlarms.Any(alarm => alarm.RiskLevel == TelemetryRiskLevel.Critical) ||
            openWorkOrders.Any(order => order.Priority == Priority.Critical))
        {
            return SafetyEmergencyNodeStatus.Critical;
        }

        if (activeAlarms.Any() || openWorkOrders.Any())
        {
            return SafetyEmergencyNodeStatus.Commanding;
        }

        return node.Status;
    }

    private static bool IsNodeWorkOrder(WorkOrder order, SafetyEmergencyNode node)
    {
        if (!string.Equals(order.Location.BimElementId, node.Location.BimElementId, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return string.Equals(order.ResponsibleTeam, node.ResponsibleTeam, StringComparison.OrdinalIgnoreCase) ||
               order.WorkOrderNo.StartsWith("WO-ALM-FIRE", StringComparison.OrdinalIgnoreCase) ||
               order.WorkOrderNo.StartsWith("WO-ALM-SEC", StringComparison.OrdinalIgnoreCase) ||
               IsSafetyText(order.Title) ||
               IsSafetyText(order.ServiceType);
    }

    private static bool IsSafetyText(string value) =>
        value.Contains("消防", StringComparison.OrdinalIgnoreCase) ||
        value.Contains("火警", StringComparison.OrdinalIgnoreCase) ||
        value.Contains("安防", StringComparison.OrdinalIgnoreCase) ||
        value.Contains("门禁", StringComparison.OrdinalIgnoreCase) ||
        value.Contains("应急", StringComparison.OrdinalIgnoreCase) ||
        value.Contains("FIRE", StringComparison.OrdinalIgnoreCase) ||
        value.Contains("SEC", StringComparison.OrdinalIgnoreCase) ||
        value.Contains("safety", StringComparison.OrdinalIgnoreCase);

    private static AssetMaintenanceDetail? BuildFallbackAssetDetail(string assetCode)
    {
        var asset = BuildFallbackSafetyAssets()
            .FirstOrDefault(item => string.Equals(item.AssetCode, assetCode, StringComparison.OrdinalIgnoreCase));
        return asset is null
            ? null
            : new AssetMaintenanceDetail(asset, [], [], [], BuildSourceEvidence());
    }

    private static AssetLedgerItem[] BuildFallbackSafetyAssets()
    {
        var outpatientFire = new SpatialLocation("同仁亦庄院区", "门诊医技楼", "F1", "门诊共享大厅", "BIM-SEC-OPD-1F-FIRE");
        var emergencyAccess = new SpatialLocation("同仁亦庄院区", "急诊楼", "F1", "急诊出入口", "BIM-SEC-ER-ACCESS");

        return
        [
            new AssetLedgerItem(
                "FIRE-ALARM-OPD-1F",
                "门诊 1F 火灾自动报警控制点",
                "消防报警",
                AssetCriticality.LifeSafety,
                outpatientFire,
                FacilityStatus.Warning,
                "消防值班人员",
                "Mock厂商",
                "FAS-OPD-1F",
                "2021-07-01",
                "每日联动测试 + 异常转工单",
                83,
                "烟感、手报、防火门和消防联动状态需进入统一告警与应急处置闭环",
                ["北建院", "PPT"]),
            new AssetLedgerItem(
                "SEC-ACCESS-ER-DOOR",
                "急诊出入口门禁安防控制器",
                "安防门禁",
                AssetCriticality.High,
                emergencyAccess,
                FacilityStatus.Normal,
                "保卫处值班人员",
                "Mock厂商",
                "ACS-ER-01",
                "2022-02-15",
                "门禁在线监测 + 强开告警",
                88,
                "强开、离线和报警状态需联动保卫处确认并可转工单",
                ["北建院", "PPT"])
        ];
    }

    private static SafetyEmergencyNode[] BuildSeedNodes()
    {
        var outpatientFire = new SpatialLocation("同仁亦庄院区", "门诊医技楼", "F1", "门诊共享大厅", "BIM-SEC-OPD-1F-FIRE");
        var emergencyAccess = new SpatialLocation("同仁亦庄院区", "急诊楼", "F1", "急诊出入口", "BIM-SEC-ER-ACCESS");

        return
        [
            new SafetyEmergencyNode(
                "SAFE-FIRE-OPD-1F",
                "门诊 1F 火灾自动报警联动点",
                SafetyEmergencyEventType.FireAlarm,
                outpatientFire,
                "消防值班人员",
                "FIRE-SMOKE-OPD-1F-01",
                "FIRE-ALARM-OPD-1F",
                SafetyEmergencyNodeStatus.Warning,
                EmergencyResponseLevel.LevelOne,
                ["火灾自动报警", "电气火灾监控", "防火门监控", "BIM 空间定位", "一站式工单"],
                BuildSourceEvidence()),
            new SafetyEmergencyNode(
                "SAFE-SEC-ER-ACCESS",
                "急诊出入口安防门禁联动点",
                SafetyEmergencyEventType.AccessControl,
                emergencyAccess,
                "保卫处值班人员",
                "SEC-ACCESS-ER-01",
                "SEC-ACCESS-ER-DOOR",
                SafetyEmergencyNodeStatus.Normal,
                EmergencyResponseLevel.Attention,
                ["门禁", "视频安防", "BIM 空间定位", "一站式工单"],
                BuildSourceEvidence())
        ];
    }

    private static EmergencyResponseStep[] BuildResponseProcedure(SafetyEmergencyEventType? eventType = null)
    {
        var dispatchRole = eventType is SafetyEmergencyEventType.AccessControl or SafetyEmergencyEventType.VideoSecurity
            ? "保卫处值班人员"
            : "消防值班人员";

        return
        [
            new EmergencyResponseStep("RESP-VERIFY", 1, "确认告警来源、BIM 位置和现场风险等级", dispatchRole, 3, true),
            new EmergencyResponseStep("RESP-BROADCAST", 2, "联动通知后勤调度、安保/消防值班和属地科室", "后勤调度员", 5, true),
            new EmergencyResponseStep("RESP-DISPATCH", 3, "转入一站式工单并派发现场处置人员", "后勤调度员", 8, true),
            new EmergencyResponseStep("RESP-REVIEW", 4, "完成处置复核、留痕和应急复盘", dispatchRole, 30, false)
        ];
    }

    private static FeatureEvidence[] BuildSourceEvidence() =>
    [
        new FeatureEvidence(
            "消防/安防/应急联动",
            ["北建院", "PPT"],
            "北建院客户数据包含火灾自动报警、电气火灾、防火门、可燃气体、公共安全、门禁和视频安防等系统；PPT 要求通过 BIM 时空底座联动事件、空间和工单。"),
        new FeatureEvidence(
            "统一告警与工单闭环",
            ["中科医信", "PPT"],
            "竞品功能树包含统一报警、设备安全和应急保洁/任务处置颗粒度；本系统仅接入事件和处置闭环，不重做消防主机或安防平台。")
    ];

    private void PersistSeedData()
    {
        if (_persistence is null)
        {
            return;
        }

        foreach (var node in _nodes.Values)
        {
            _persistence.SaveNode(node);
        }
    }
}
