using SmartHospitalLogistics.Domain;

namespace SmartHospitalLogistics.Application;

public interface IPowerDistributionService
{
    PowerDistributionBoard GetBoard();

    PowerDistributionCircuitDetail? GetCircuitDetail(string circuitCode);
}

public sealed class PowerDistributionService : IPowerDistributionService
{
    private static readonly DateTimeOffset SeedTime = new(2026, 5, 30, 9, 30, 0, TimeSpan.FromHours(8));

    private readonly IPowerDistributionPersistence? _persistence;
    private readonly IIotIntegrationService _iotIntegration;
    private readonly IAssetMaintenanceService _assetMaintenance;
    private readonly IMonitoringAlarmService _monitoringAlarms;
    private readonly IWorkOrderDispatchService _workOrderDispatch;
    private readonly Dictionary<string, PowerDistributionCircuit> _circuits;

    public PowerDistributionService(
        IPowerDistributionPersistence? persistence = null,
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
        if (persisted is not null && persisted.Circuits.Count > 0)
        {
            _circuits = persisted.Circuits.ToDictionary(circuit => circuit.CircuitCode, StringComparer.OrdinalIgnoreCase);
        }
        else
        {
            _circuits = BuildSeedCircuits().ToDictionary(circuit => circuit.CircuitCode, StringComparer.OrdinalIgnoreCase);
            PersistSeedData();
        }
    }

    public PowerDistributionBoard GetBoard()
    {
        var catalog = _iotIntegration.GetCatalog();
        var maintenanceBoard = _assetMaintenance.GetMaintenanceBoard();
        var alarmBoard = _monitoringAlarms.GetAlarmBoard();
        var dispatchBoard = _workOrderDispatch.GetDispatchBoard();

        var circuits = _circuits.Values
            .OrderBy(circuit => circuit.CircuitCode)
            .ToArray();
        var pointCodes = circuits.Select(circuit => circuit.MeterPointCode).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var assetCodes = circuits.Select(circuit => circuit.AssetCode).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var monitoringPoints = catalog.Points
            .Where(point => point.Category == IotSystemCategory.StrongElectric || pointCodes.Contains(point.PointCode))
            .OrderBy(point => point.PointCode)
            .ToArray();
        var electricalAssets = maintenanceBoard.Assets
            .Where(asset => assetCodes.Contains(asset.AssetCode) || IsPowerText(asset.System))
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
            .Where(order => IsPowerWorkOrder(order, circuits))
            .OrderByDescending(order => order.Priority)
            .ThenBy(order => order.SlaDueAt)
            .ToArray();
        var enrichedCircuits = circuits
            .Select(circuit => circuit with
            {
                Status = DetermineStatus(
                    circuit,
                    activeAlarms.Where(alarm => string.Equals(alarm.PointCode, circuit.MeterPointCode, StringComparison.OrdinalIgnoreCase)),
                    openWorkOrders.Where(order => IsCircuitWorkOrder(order, circuit)),
                    dueTasks.Where(task => string.Equals(task.AssetCode, circuit.AssetCode, StringComparison.OrdinalIgnoreCase)),
                    electricalAssets.Where(asset => string.Equals(asset.AssetCode, circuit.AssetCode, StringComparison.OrdinalIgnoreCase)))
            })
            .ToArray();

        return new PowerDistributionBoard(
            SeedTime,
            enrichedCircuits,
            monitoringPoints,
            electricalAssets,
            activeAlarms,
            openWorkOrders,
            dueTasks,
            BuildSourceEvidence(),
            new PowerDistributionBoardKpi(
                enrichedCircuits.Length,
                enrichedCircuits.Count(circuit => circuit.Status != PowerDistributionCircuitStatus.Normal),
                activeAlarms.Length,
                openWorkOrders.Length,
                dueTasks.Length));
    }

    public PowerDistributionCircuitDetail? GetCircuitDetail(string circuitCode)
    {
        if (!_circuits.TryGetValue(circuitCode, out var circuit))
        {
            return null;
        }

        var board = GetBoard();
        var enrichedCircuit = board.Circuits.First(item => string.Equals(item.CircuitCode, circuit.CircuitCode, StringComparison.OrdinalIgnoreCase));
        var monitoringPoint = _iotIntegration.GetPointDetail(enrichedCircuit.MeterPointCode);
        var electricalAsset = _assetMaintenance.GetAssetMaintenanceDetail(enrichedCircuit.AssetCode);
        var activeAlarms = board.ActiveAlarms
            .Where(alarm => string.Equals(alarm.PointCode, enrichedCircuit.MeterPointCode, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        var openWorkOrders = board.OpenWorkOrders
            .Where(order => IsCircuitWorkOrder(order, enrichedCircuit))
            .ToArray();
        var maintenanceTasks = electricalAsset?.Tasks
                .Where(task => task.Status != MaintenanceTaskStatus.Completed)
                .OrderByDescending(task => task.Priority)
                .ThenBy(task => task.DueAt)
                .ToArray()
            ?? [];

        return new PowerDistributionCircuitDetail(
            enrichedCircuit,
            monitoringPoint,
            electricalAsset,
            activeAlarms,
            openWorkOrders,
            maintenanceTasks,
            BuildSourceEvidence());
    }

    private static PowerDistributionCircuitStatus DetermineStatus(
        PowerDistributionCircuit circuit,
        IEnumerable<MonitoringAlarmEvent> activeAlarms,
        IEnumerable<WorkOrder> openWorkOrders,
        IEnumerable<MaintenanceTask> dueTasks,
        IEnumerable<AssetLedgerItem> assets)
    {
        if (activeAlarms.Any(alarm => alarm.RiskLevel == TelemetryRiskLevel.Critical) ||
            openWorkOrders.Any(order => order.Priority == Priority.Critical))
        {
            return PowerDistributionCircuitStatus.Critical;
        }

        if (dueTasks.Any(task => task.Status == MaintenanceTaskStatus.Overdue) ||
            assets.Any(asset => asset.Status == FacilityStatus.Maintenance))
        {
            return PowerDistributionCircuitStatus.Maintenance;
        }

        if (activeAlarms.Any() ||
            dueTasks.Any() ||
            assets.Any(asset => asset.Status is FacilityStatus.Warning or FacilityStatus.Fault))
        {
            return PowerDistributionCircuitStatus.Warning;
        }

        return circuit.Status;
    }

    private static bool IsPowerWorkOrder(WorkOrder order, IReadOnlyList<PowerDistributionCircuit> circuits) =>
        circuits.Any(circuit => IsCircuitWorkOrder(order, circuit));

    private static bool IsCircuitWorkOrder(WorkOrder order, PowerDistributionCircuit circuit)
    {
        if (!string.Equals(order.Location.BimElementId, circuit.Location.BimElementId, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return string.Equals(order.ResponsibleTeam, circuit.ResponsibleTeam, StringComparison.OrdinalIgnoreCase) ||
               IsPowerText(order.Title) ||
               IsPowerText(order.ServiceType) ||
               order.WorkOrderNo.StartsWith("WO-ALM-PWR", StringComparison.OrdinalIgnoreCase) ||
               (order.WorkOrderNo.StartsWith("WO-MT-", StringComparison.OrdinalIgnoreCase) && IsPowerText(order.Title));
    }

    private static bool IsPowerText(string value) =>
        value.Contains("供配电", StringComparison.OrdinalIgnoreCase) ||
        value.Contains("强电", StringComparison.OrdinalIgnoreCase) ||
        value.Contains("低压", StringComparison.OrdinalIgnoreCase) ||
        value.Contains("配电", StringComparison.OrdinalIgnoreCase) ||
        value.Contains("电压", StringComparison.OrdinalIgnoreCase) ||
        value.Contains("PWR", StringComparison.OrdinalIgnoreCase) ||
        value.Contains("power", StringComparison.OrdinalIgnoreCase);

    private static PowerDistributionCircuit[] BuildSeedCircuits()
    {
        var powerRoom = new SpatialLocation("同仁亦庄院区", "能源中心", "B1", "变配电室", "BIM-ENE-B1-PDU");

        return
        [
            new PowerDistributionCircuit(
                "PWR-CIRCUIT-B1-LV-IN",
                "B1 低压进线柜主进线回路",
                "供配电/强电",
                powerRoom,
                "电工班工作人员",
                "PWR-LV-B1-IN-01",
                "PWR-LV-B1-IN-CAB",
                PowerDistributionCircuitStatus.Warning,
                ["voltage", "current", "active_power", "power_factor", "frequency", "kwh", "harmonic"],
                "低压进线电压、电流、功率因数和谐波需要按阈值预警，并联动配电柜巡检与一站式工单调度。",
                BuildSourceEvidence())
        ];
    }

    private static FeatureEvidence[] BuildSourceEvidence() =>
    [
        new FeatureEvidence(
            "供配电/强电专项运行",
            ["北建院", "中科医信", "PPT"],
            "北建院客户数据定义强电系统、传感器、安装位置、采集字段、角色和使用端；中科医信竞品包含供配电监测管理系统；PPT定义BIM智慧运维集成边界。"),
        new FeatureEvidence(
            "强电告警巡检工单闭环",
            ["北建院", "中科医信", "PPT"],
            "电压、电流、功率、功率因数、频率、电度、谐波等读数必须能进入预警池，并与低压配电资产、巡检任务和工单调度贯通。")
    ];

    private void PersistSeedData()
    {
        if (_persistence is null)
        {
            return;
        }

        foreach (var circuit in _circuits.Values)
        {
            _persistence.SaveCircuit(circuit);
        }
    }
}
