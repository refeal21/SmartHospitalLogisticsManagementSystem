using SmartHospitalLogistics.Domain;

namespace SmartHospitalLogistics.Application;

public interface IIotIntegrationService
{
    IotIntegrationCatalog GetCatalog();

    IotPointDetail? GetPointDetail(string pointCode);

    TelemetryIngestionResult IngestReading(TelemetryIngestionCommand command);
}

public sealed class IotIntegrationService : IIotIntegrationService
{
    private static readonly DateTimeOffset SeedTime = new(2026, 5, 30, 9, 30, 0, TimeSpan.FromHours(8));

    private readonly Dictionary<string, IotMonitoringPoint> _points;

    private readonly IReadOnlyList<IotSystemProfile> _systems;

    private readonly IReadOnlyList<TelemetryThresholdRule> _thresholdRules;

    private readonly Dictionary<string, List<TelemetryReading>> _readings;

    public IotIntegrationService()
    {
        _systems = BuildSystems();
        _points = BuildPoints().ToDictionary(point => point.PointCode, StringComparer.OrdinalIgnoreCase);
        _thresholdRules = BuildThresholdRules();
        _readings = BuildReadings()
            .GroupBy(reading => reading.PointCode, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.OrderByDescending(item => item.CollectedAt).ToList(), StringComparer.OrdinalIgnoreCase);
    }

    public IotIntegrationCatalog GetCatalog() =>
        new(
            SeedTime,
            _systems,
            _points.Values
                .OrderBy(point => point.Category)
                .ThenBy(point => point.PointCode)
                .ToArray(),
            _thresholdRules,
            BuildSourceEvidence());

    public IotPointDetail? GetPointDetail(string pointCode)
    {
        if (!_points.TryGetValue(pointCode, out var point))
        {
            return null;
        }

        return new IotPointDetail(
            point,
            _thresholdRules.Where(rule => string.Equals(rule.PointCode, point.PointCode, StringComparison.OrdinalIgnoreCase)).ToArray(),
            GetRecentReadings(point.PointCode),
            BuildSourceEvidence());
    }

    public TelemetryIngestionResult IngestReading(TelemetryIngestionCommand command)
    {
        if (!_points.TryGetValue(command.PointCode, out var point))
        {
            return new TelemetryIngestionResult(
                false,
                $"Monitoring point {command.PointCode} was not found.",
                command.PointCode,
                command.MetricCode,
                TelemetryRiskLevel.Normal,
                "点位不存在",
                NotFound: true);
        }

        if (!point.Metrics.Any(metric => string.Equals(metric.Code, command.MetricCode, StringComparison.OrdinalIgnoreCase)))
        {
            return new TelemetryIngestionResult(
                false,
                $"Metric {command.MetricCode} is not configured for point {command.PointCode}.",
                command.PointCode,
                command.MetricCode,
                TelemetryRiskLevel.Normal,
                "指标未配置");
        }

        var rule = _thresholdRules.FirstOrDefault(item =>
            string.Equals(item.PointCode, command.PointCode, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(item.MetricCode, command.MetricCode, StringComparison.OrdinalIgnoreCase));
        var risk = rule is null ? TelemetryRiskLevel.Normal : EvaluateRisk(command.Value, rule);
        var ruleSummary = rule is null ? "未配置阈值，按正常读数入库" : BuildReadingSummary(command.Value, rule, risk);
        var reading = new TelemetryReading(
            command.PointCode,
            command.MetricCode,
            command.Value,
            command.Unit,
            command.CollectedAt,
            risk,
            ruleSummary);

        if (!_readings.TryGetValue(command.PointCode, out var readings))
        {
            readings = [];
            _readings[command.PointCode] = readings;
        }

        readings.Insert(0, reading);
        return new TelemetryIngestionResult(true, null, command.PointCode, command.MetricCode, risk, ruleSummary, reading);
    }

    private IReadOnlyList<TelemetryReading> GetRecentReadings(string pointCode) =>
        _readings.TryGetValue(pointCode, out var readings)
            ? readings.OrderByDescending(reading => reading.CollectedAt).Take(12).ToArray()
            : [];

    private static TelemetryRiskLevel EvaluateRisk(decimal value, TelemetryThresholdRule rule) =>
        rule.Direction switch
        {
            ThresholdDirection.Above when rule.CriticalMax.HasValue && value > rule.CriticalMax.Value => TelemetryRiskLevel.Critical,
            ThresholdDirection.Above when rule.WarningMax.HasValue && value > rule.WarningMax.Value => TelemetryRiskLevel.Warning,
            ThresholdDirection.Below when rule.CriticalMin.HasValue && value < rule.CriticalMin.Value => TelemetryRiskLevel.Critical,
            ThresholdDirection.Below when rule.WarningMin.HasValue && value < rule.WarningMin.Value => TelemetryRiskLevel.Warning,
            ThresholdDirection.OutsideRange when IsOutside(value, rule.CriticalMin, rule.CriticalMax) => TelemetryRiskLevel.Critical,
            ThresholdDirection.OutsideRange when IsOutside(value, rule.WarningMin, rule.WarningMax) => TelemetryRiskLevel.Warning,
            _ => TelemetryRiskLevel.Normal
        };

    private static bool IsOutside(decimal value, decimal? min, decimal? max) =>
        (min.HasValue && value < min.Value) || (max.HasValue && value > max.Value);

    private static string BuildReadingSummary(decimal value, TelemetryThresholdRule rule, TelemetryRiskLevel risk)
    {
        if (risk == TelemetryRiskLevel.Normal)
        {
            return $"{rule.RuleSummary}；当前值 {value} 正常";
        }

        var direction = rule.Direction switch
        {
            ThresholdDirection.Above => "高于",
            ThresholdDirection.Below => "低于",
            _ => "超出"
        };

        return $"{rule.RuleSummary}；当前值 {value} {direction}阈值";
    }

    private static IotSystemProfile[] BuildSystems() =>
    [
        new IotSystemProfile(IotSystemCategory.StrongElectric, "强电系统", ["变配电智能配电", "多功能远传电表", "照明"], ["电工班工作人员", "总务处管理者"], ["控制室电脑端", "移动端"]),
        new IotSystemProfile(IotSystemCategory.Hvac, "供暖空调系统", ["冷热源", "空调水", "空气处理机组", "新风机组"], ["暖通班工作人员", "第三方服务方"], ["控制室电脑端", "移动端"]),
        new IotSystemProfile(IotSystemCategory.WaterSupplyDrainage, "给排水系统", ["给水", "热水", "中水", "排水", "消防水"], ["给排水班工作人员", "总务处管理者"], ["控制室电脑端", "移动端"]),
        new IotSystemProfile(IotSystemCategory.MedicalGas, "医用气体系统", ["氧气", "压缩空气", "负压真空", "汇流排"], ["医气维保人员", "总务处管理者"], ["控制室电脑端", "移动端"]),
        new IotSystemProfile(IotSystemCategory.EnvironmentQuality, "环境质量系统", ["室内空气质量", "CO2", "温湿度"], ["环境监管班组", "医院管理者"], ["控制室电脑端", "移动端"]),
        new IotSystemProfile(IotSystemCategory.Sewage, "污水站监测", ["医疗废水", "污水处理站", "水质监测"], ["给排水班工作人员", "第三方服务方"], ["控制室电脑端", "移动端"])
    ];

    private static IotMonitoringPoint[] BuildPoints()
    {
        var energyRoom = new SpatialLocation("同仁亦庄院区", "能源中心", "B1", "变配电室", "BIM-ENE-B1-PDU");
        var chillerRoom = new SpatialLocation("同仁亦庄院区", "能源中心", "B1", "冷站机房", "BIM-ENE-B1-CHILLER");
        var pumpRoom = new SpatialLocation("同仁亦庄院区", "能源中心", "B1", "给水泵房", "BIM-ENE-B1-PUMP");
        var ward = new SpatialLocation("同仁亦庄院区", "住院楼", "F8", "眼科病区", "BIM-IPD-F8-WARD");
        var sewage = new SpatialLocation("同仁亦庄院区", "后勤楼", "B1", "污水处理站", "BIM-LOG-B1-SEWAGE");

        return
        [
            new IotMonitoringPoint(
                "PWR-LV-B1-IN-01",
                "B1 低压进线柜多功能电表",
                IotSystemCategory.StrongElectric,
                energyRoom,
                "METER-LV-001",
                "modbus-adapter",
                [
                    new IotMetricDefinition("voltage", "电压", "V", "decimal", "电压"),
                    new IotMetricDefinition("current", "电流", "A", "decimal", "电流"),
                    new IotMetricDefinition("active_power", "有功功率", "kW", "decimal", "有功功率"),
                    new IotMetricDefinition("power_factor", "功率因数", "", "decimal", "功率因数"),
                    new IotMetricDefinition("frequency", "频率", "Hz", "decimal", "频率"),
                    new IotMetricDefinition("kwh", "电度", "kWh", "decimal", "电度"),
                    new IotMetricDefinition("harmonic", "谐波", "%", "decimal", "谐波")
                ],
                BuildSourceEvidence()),
            new IotMonitoringPoint(
                "HVAC-CHW-B1-02",
                "冷站 2 号冷冻泵运行点",
                IotSystemCategory.Hvac,
                chillerRoom,
                "CHW-P-02",
                "bacnet-adapter",
                [
                    new IotMetricDefinition("supply_temp", "供水温度", "C", "decimal", "供回水温"),
                    new IotMetricDefinition("pressure", "压力", "MPa", "decimal", "压力"),
                    new IotMetricDefinition("flow", "流量", "m3/h", "decimal", "流量"),
                    new IotMetricDefinition("energy", "能耗", "kWh", "decimal", "能耗")
                ],
                BuildSourceEvidence()),
            new IotMonitoringPoint(
                "WATER-PUMP-B1-01",
                "B1 给水泵房压力点",
                IotSystemCategory.WaterSupplyDrainage,
                pumpRoom,
                "WATER-PUMP-001",
                "modbus-adapter",
                [
                    new IotMetricDefinition("pressure", "压力", "MPa", "decimal", "压力"),
                    new IotMetricDefinition("flow", "流量", "m3/h", "decimal", "流量"),
                    new IotMetricDefinition("level", "液位", "m", "decimal", "液位"),
                    new IotMetricDefinition("water_quality", "水质", "index", "decimal", "水质")
                ],
                BuildSourceEvidence()),
            new IotMonitoringPoint(
                "MEDGAS-O2-8F",
                "住院 8F 氧气压力监测点",
                IotSystemCategory.MedicalGas,
                ward,
                "MEDGAS-O2-8F",
                "medical-gas-adapter",
                [
                    new IotMetricDefinition("pressure", "氧气压力", "MPa", "decimal", "医气压力"),
                    new IotMetricDefinition("flow", "氧气流量", "m3/h", "decimal", "医气流量"),
                    new IotMetricDefinition("alarm_status", "报警状态", "", "enum", "报警状态")
                ],
                BuildSourceEvidence()),
            new IotMonitoringPoint(
                "ENV-WARD-CO2-8F",
                "住院 8F CO2 与温湿度点",
                IotSystemCategory.EnvironmentQuality,
                ward,
                "ENV-CO2-8F",
                "iot-http-adapter",
                [
                    new IotMetricDefinition("co2", "CO2", "ppm", "decimal", "CO2浓度"),
                    new IotMetricDefinition("temperature", "温度", "C", "decimal", "温度"),
                    new IotMetricDefinition("humidity", "湿度", "%", "decimal", "湿度")
                ],
                BuildSourceEvidence()),
            new IotMonitoringPoint(
                "SEWAGE-STATION-01",
                "污水处理站综合水质点",
                IotSystemCategory.Sewage,
                sewage,
                "SEWAGE-001",
                "opcua-adapter",
                [
                    new IotMetricDefinition("ph", "PH", "", "decimal", "水质"),
                    new IotMetricDefinition("cod", "COD", "mg/L", "decimal", "医疗废水指标"),
                    new IotMetricDefinition("flow", "流量", "m3/h", "decimal", "流量")
                ],
                BuildSourceEvidence())
        ];
    }

    private static TelemetryThresholdRule[] BuildThresholdRules() =>
    [
        new TelemetryThresholdRule("PWR-LV-B1-IN-01", "voltage", ThresholdDirection.OutsideRange, 200m, 245m, 190m, 255m, "低压进线电压需保持在安全范围"),
        new TelemetryThresholdRule("HVAC-CHW-B1-02", "pressure", ThresholdDirection.OutsideRange, 0.25m, 0.65m, 0.18m, 0.75m, "冷冻泵压力需保持稳定"),
        new TelemetryThresholdRule("WATER-PUMP-B1-01", "pressure", ThresholdDirection.Below, 0.32m, null, 0.26m, null, "给水压力低于阈值影响供水安全"),
        new TelemetryThresholdRule("MEDGAS-O2-8F", "pressure", ThresholdDirection.Below, 0.38m, null, 0.35m, null, "医用氧气压力低于阈值需告警处置"),
        new TelemetryThresholdRule("ENV-WARD-CO2-8F", "co2", ThresholdDirection.Above, null, 1000m, null, 1200m, "住院病区 CO2 高于阈值需通风联动"),
        new TelemetryThresholdRule("SEWAGE-STATION-01", "cod", ThresholdDirection.Above, null, 150m, null, 220m, "医疗废水 COD 超阈值需污水站处置")
    ];

    private static TelemetryReading[] BuildReadings() =>
    [
        new TelemetryReading("PWR-LV-B1-IN-01", "voltage", 228m, "V", SeedTime.AddMinutes(-4), TelemetryRiskLevel.Normal, "低压进线电压正常"),
        new TelemetryReading("MEDGAS-O2-8F", "pressure", 0.39m, "MPa", SeedTime.AddMinutes(-5), TelemetryRiskLevel.Normal, "医用氧气压力正常"),
        new TelemetryReading("ENV-WARD-CO2-8F", "co2", 960m, "ppm", SeedTime.AddMinutes(-3), TelemetryRiskLevel.Normal, "住院病区 CO2 正常"),
        new TelemetryReading("SEWAGE-STATION-01", "cod", 138m, "mg/L", SeedTime.AddMinutes(-8), TelemetryRiskLevel.Normal, "污水站 COD 正常")
    ];

    private static FeatureEvidence[] BuildSourceEvidence() =>
    [
        new FeatureEvidence(
            "客户物联数据接入模型",
            ["北建院", "PPT"],
            "北建院调研数据定义系统、传感器、安装位置、可采集字段、希望采集字段、角色和使用端；PPT 定义 BIM 智慧运维集成边界。"),
        new FeatureEvidence(
            "供配电监测",
            ["北建院", "中科医信", "PPT"],
            "客户数据包含电压、电流、功率、功率因数、频率、电度、谐波等字段；竞品和 PPT 均要求供配电监测。"),
        new FeatureEvidence(
            "医用气体监测",
            ["北建院", "中科医信", "PPT"],
            "客户数据和竞品功能均覆盖氧气、压缩空气、负压真空、压力流量与报警处置。"),
        new FeatureEvidence(
            "环境质量与污水站监测",
            ["北建院", "中科医信", "PPT"],
            "客户调研涉及 CO2、温湿度、污水和给排水字段；竞品功能包含环境质量和污水站实时监测。")
    ];
}
