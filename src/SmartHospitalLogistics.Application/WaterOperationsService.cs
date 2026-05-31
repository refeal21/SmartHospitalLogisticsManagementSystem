using SmartHospitalLogistics.Domain;

namespace SmartHospitalLogistics.Application;

public interface IWaterOperationsService
{
    WaterOperationsBoard GetBoard();

    WaterOperationsUnitDetail? GetUnitDetail(string unitCode);
}

public sealed class WaterOperationsService : IWaterOperationsService
{
    private static readonly DateTimeOffset SeedTime = new(2026, 5, 30, 9, 30, 0, TimeSpan.FromHours(8));

    private readonly IWaterOperationsPersistence? _persistence;
    private readonly IIotIntegrationService _iotIntegration;
    private readonly IAssetMaintenanceService _assetMaintenance;
    private readonly IMonitoringAlarmService _monitoringAlarms;
    private readonly IWorkOrderDispatchService _workOrderDispatch;
    private readonly Dictionary<string, WaterOperationsUnit> _units;

    public WaterOperationsService(
        IWaterOperationsPersistence? persistence = null,
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
        if (persisted is not null && persisted.Units.Count > 0)
        {
            _units = persisted.Units.ToDictionary(unit => unit.UnitCode, StringComparer.OrdinalIgnoreCase);
        }
        else
        {
            _units = BuildSeedUnits().ToDictionary(unit => unit.UnitCode, StringComparer.OrdinalIgnoreCase);
            PersistSeedData();
        }
    }

    public WaterOperationsBoard GetBoard()
    {
        var catalog = _iotIntegration.GetCatalog();
        var maintenanceBoard = _assetMaintenance.GetMaintenanceBoard();
        var alarmBoard = _monitoringAlarms.GetAlarmBoard();
        var dispatchBoard = _workOrderDispatch.GetDispatchBoard();

        var units = _units.Values
            .OrderBy(unit => unit.UnitCode)
            .ToArray();
        var pointCodes = units.Select(unit => unit.MonitoringPointCode).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var assetCodes = units.Select(unit => unit.AssetCode).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var monitoringPoints = catalog.Points
            .Where(point =>
                point.Category is IotSystemCategory.WaterSupplyDrainage or IotSystemCategory.Sewage ||
                pointCodes.Contains(point.PointCode))
            .OrderBy(point => point.PointCode)
            .ToArray();
        var waterAssets = maintenanceBoard.Assets
            .Where(asset => assetCodes.Contains(asset.AssetCode) || IsWaterText(asset.System))
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
            .Where(order => IsWaterWorkOrder(order, units))
            .OrderByDescending(order => order.Priority)
            .ThenBy(order => order.SlaDueAt)
            .ToArray();
        var enrichedUnits = units
            .Select(unit => unit with
            {
                Status = DetermineStatus(
                    unit,
                    activeAlarms.Where(alarm => string.Equals(alarm.PointCode, unit.MonitoringPointCode, StringComparison.OrdinalIgnoreCase)),
                    openWorkOrders.Where(order => IsUnitWorkOrder(order, unit)),
                    dueTasks.Where(task => string.Equals(task.AssetCode, unit.AssetCode, StringComparison.OrdinalIgnoreCase)),
                    waterAssets.Where(asset => string.Equals(asset.AssetCode, unit.AssetCode, StringComparison.OrdinalIgnoreCase)))
            })
            .ToArray();

        return new WaterOperationsBoard(
            SeedTime,
            enrichedUnits,
            monitoringPoints,
            waterAssets,
            activeAlarms,
            openWorkOrders,
            dueTasks,
            BuildSourceEvidence(),
            new WaterOperationsBoardKpi(
                enrichedUnits.Length,
                enrichedUnits.Count(unit => unit.Status != WaterOperationsUnitStatus.Normal),
                activeAlarms.Length,
                openWorkOrders.Length,
                dueTasks.Length));
    }

    public WaterOperationsUnitDetail? GetUnitDetail(string unitCode)
    {
        if (!_units.TryGetValue(unitCode, out var unit))
        {
            return null;
        }

        var board = GetBoard();
        var enrichedUnit = board.Units.First(item => string.Equals(item.UnitCode, unit.UnitCode, StringComparison.OrdinalIgnoreCase));
        var monitoringPoint = _iotIntegration.GetPointDetail(enrichedUnit.MonitoringPointCode);
        var waterAsset = _assetMaintenance.GetAssetMaintenanceDetail(enrichedUnit.AssetCode);
        var activeAlarms = board.ActiveAlarms
            .Where(alarm => string.Equals(alarm.PointCode, enrichedUnit.MonitoringPointCode, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        var openWorkOrders = board.OpenWorkOrders
            .Where(order => IsUnitWorkOrder(order, enrichedUnit))
            .ToArray();
        var maintenanceTasks = waterAsset?.Tasks
                .Where(task => task.Status != MaintenanceTaskStatus.Completed)
                .OrderByDescending(task => task.Priority)
                .ThenBy(task => task.DueAt)
                .ToArray()
            ?? [];

        return new WaterOperationsUnitDetail(
            enrichedUnit,
            monitoringPoint,
            waterAsset,
            activeAlarms,
            openWorkOrders,
            maintenanceTasks,
            BuildSourceEvidence());
    }

    private static WaterOperationsUnitStatus DetermineStatus(
        WaterOperationsUnit unit,
        IEnumerable<MonitoringAlarmEvent> activeAlarms,
        IEnumerable<WorkOrder> openWorkOrders,
        IEnumerable<MaintenanceTask> dueTasks,
        IEnumerable<AssetLedgerItem> assets)
    {
        if (activeAlarms.Any(alarm => alarm.RiskLevel == TelemetryRiskLevel.Critical) ||
            openWorkOrders.Any(order => order.Priority == Priority.Critical))
        {
            return WaterOperationsUnitStatus.Critical;
        }

        if (dueTasks.Any(task => task.Status == MaintenanceTaskStatus.Overdue) ||
            assets.Any(asset => asset.Status == FacilityStatus.Maintenance))
        {
            return WaterOperationsUnitStatus.Maintenance;
        }

        if (activeAlarms.Any() ||
            dueTasks.Any() ||
            assets.Any(asset => asset.Status is FacilityStatus.Warning or FacilityStatus.Fault))
        {
            return WaterOperationsUnitStatus.Warning;
        }

        return unit.Status;
    }

    private static bool IsWaterWorkOrder(WorkOrder order, IReadOnlyList<WaterOperationsUnit> units) =>
        units.Any(unit => IsUnitWorkOrder(order, unit));

    private static bool IsUnitWorkOrder(WorkOrder order, WaterOperationsUnit unit)
    {
        if (!string.Equals(order.Location.BimElementId, unit.Location.BimElementId, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return string.Equals(order.ResponsibleTeam, unit.ResponsibleTeam, StringComparison.OrdinalIgnoreCase) ||
               IsWaterText(order.Title) ||
               IsWaterText(order.ServiceType) ||
               order.WorkOrderNo.StartsWith("WO-ALM-WATER", StringComparison.OrdinalIgnoreCase) ||
               order.WorkOrderNo.StartsWith("WO-ALM-SEWAGE", StringComparison.OrdinalIgnoreCase) ||
               (order.WorkOrderNo.StartsWith("WO-MT-", StringComparison.OrdinalIgnoreCase) && IsWaterText(order.Title));
    }

    private static bool IsWaterText(string value) =>
        value.Contains("给排水", StringComparison.OrdinalIgnoreCase) ||
        value.Contains("给水", StringComparison.OrdinalIgnoreCase) ||
        value.Contains("排水", StringComparison.OrdinalIgnoreCase) ||
        value.Contains("污水", StringComparison.OrdinalIgnoreCase) ||
        value.Contains("水质", StringComparison.OrdinalIgnoreCase) ||
        value.Contains("COD", StringComparison.OrdinalIgnoreCase) ||
        value.Contains("WATER", StringComparison.OrdinalIgnoreCase) ||
        value.Contains("SEWAGE", StringComparison.OrdinalIgnoreCase);

    private static WaterOperationsUnit[] BuildSeedUnits()
    {
        var pumpRoom = new SpatialLocation("同仁亦庄院区", "能源中心", "B1", "给水泵房", "BIM-ENE-B1-PUMP");
        var sewage = new SpatialLocation("同仁亦庄院区", "后勤楼", "B1", "污水处理站", "BIM-LOG-B1-SEWAGE");

        return
        [
            new WaterOperationsUnit(
                "WATER-SYS-B1-PUMP",
                "B1 给水泵房稳压供水单元",
                "给排水",
                pumpRoom,
                "给排水班工作人员",
                "WATER-PUMP-B1-01",
                "WATER-PUMP-B1-01",
                WaterOperationsUnitStatus.Warning,
                ["pressure", "flow", "level", "water_quality"],
                "给水压力、流量、液位和水质需要按阈值预警，并联动给水泵巡检与一站式工单调度。",
                BuildSourceEvidence()),
            new WaterOperationsUnit(
                "SEWAGE-SYS-B1-TREATMENT",
                "B1 污水处理站水质监管单元",
                "污水站",
                sewage,
                "给排水班工作人员",
                "SEWAGE-STATION-01",
                "SEWAGE-STATION-01",
                WaterOperationsUnitStatus.Warning,
                ["ph", "cod", "flow"],
                "医疗废水 COD、PH 和流量必须进入水质预警池，并联动污水站巡检、第三方处置和工单调度。",
                BuildSourceEvidence())
        ];
    }

    private static FeatureEvidence[] BuildSourceEvidence() =>
    [
        new FeatureEvidence(
            "给排水/污水站专项运行",
            ["北建院", "中科医信", "PPT"],
            "北建院客户数据定义给排水系统、污水站监测、点位、安装位置、采集字段、角色和使用端；中科医信竞品包含给排水、污水站、巡检保养和工单颗粒度；PPT定义BIM智慧运维集成边界。"),
        new FeatureEvidence(
            "水务告警巡检工单闭环",
            ["北建院", "中科医信", "PPT"],
            "给水压力、流量、液位、水质、污水 COD 和 PH 必须能进入预警池，并与给水泵、污水站资产、巡检任务和工单调度贯通。")
    ];

    private void PersistSeedData()
    {
        if (_persistence is null)
        {
            return;
        }

        foreach (var unit in _units.Values)
        {
            _persistence.SaveUnit(unit);
        }
    }
}
