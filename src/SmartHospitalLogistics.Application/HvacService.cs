using SmartHospitalLogistics.Domain;

namespace SmartHospitalLogistics.Application;

public interface IHvacService
{
    HvacBoard GetBoard();

    HvacLoopDetail? GetLoopDetail(string loopCode);
}

public sealed class HvacService : IHvacService
{
    private static readonly DateTimeOffset SeedTime = new(2026, 5, 30, 9, 30, 0, TimeSpan.FromHours(8));

    private readonly IHvacPersistence? _persistence;
    private readonly IIotIntegrationService _iotIntegration;
    private readonly IAssetMaintenanceService _assetMaintenance;
    private readonly IMonitoringAlarmService _monitoringAlarms;
    private readonly IWorkOrderDispatchService _workOrderDispatch;
    private readonly Dictionary<string, HvacLoop> _loops;

    public HvacService(
        IHvacPersistence? persistence = null,
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
        if (persisted is not null && persisted.Loops.Count > 0)
        {
            _loops = persisted.Loops.ToDictionary(loop => loop.LoopCode, StringComparer.OrdinalIgnoreCase);
        }
        else
        {
            _loops = BuildSeedLoops().ToDictionary(loop => loop.LoopCode, StringComparer.OrdinalIgnoreCase);
            PersistSeedData();
        }
    }

    public HvacBoard GetBoard()
    {
        var catalog = _iotIntegration.GetCatalog();
        var maintenanceBoard = _assetMaintenance.GetMaintenanceBoard();
        var alarmBoard = _monitoringAlarms.GetAlarmBoard();
        var dispatchBoard = _workOrderDispatch.GetDispatchBoard();

        var loops = _loops.Values
            .OrderBy(loop => loop.LoopCode)
            .ToArray();
        var pointCodes = loops.Select(loop => loop.MonitoringPointCode).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var assetCodes = loops.Select(loop => loop.AssetCode).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var monitoringPoints = catalog.Points
            .Where(point => point.Category == IotSystemCategory.Hvac || pointCodes.Contains(point.PointCode))
            .OrderBy(point => point.PointCode)
            .ToArray();
        var hvacAssets = maintenanceBoard.Assets
            .Where(asset => assetCodes.Contains(asset.AssetCode) || IsHvacText(asset.System))
            .OrderBy(asset => asset.AssetCode)
            .ToArray();
        var activeAlarms = alarmBoard.Alarms
            .Where(alarm => pointCodes.Contains(alarm.PointCode) && alarm.Status != MonitoringAlarmStatus.Closed)
            .OrderByDescending(alarm => alarm.RiskLevel)
            .ThenByDescending(alarm => alarm.TriggeredAt)
            .ToArray();
        var dueTasks = maintenanceBoard.DueTasks
            .Where(task => assetCodes.Contains(task.AssetCode))
            .OrderByDescending(task => task.Priority)
            .ThenBy(task => task.DueAt)
            .ToArray();
        var openWorkOrders = dispatchBoard.WorkOrders
            .Where(order => IsHvacWorkOrder(order, loops))
            .OrderByDescending(order => order.Priority)
            .ThenBy(order => order.SlaDueAt)
            .ToArray();
        var enrichedLoops = loops
            .Select(loop => loop with
            {
                Status = DetermineStatus(
                    loop,
                    activeAlarms.Where(alarm => string.Equals(alarm.PointCode, loop.MonitoringPointCode, StringComparison.OrdinalIgnoreCase)),
                    openWorkOrders.Where(order => IsLoopWorkOrder(order, loop)),
                    dueTasks.Where(task => string.Equals(task.AssetCode, loop.AssetCode, StringComparison.OrdinalIgnoreCase)),
                    hvacAssets.Where(asset => string.Equals(asset.AssetCode, loop.AssetCode, StringComparison.OrdinalIgnoreCase)))
            })
            .ToArray();

        return new HvacBoard(
            SeedTime,
            enrichedLoops,
            monitoringPoints,
            hvacAssets,
            activeAlarms,
            openWorkOrders,
            dueTasks,
            BuildSourceEvidence(),
            new HvacBoardKpi(
                enrichedLoops.Length,
                enrichedLoops.Count(loop => loop.Status != HvacLoopStatus.Normal),
                activeAlarms.Length,
                openWorkOrders.Length,
                dueTasks.Length));
    }

    public HvacLoopDetail? GetLoopDetail(string loopCode)
    {
        if (!_loops.TryGetValue(loopCode, out var loop))
        {
            return null;
        }

        var board = GetBoard();
        var enrichedLoop = board.Loops.First(item => string.Equals(item.LoopCode, loop.LoopCode, StringComparison.OrdinalIgnoreCase));
        var monitoringPoint = _iotIntegration.GetPointDetail(enrichedLoop.MonitoringPointCode);
        var hvacAsset = _assetMaintenance.GetAssetMaintenanceDetail(enrichedLoop.AssetCode);
        var activeAlarms = board.ActiveAlarms
            .Where(alarm => string.Equals(alarm.PointCode, enrichedLoop.MonitoringPointCode, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        var openWorkOrders = board.OpenWorkOrders
            .Where(order => IsLoopWorkOrder(order, enrichedLoop))
            .ToArray();
        var maintenanceTasks = hvacAsset?.Tasks
                .Where(task => task.Status != MaintenanceTaskStatus.Completed)
                .OrderByDescending(task => task.Priority)
                .ThenBy(task => task.DueAt)
                .ToArray()
            ?? [];

        return new HvacLoopDetail(
            enrichedLoop,
            monitoringPoint,
            hvacAsset,
            activeAlarms,
            openWorkOrders,
            maintenanceTasks,
            BuildSourceEvidence());
    }

    private static HvacLoopStatus DetermineStatus(
        HvacLoop loop,
        IEnumerable<MonitoringAlarmEvent> activeAlarms,
        IEnumerable<WorkOrder> openWorkOrders,
        IEnumerable<MaintenanceTask> dueTasks,
        IEnumerable<AssetLedgerItem> assets)
    {
        if (activeAlarms.Any(alarm => alarm.RiskLevel == TelemetryRiskLevel.Critical) ||
            openWorkOrders.Any(order => order.Priority == Priority.Critical))
        {
            return HvacLoopStatus.Critical;
        }

        if (dueTasks.Any(task => task.Status == MaintenanceTaskStatus.Overdue) ||
            assets.Any(asset => asset.Status == FacilityStatus.Maintenance))
        {
            return HvacLoopStatus.Maintenance;
        }

        if (activeAlarms.Any() ||
            dueTasks.Any() ||
            assets.Any(asset => asset.Status is FacilityStatus.Warning or FacilityStatus.Fault))
        {
            return HvacLoopStatus.Warning;
        }

        return loop.Status;
    }

    private static bool IsHvacWorkOrder(WorkOrder order, IReadOnlyList<HvacLoop> loops) =>
        loops.Any(loop => IsLoopWorkOrder(order, loop));

    private static bool IsLoopWorkOrder(WorkOrder order, HvacLoop loop)
    {
        if (!string.Equals(order.Location.BimElementId, loop.Location.BimElementId, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return string.Equals(order.ResponsibleTeam, loop.ResponsibleTeam, StringComparison.OrdinalIgnoreCase) ||
               IsHvacText(order.Title) ||
               IsHvacText(order.ServiceType) ||
               order.WorkOrderNo.StartsWith("WO-ALM-HVAC", StringComparison.OrdinalIgnoreCase) ||
               (order.WorkOrderNo.StartsWith("WO-MT-", StringComparison.OrdinalIgnoreCase) && IsHvacText(order.Title));
    }

    private static bool IsHvacText(string value) =>
        value.Contains("暖通", StringComparison.OrdinalIgnoreCase) ||
        value.Contains("空调", StringComparison.OrdinalIgnoreCase) ||
        value.Contains("冷站", StringComparison.OrdinalIgnoreCase) ||
        value.Contains("冷冻", StringComparison.OrdinalIgnoreCase) ||
        value.Contains("压差", StringComparison.OrdinalIgnoreCase) ||
        value.Contains("HVAC", StringComparison.OrdinalIgnoreCase) ||
        value.Contains("CHW", StringComparison.OrdinalIgnoreCase);

    private static HvacLoop[] BuildSeedLoops()
    {
        var chillerRoom = new SpatialLocation("同仁亦庄院区", "能源中心", "B1", "冷站机房", "BIM-ENE-B1-CHILLER");

        return
        [
            new HvacLoop(
                "HVAC-LOOP-B1-CHW",
                "B1 冷站冷冻水循环回路",
                "暖通/冷热站",
                chillerRoom,
                "暖通班工作人员",
                "HVAC-CHW-B1-02",
                "CHW-B1-02",
                HvacLoopStatus.Warning,
                ["supply_temp", "pressure", "flow", "energy"],
                "冷冻水回路压力、流量、供回水温度和能耗需要按阈值预警，并联动冷冻泵巡检与一站式工单调度。",
                BuildSourceEvidence())
        ];
    }

    private static FeatureEvidence[] BuildSourceEvidence() =>
    [
        new FeatureEvidence(
            "暖通/冷热站专项运行",
            ["北建院", "中科医信", "PPT"],
            "北建院客户数据定义供暖空调系统、传感器、安装位置、采集字段、角色和使用端；中科医信竞品包含冷热站、暖通监测、巡检保养和工单颗粒度；PPT定义BIM智慧运维集成边界。"),
        new FeatureEvidence(
            "暖通告警巡检工单闭环",
            ["北建院", "中科医信", "PPT"],
            "供回水温度、压力、流量、能耗、启停状态和故障状态必须能进入预警池，并与冷冻泵资产、巡检任务和工单调度贯通。")
    ];

    private void PersistSeedData()
    {
        if (_persistence is null)
        {
            return;
        }

        foreach (var loop in _loops.Values)
        {
            _persistence.SaveLoop(loop);
        }
    }
}
