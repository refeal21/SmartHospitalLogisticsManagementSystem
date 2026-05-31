using SmartHospitalLogistics.Application;
using SmartHospitalLogistics.Domain;

namespace SmartHospitalLogistics.Application.Tests;

public sealed class EnergyPerformanceServiceTests
{
    [Fact]
    public void BoardAggregatesEnergyMetersSpecialtySystemsWorkOrdersAndTraceability()
    {
        var dispatch = new WorkOrderDispatchService();
        var alarms = new MonitoringAlarmService(workOrderIntake: dispatch);
        var iot = new IotIntegrationService(alarmRecorder: alarms);
        var assets = new AssetMaintenanceService(workOrderIntake: dispatch);
        var power = new PowerDistributionService(
            persistence: null,
            iotIntegration: iot,
            assetMaintenance: assets,
            monitoringAlarms: alarms,
            workOrderDispatch: dispatch);
        var hvac = new HvacService(
            persistence: null,
            iotIntegration: iot,
            assetMaintenance: assets,
            monitoringAlarms: alarms,
            workOrderDispatch: dispatch);
        var water = new WaterOperationsService(
            persistence: null,
            iotIntegration: iot,
            assetMaintenance: assets,
            monitoringAlarms: alarms,
            workOrderDispatch: dispatch);
        var service = new EnergyPerformanceService(
            persistence: null,
            powerDistribution: power,
            hvac: hvac,
            waterOperations: water,
            iotIntegration: iot,
            workOrderDispatch: dispatch,
            monitoringAlarms: alarms);

        iot.IngestReading(new TelemetryIngestionCommand(
            "PWR-LV-B1-IN-01",
            "voltage",
            260m,
            "V",
            new DateTimeOffset(2026, 5, 30, 10, 55, 0, TimeSpan.FromHours(8))));

        var board = service.GetBoard();

        Assert.Contains(board.Areas, area => area.AreaCode == "ENE-POWER-B1");
        Assert.Contains(board.Areas, area => area.AreaCode == "ENE-HVAC-B1");
        Assert.Contains(board.Areas, area => area.AreaCode == "ENE-WATER-B1");
        Assert.Contains(board.MeterPoints, point => point.PointCode == "PWR-LV-B1-IN-01");
        Assert.Contains(board.MeterPoints, point => point.PointCode == "HVAC-CHW-B1-02");
        Assert.Contains(board.MeterPoints, point => point.PointCode == "WATER-PUMP-B1-01");
        Assert.True(board.Kpis.TotalEnergyCost > 0);
        Assert.True(board.Kpis.AbnormalAreaCount >= 1);
        Assert.Contains(board.SavingRecommendations, item => item.RelatedAreaCode == "ENE-HVAC-B1");
        Assert.Contains(board.OperationPerformance, item => item.MetricCode == "sla-energy-response");
        Assert.Contains(board.ActiveAlarms, alarm => alarm.PointCode == "PWR-LV-B1-IN-01");
        Assert.Contains(board.SourceEvidence, evidence =>
            evidence.Sources.Contains("中科医信") &&
            evidence.Sources.Contains("PPT"));
        Assert.DoesNotContain(board.SourceEvidence, evidence =>
            evidence.Sources.Count == 1 &&
            evidence.Sources[0].Contains("AI", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void AreaDetailReturnsBimCostTrendRecommendationsAndSourceTraceability()
    {
        var service = new EnergyPerformanceService();

        var detail = service.GetAreaDetail("ENE-HVAC-B1");

        Assert.NotNull(detail);
        Assert.Equal("BIM-ENE-B1-CHILLER", detail.Area.Location.BimElementId);
        Assert.Equal("HVAC-CHW-B1-02", detail.RelatedMeterPoints.Single().PointCode);
        Assert.Contains(detail.Trend, point => point.MetricCode == "energy");
        Assert.Contains(detail.SavingRecommendations, item => item.RelatedAreaCode == "ENE-HVAC-B1");
        Assert.Contains(detail.SourceEvidence, evidence =>
            evidence.Sources.Contains("北建院") &&
            evidence.Sources.Contains("中科医信") &&
            evidence.Sources.Contains("PPT"));
    }
}
