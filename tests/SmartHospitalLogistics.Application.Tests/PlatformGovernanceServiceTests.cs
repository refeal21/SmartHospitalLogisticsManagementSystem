using SmartHospitalLogistics.Application;
using SmartHospitalLogistics.Domain;

namespace SmartHospitalLogistics.Application.Tests;

public sealed class PlatformGovernanceServiceTests
{
    [Fact]
    public void BoardAggregatesQualityContractEnergySafetyWorkOrdersAndTraceability()
    {
        var dispatch = new WorkOrderDispatchService();
        var alarms = new MonitoringAlarmService(workOrderIntake: dispatch);
        var iot = new IotIntegrationService(alarmRecorder: alarms);
        var assets = new AssetMaintenanceService(workOrderIntake: dispatch);
        var energy = new EnergyPerformanceService(
            persistence: null,
            iotIntegration: iot,
            workOrderDispatch: dispatch,
            monitoringAlarms: alarms);
        var safety = new SafetyEmergencyService(
            persistence: null,
            iotIntegration: iot,
            assetMaintenance: assets,
            monitoringAlarms: alarms,
            workOrderDispatch: dispatch);
        var service = new PlatformGovernanceService(
            persistence: null,
            workOrderDispatch: dispatch,
            monitoringAlarms: alarms,
            assetMaintenance: assets,
            energyPerformance: energy,
            safetyEmergency: safety);

        iot.IngestReading(new TelemetryIngestionCommand(
            "FIRE-SMOKE-OPD-1F-01",
            "smoke_density",
            2.1m,
            "obs/m",
            new DateTimeOffset(2026, 5, 30, 11, 10, 0, TimeSpan.FromHours(8))));

        var board = service.GetBoard();

        Assert.Contains(board.Controls, control => control.ControlCode == "GOV-SLA-ONE-STOP");
        Assert.Contains(board.Controls, control => control.ControlCode == "GOV-ENERGY-COST");
        Assert.Contains(board.Controls, control => control.ControlCode == "GOV-SAFETY-EMERGENCY");
        Assert.Contains(board.RelatedAlarms, alarm => alarm.PointCode == "FIRE-SMOKE-OPD-1F-01");
        Assert.True(board.Kpis.ControlCount >= 4);
        Assert.True(board.Kpis.OpenActionCount >= 1);
        Assert.Contains(board.PerformanceMetrics, metric => metric.MetricCode == "governance-closure-rate");
        Assert.Contains(board.SourceEvidence, evidence =>
            evidence.Sources.Contains("PPT") &&
            evidence.Sources.Count >= 2);
        Assert.DoesNotContain(board.SourceEvidence, evidence =>
            evidence.Sources.Count == 1 &&
            evidence.Sources[0].Contains("AI", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void RecordingCorrectiveActionUpdatesControlDetailAndAuditTrail()
    {
        var service = new PlatformGovernanceService();

        var result = service.RecordAction(
            "GOV-SLA-ONE-STOP",
            new CreateGovernanceActionCommand(
                "复盘夜间工单超时并调整值班派工规则",
                "后勤调度员",
                "后勤管理部"));

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Detail);
        Assert.Equal(PlatformGovernanceControlStatus.InProgress, result.Detail.Control.Status);
        Assert.Contains(result.Detail.Actions, action => action.ActionCode.StartsWith("ACT-GOV-SLA-ONE-STOP", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Detail.AuditTrail, entry => entry.Operation == "RecordAction");
    }
}
