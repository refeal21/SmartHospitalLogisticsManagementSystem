using SmartHospitalLogistics.Domain;

namespace SmartHospitalLogistics.Application;

public interface IEnergyPerformanceService
{
    EnergyPerformanceBoard GetBoard();

    EnergyPerformanceAreaDetail? GetAreaDetail(string areaCode);
}

public sealed class EnergyPerformanceService : IEnergyPerformanceService
{
    private static readonly DateTimeOffset SeedTime = new(2026, 5, 30, 9, 30, 0, TimeSpan.FromHours(8));

    private readonly IEnergyPerformancePersistence? _persistence;
    private readonly IPowerDistributionService _powerDistribution;
    private readonly IHvacService _hvac;
    private readonly IWaterOperationsService _waterOperations;
    private readonly IIotIntegrationService _iotIntegration;
    private readonly IWorkOrderDispatchService _workOrderDispatch;
    private readonly IMonitoringAlarmService _monitoringAlarms;
    private readonly Dictionary<string, EnergyPerformanceArea> _areas;

    public EnergyPerformanceService(
        IEnergyPerformancePersistence? persistence = null,
        IPowerDistributionService? powerDistribution = null,
        IHvacService? hvac = null,
        IWaterOperationsService? waterOperations = null,
        IIotIntegrationService? iotIntegration = null,
        IWorkOrderDispatchService? workOrderDispatch = null,
        IMonitoringAlarmService? monitoringAlarms = null)
    {
        _persistence = persistence;
        _workOrderDispatch = workOrderDispatch ?? new WorkOrderDispatchService();
        _monitoringAlarms = monitoringAlarms ?? new MonitoringAlarmService(workOrderIntake: _workOrderDispatch as IWorkOrderIntakeService);
        _iotIntegration = iotIntegration ?? new IotIntegrationService(alarmRecorder: _monitoringAlarms);

        var assetMaintenance = new AssetMaintenanceService(workOrderIntake: _workOrderDispatch as IWorkOrderIntakeService);
        _powerDistribution = powerDistribution ?? new PowerDistributionService(
            iotIntegration: _iotIntegration,
            assetMaintenance: assetMaintenance,
            monitoringAlarms: _monitoringAlarms,
            workOrderDispatch: _workOrderDispatch);
        _hvac = hvac ?? new HvacService(
            iotIntegration: _iotIntegration,
            assetMaintenance: assetMaintenance,
            monitoringAlarms: _monitoringAlarms,
            workOrderDispatch: _workOrderDispatch);
        _waterOperations = waterOperations ?? new WaterOperationsService(
            iotIntegration: _iotIntegration,
            assetMaintenance: assetMaintenance,
            monitoringAlarms: _monitoringAlarms,
            workOrderDispatch: _workOrderDispatch);

        var persisted = _persistence?.Load();
        if (persisted is not null && persisted.Areas.Count > 0)
        {
            _areas = persisted.Areas.ToDictionary(area => area.AreaCode, StringComparer.OrdinalIgnoreCase);
        }
        else
        {
            _areas = BuildSeedAreas().ToDictionary(area => area.AreaCode, StringComparer.OrdinalIgnoreCase);
            PersistSeedData();
        }
    }

    public EnergyPerformanceBoard GetBoard()
    {
        var catalog = _iotIntegration.GetCatalog();
        var alarmBoard = _monitoringAlarms.GetAlarmBoard();
        var dispatchBoard = _workOrderDispatch.GetDispatchBoard();
        var powerBoard = _powerDistribution.GetBoard();
        var hvacBoard = _hvac.GetBoard();
        var waterBoard = _waterOperations.GetBoard();
        var areas = BuildEnrichedAreas(alarmBoard, powerBoard, hvacBoard, waterBoard);
        var pointCodes = areas.Select(area => area.PrimaryMeterPointCode).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var activeAlarms = alarmBoard.Alarms
            .Where(alarm => pointCodes.Contains(alarm.PointCode) && alarm.Status != MonitoringAlarmStatus.Closed)
            .OrderByDescending(alarm => alarm.RiskLevel)
            .ThenByDescending(alarm => alarm.TriggeredAt)
            .ToArray();
        var openWorkOrders = dispatchBoard.WorkOrders
            .Where(order => areas.Any(area => IsAreaWorkOrder(order, area)))
            .OrderByDescending(order => order.Priority)
            .ThenBy(order => order.SlaDueAt)
            .ToArray();
        var recommendations = BuildRecommendations(areas, activeAlarms, openWorkOrders);

        return new EnergyPerformanceBoard(
            SeedTime,
            areas,
            catalog.Points.Where(point => pointCodes.Contains(point.PointCode)).OrderBy(point => point.PointCode).ToArray(),
            activeAlarms,
            openWorkOrders,
            recommendations,
            BuildOperationPerformance(areas, activeAlarms, openWorkOrders),
            BuildSourceEvidence(),
            new EnergyPerformanceKpi(
                areas.Sum(area => area.CurrentConsumption),
                areas.Sum(area => area.CurrentConsumption * area.CostRate),
                areas.Count(area => area.Status is EnergyPerformanceAreaStatus.Warning or EnergyPerformanceAreaStatus.Critical),
                recommendations.Length == 0 ? 0 : Math.Round(recommendations.Average(item => item.ExpectedSavingRate), 1),
                openWorkOrders.Length));
    }

    public EnergyPerformanceAreaDetail? GetAreaDetail(string areaCode)
    {
        var board = GetBoard();
        var area = board.Areas.FirstOrDefault(item => string.Equals(item.AreaCode, areaCode, StringComparison.OrdinalIgnoreCase));
        if (area is null)
        {
            return null;
        }

        return new EnergyPerformanceAreaDetail(
            area,
            board.MeterPoints.Where(point => string.Equals(point.PointCode, area.PrimaryMeterPointCode, StringComparison.OrdinalIgnoreCase)).ToArray(),
            board.ActiveAlarms.Where(alarm => string.Equals(alarm.PointCode, area.PrimaryMeterPointCode, StringComparison.OrdinalIgnoreCase)).ToArray(),
            board.OpenWorkOrders.Where(order => IsAreaWorkOrder(order, area)).ToArray(),
            BuildTrend(area),
            board.SavingRecommendations.Where(item => string.Equals(item.RelatedAreaCode, area.AreaCode, StringComparison.OrdinalIgnoreCase)).ToArray(),
            board.OperationPerformance,
            BuildSourceEvidence());
    }

    private EnergyPerformanceArea[] BuildEnrichedAreas(
        MonitoringAlarmBoard alarmBoard,
        PowerDistributionBoard powerBoard,
        HvacBoard hvacBoard,
        WaterOperationsBoard waterBoard)
    {
        return _areas.Values
            .OrderBy(area => area.AreaCode)
            .Select(area =>
            {
                var hasAlarm = alarmBoard.Alarms.Any(alarm =>
                    string.Equals(alarm.PointCode, area.PrimaryMeterPointCode, StringComparison.OrdinalIgnoreCase) &&
                    alarm.Status != MonitoringAlarmStatus.Closed);
                var relatedAbnormal =
                    powerBoard.Circuits.Any(circuit => string.Equals(circuit.MeterPointCode, area.PrimaryMeterPointCode, StringComparison.OrdinalIgnoreCase) && circuit.Status != PowerDistributionCircuitStatus.Normal) ||
                    hvacBoard.Loops.Any(loop => string.Equals(loop.MonitoringPointCode, area.PrimaryMeterPointCode, StringComparison.OrdinalIgnoreCase) && loop.Status != HvacLoopStatus.Normal) ||
                    waterBoard.Units.Any(unit => string.Equals(unit.MonitoringPointCode, area.PrimaryMeterPointCode, StringComparison.OrdinalIgnoreCase) && unit.Status != WaterOperationsUnitStatus.Normal);
                var overBaseline = area.CurrentConsumption > area.BaselineConsumption * 1.08m;
                var status = hasAlarm
                    ? EnergyPerformanceAreaStatus.Critical
                    : relatedAbnormal || overBaseline
                        ? EnergyPerformanceAreaStatus.Warning
                        : area.Status;

                return area with { Status = status };
            })
            .ToArray();
    }

    private static bool IsAreaWorkOrder(WorkOrder order, EnergyPerformanceArea area) =>
        string.Equals(order.Location.BimElementId, area.Location.BimElementId, StringComparison.OrdinalIgnoreCase) &&
        (string.Equals(order.ResponsibleTeam, area.ResponsibleTeam, StringComparison.OrdinalIgnoreCase) ||
         order.Title.Contains(area.Name, StringComparison.OrdinalIgnoreCase) ||
         order.ServiceType.Contains("能耗", StringComparison.OrdinalIgnoreCase) ||
         order.ServiceType.Contains("告警", StringComparison.OrdinalIgnoreCase));

    private static EnergySavingRecommendation[] BuildRecommendations(
        IReadOnlyList<EnergyPerformanceArea> areas,
        IReadOnlyList<MonitoringAlarmEvent> activeAlarms,
        IReadOnlyList<WorkOrder> openWorkOrders) =>
        areas
            .Where(area => area.Status != EnergyPerformanceAreaStatus.Normal || area.CurrentConsumption > area.BaselineConsumption)
            .Select(area =>
            {
                var hasAlarm = activeAlarms.Any(alarm => string.Equals(alarm.PointCode, area.PrimaryMeterPointCode, StringComparison.OrdinalIgnoreCase));
                var hasWorkOrder = openWorkOrders.Any(order => IsAreaWorkOrder(order, area));
                var savingRate = Math.Max(3m, Math.Round((area.CurrentConsumption - area.BaselineConsumption) / area.BaselineConsumption * 100m, 1));
                return new EnergySavingRecommendation(
                    $"REC-{area.AreaCode}",
                    area.AreaCode,
                    area.EnergyType == "冷量" ? "冷站夜间节能策略复核" : $"{area.Name}异常用能复核",
                    hasAlarm ? Priority.Critical : Priority.High,
                    savingRate,
                    hasWorkOrder ? "已有工单闭环，建议复核处置后能耗回落。" : "专项运行状态或用能偏离基线，建议联动巡检和调度。",
                    ["中科医信", "PPT"]);
            })
            .OrderByDescending(item => item.Priority)
            .ToArray();

    private static OperationPerformanceMetric[] BuildOperationPerformance(
        IReadOnlyList<EnergyPerformanceArea> areas,
        IReadOnlyList<MonitoringAlarmEvent> activeAlarms,
        IReadOnlyList<WorkOrder> openWorkOrders) =>
    [
        new OperationPerformanceMetric(
            "energy-saving-potential",
            "节能潜力",
            Math.Round(areas.Sum(area => Math.Max(0, area.CurrentConsumption - area.BaselineConsumption)) / areas.Sum(area => area.BaselineConsumption) * 100m, 1),
            "%",
            "控制在 5% 以内",
            areas.Any(area => area.CurrentConsumption > area.BaselineConsumption * 1.08m) ? EnergyPerformanceAreaStatus.Warning : EnergyPerformanceAreaStatus.Normal),
        new OperationPerformanceMetric(
            "sla-energy-response",
            "能耗异常响应",
            activeAlarms.Count == 0 ? 100 : Math.Max(60, 100 - activeAlarms.Count * 12 - openWorkOrders.Count * 5),
            "%",
            "目标 >= 90%",
            activeAlarms.Count > 0 ? EnergyPerformanceAreaStatus.Warning : EnergyPerformanceAreaStatus.Normal)
    ];

    private static EnergyTrendPoint[] BuildTrend(EnergyPerformanceArea area) =>
    [
        new EnergyTrendPoint(SeedTime.AddHours(-3), area.AreaCode, area.EnergyType == "冷量" ? "energy" : "consumption", Math.Round(area.BaselineConsumption * 0.92m, 1), "kWh"),
        new EnergyTrendPoint(SeedTime.AddHours(-2), area.AreaCode, area.EnergyType == "冷量" ? "energy" : "consumption", Math.Round(area.BaselineConsumption * 0.98m, 1), "kWh"),
        new EnergyTrendPoint(SeedTime.AddHours(-1), area.AreaCode, area.EnergyType == "冷量" ? "energy" : "consumption", area.CurrentConsumption, "kWh")
    ];

    private static EnergyPerformanceArea[] BuildSeedAreas()
    {
        var powerRoom = new SpatialLocation("同仁亦庄院区", "能源中心", "B1", "变配电室", "BIM-ENE-B1-PDU");
        var chillerRoom = new SpatialLocation("同仁亦庄院区", "能源中心", "B1", "冷站机房", "BIM-ENE-B1-CHILLER");
        var pumpRoom = new SpatialLocation("同仁亦庄院区", "能源中心", "B1", "给水泵房", "BIM-ENE-B1-PUMP");

        return
        [
            new EnergyPerformanceArea(
                "ENE-POWER-B1",
                "B1 变配电室电力能耗",
                "电",
                powerRoom,
                "电工班工作人员",
                "PWR-LV-B1-IN-01",
                "PWR-CIRCUIT-B1-LV-IN",
                980m,
                1088m,
                0.92m,
                EnergyPerformanceAreaStatus.Warning,
                BuildSourceEvidence()),
            new EnergyPerformanceArea(
                "ENE-HVAC-B1",
                "B1 冷站冷量与泵组能耗",
                "冷量",
                chillerRoom,
                "暖通班工作人员",
                "HVAC-CHW-B1-02",
                "HVAC-LOOP-B1-CHW",
                640m,
                712m,
                0.86m,
                EnergyPerformanceAreaStatus.Optimizing,
                BuildSourceEvidence()),
            new EnergyPerformanceArea(
                "ENE-WATER-B1",
                "B1 给水与污水运行能耗",
                "水",
                pumpRoom,
                "给排水班工作人员",
                "WATER-PUMP-B1-01",
                "WATER-SYS-B1-PUMP",
                220m,
                238m,
                0.38m,
                EnergyPerformanceAreaStatus.Warning,
                BuildSourceEvidence())
        ];
    }

    private static FeatureEvidence[] BuildSourceEvidence() =>
    [
        new FeatureEvidence(
            "综合能耗监管",
            ["中科医信", "PPT"],
            "中科医信竞品包含用能总览、实时监控、告警、用能分析、报表、成本和配置；PPT 将能耗成本管理纳入综合管理闭环。"),
        new FeatureEvidence(
            "客户专项能耗映射",
            ["北建院", "中科医信", "PPT"],
            "北建院强电、暖通、给排水点位包含电度、能耗、压力、流量和水质字段，必须与专项告警、工单和运行绩效联动。")
    ];

    private void PersistSeedData()
    {
        if (_persistence is null)
        {
            return;
        }

        foreach (var area in _areas.Values)
        {
            _persistence.SaveArea(area);
        }
    }
}
