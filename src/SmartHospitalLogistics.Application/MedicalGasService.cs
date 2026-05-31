using SmartHospitalLogistics.Domain;

namespace SmartHospitalLogistics.Application;

public interface IMedicalGasService
{
    MedicalGasBoard GetBoard();

    MedicalGasZoneDetail? GetZoneDetail(string zoneCode);
}

public sealed class MedicalGasService : IMedicalGasService
{
    private static readonly DateTimeOffset SeedTime = new(2026, 5, 30, 9, 30, 0, TimeSpan.FromHours(8));

    private readonly IMedicalGasPersistence? _persistence;
    private readonly IIotIntegrationService _iotIntegration;
    private readonly IAssetMaintenanceService _assetMaintenance;
    private readonly IMonitoringAlarmService _monitoringAlarms;
    private readonly IWorkOrderDispatchService _workOrderDispatch;
    private readonly Dictionary<string, MedicalGasZone> _zones;

    public MedicalGasService(
        IMedicalGasPersistence? persistence = null,
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
        if (persisted is not null && persisted.Zones.Count > 0)
        {
            _zones = persisted.Zones.ToDictionary(zone => zone.ZoneCode, StringComparer.OrdinalIgnoreCase);
        }
        else
        {
            _zones = BuildSeedZones().ToDictionary(zone => zone.ZoneCode, StringComparer.OrdinalIgnoreCase);
            PersistSeedData();
        }
    }

    public MedicalGasBoard GetBoard()
    {
        var catalog = _iotIntegration.GetCatalog();
        var maintenanceBoard = _assetMaintenance.GetMaintenanceBoard();
        var alarmBoard = _monitoringAlarms.GetAlarmBoard();
        var dispatchBoard = _workOrderDispatch.GetDispatchBoard();

        var zones = _zones.Values
            .OrderBy(zone => zone.ZoneCode)
            .ToArray();
        var pointCodes = zones.Select(zone => zone.PressurePointCode).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var assetCodes = zones.Select(zone => zone.ValveAssetCode).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var monitoringPoints = catalog.Points
            .Where(point => point.Category == IotSystemCategory.MedicalGas || pointCodes.Contains(point.PointCode))
            .OrderBy(point => point.PointCode)
            .ToArray();
        var valveAssets = maintenanceBoard.Assets
            .Where(asset => assetCodes.Contains(asset.AssetCode) || IsMedicalGasText(asset.System))
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
            .Where(order => IsMedicalGasWorkOrder(order, zones))
            .OrderByDescending(order => order.Priority)
            .ThenBy(order => order.SlaDueAt)
            .ToArray();
        var enrichedZones = zones
            .Select(zone => zone with
            {
                Status = DetermineStatus(
                    zone,
                    activeAlarms.Where(alarm => string.Equals(alarm.PointCode, zone.PressurePointCode, StringComparison.OrdinalIgnoreCase)),
                    openWorkOrders.Where(order => IsZoneWorkOrder(order, zone)),
                    dueTasks.Where(task => string.Equals(task.AssetCode, zone.ValveAssetCode, StringComparison.OrdinalIgnoreCase)),
                    valveAssets.Where(asset => string.Equals(asset.AssetCode, zone.ValveAssetCode, StringComparison.OrdinalIgnoreCase)))
            })
            .ToArray();

        return new MedicalGasBoard(
            SeedTime,
            enrichedZones,
            monitoringPoints,
            valveAssets,
            activeAlarms,
            openWorkOrders,
            dueTasks,
            BuildSourceEvidence(),
            new MedicalGasBoardKpi(
                enrichedZones.Length,
                enrichedZones.Count(zone => zone.Status != MedicalGasZoneStatus.Normal),
                activeAlarms.Length,
                openWorkOrders.Length,
                dueTasks.Length));
    }

    public MedicalGasZoneDetail? GetZoneDetail(string zoneCode)
    {
        if (!_zones.TryGetValue(zoneCode, out var zone))
        {
            return null;
        }

        var board = GetBoard();
        var enrichedZone = board.Zones.First(item => string.Equals(item.ZoneCode, zone.ZoneCode, StringComparison.OrdinalIgnoreCase));
        var monitoringPoint = _iotIntegration.GetPointDetail(enrichedZone.PressurePointCode);
        var valveAsset = _assetMaintenance.GetAssetMaintenanceDetail(enrichedZone.ValveAssetCode);
        var activeAlarms = board.ActiveAlarms
            .Where(alarm => string.Equals(alarm.PointCode, enrichedZone.PressurePointCode, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        var openWorkOrders = board.OpenWorkOrders
            .Where(order => IsZoneWorkOrder(order, enrichedZone))
            .ToArray();
        var maintenanceTasks = valveAsset?.Tasks
                .Where(task => task.Status != MaintenanceTaskStatus.Completed)
                .OrderByDescending(task => task.Priority)
                .ThenBy(task => task.DueAt)
                .ToArray()
            ?? [];

        return new MedicalGasZoneDetail(
            enrichedZone,
            monitoringPoint,
            valveAsset,
            activeAlarms,
            openWorkOrders,
            maintenanceTasks,
            BuildSourceEvidence());
    }

    private static MedicalGasZoneStatus DetermineStatus(
        MedicalGasZone zone,
        IEnumerable<MonitoringAlarmEvent> activeAlarms,
        IEnumerable<WorkOrder> openWorkOrders,
        IEnumerable<MaintenanceTask> dueTasks,
        IEnumerable<AssetLedgerItem> assets)
    {
        if (activeAlarms.Any(alarm => alarm.RiskLevel == TelemetryRiskLevel.Critical) ||
            openWorkOrders.Any(order => order.Priority == Priority.Critical))
        {
            return MedicalGasZoneStatus.Critical;
        }

        if (dueTasks.Any(task => task.Status == MaintenanceTaskStatus.Overdue) ||
            assets.Any(asset => asset.Status == FacilityStatus.Maintenance))
        {
            return MedicalGasZoneStatus.Maintenance;
        }

        if (activeAlarms.Any() ||
            dueTasks.Any() ||
            assets.Any(asset => asset.Status is FacilityStatus.Warning or FacilityStatus.Fault))
        {
            return MedicalGasZoneStatus.Warning;
        }

        return zone.Status;
    }

    private static bool IsMedicalGasWorkOrder(WorkOrder order, IReadOnlyList<MedicalGasZone> zones) =>
        zones.Any(zone => IsZoneWorkOrder(order, zone));

    private static bool IsZoneWorkOrder(WorkOrder order, MedicalGasZone zone)
    {
        if (!string.Equals(order.Location.BimElementId, zone.Location.BimElementId, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return string.Equals(order.ResponsibleTeam, zone.ResponsibleTeam, StringComparison.OrdinalIgnoreCase) ||
               IsMedicalGasText(order.Title) ||
               IsMedicalGasText(order.ServiceType) ||
               (order.WorkOrderNo.StartsWith("WO-MT-", StringComparison.OrdinalIgnoreCase) && IsMedicalGasText(order.Title)) ||
               order.WorkOrderNo.StartsWith("WO-ALM-MEDGAS", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsMedicalGasText(string value) =>
        value.Contains("医气", StringComparison.OrdinalIgnoreCase) ||
        value.Contains("医用气体", StringComparison.OrdinalIgnoreCase) ||
        value.Contains("medical", StringComparison.OrdinalIgnoreCase) ||
        value.Contains("MEDGAS", StringComparison.OrdinalIgnoreCase);

    private static MedicalGasZone[] BuildSeedZones()
    {
        var ward = new SpatialLocation("同仁亦庄院区", "住院楼", "F8", "眼科病区", "BIM-IPD-F8-WARD");

        return
        [
            new MedicalGasZone(
                "MG-ZONE-IPD-8F",
                "住院 8F 医用气体分区",
                "眼科病区",
                ward,
                [MedicalGasSupplyType.Oxygen, MedicalGasSupplyType.CompressedAir, MedicalGasSupplyType.Vacuum],
                "医气维保人员",
                "MEDGAS-O2-8F",
                "MEDGAS-IPD-8F",
                MedicalGasZoneStatus.Maintenance,
                "氧气压力监测、分区阀箱巡检、异常告警和工单调度必须形成闭环。",
                BuildSourceEvidence())
        ];
    }

    private static FeatureEvidence[] BuildSourceEvidence() =>
    [
        new FeatureEvidence(
            "医用气体专项运行",
            ["北建院", "中科医信", "PPT"],
            "客户调研数据包含医用气体系统、传感器、安装位置和采集字段；竞品覆盖医气专项、巡检和告警处置；PPT定义 BIM 智慧运维集成边界。"),
        new FeatureEvidence(
            "医气告警巡检工单闭环",
            ["北建院", "中科医信", "PPT"],
            "氧气压力、分区阀箱、巡检保养、物联告警和一站式工单调度必须按空间位置和责任班组贯通。")
    ];

    private void PersistSeedData()
    {
        if (_persistence is null)
        {
            return;
        }

        foreach (var zone in _zones.Values)
        {
            _persistence.SaveZone(zone);
        }
    }
}
