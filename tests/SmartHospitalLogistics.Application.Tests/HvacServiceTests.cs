using SmartHospitalLogistics.Application;
using SmartHospitalLogistics.Domain;

namespace SmartHospitalLogistics.Application.Tests;

public sealed class HvacServiceTests
{
    [Fact]
    public void BoardAggregatesHvacPointAssetMaintenanceAlarmWorkOrderAndTraceability()
    {
        var dispatch = new WorkOrderDispatchService();
        var alarms = new MonitoringAlarmService(workOrderIntake: dispatch);
        var iot = new IotIntegrationService(alarmRecorder: alarms);
        var assets = new AssetMaintenanceService(workOrderIntake: dispatch);
        var service = new HvacService(
            persistence: null,
            iotIntegration: iot,
            assetMaintenance: assets,
            monitoringAlarms: alarms,
            workOrderDispatch: dispatch);

        var ingestion = iot.IngestReading(new TelemetryIngestionCommand(
            "HVAC-CHW-B1-02",
            "pressure",
            0.8m,
            "MPa",
            new DateTimeOffset(2026, 5, 30, 10, 35, 0, TimeSpan.FromHours(8))));
        Assert.True(ingestion.Succeeded, ingestion.ErrorMessage);

        var alarm = Assert.Single(alarms.GetAlarmBoard().Alarms, item => item.PointCode == "HVAC-CHW-B1-02");
        var converted = alarms.ConvertToWorkOrder(
            alarm.AlarmNo,
            new ConvertAlarmToWorkOrderCommand("hvac-dispatcher", "暖通班工作人员", "chilled water pressure critical"));
        Assert.True(converted.Succeeded, converted.ErrorMessage);

        var board = service.GetBoard();

        var loop = Assert.Single(board.Loops, item => item.LoopCode == "HVAC-LOOP-B1-CHW");
        Assert.Equal("HVAC-CHW-B1-02", loop.MonitoringPointCode);
        Assert.Equal("CHW-B1-02", loop.AssetCode);
        Assert.Equal("BIM-ENE-B1-CHILLER", loop.Location.BimElementId);
        Assert.Equal("暖通班工作人员", loop.ResponsibleTeam);
        Assert.Contains(loop.SourceEvidence, evidence =>
            evidence.Sources.Contains("北建院") &&
            evidence.Sources.Contains("中科医信") &&
            evidence.Sources.Contains("PPT"));

        Assert.Contains(board.MonitoringPoints, point => point.PointCode == "HVAC-CHW-B1-02");
        Assert.Contains(board.HvacAssets, asset => asset.AssetCode == "CHW-B1-02");
        Assert.Contains(board.ActiveAlarms, item => item.PointCode == "HVAC-CHW-B1-02");
        Assert.Contains(board.OpenWorkOrders, item => item.WorkOrderNo.StartsWith("WO-ALM-", StringComparison.Ordinal));
        Assert.Contains(board.DueMaintenanceTasks, task => task.TaskNo == "MT-20260530-0003");
        Assert.True(board.Kpis.AbnormalLoops >= 1);
        Assert.True(board.Kpis.ActiveAlarms >= 1);
        Assert.DoesNotContain(board.SourceEvidence, evidence =>
            evidence.Sources.Count == 1 &&
            evidence.Sources[0].Contains("AI", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void LoopDetailReturnsBimLocationSourceTraceabilityAndLinkedOperationalRecords()
    {
        var dispatch = new WorkOrderDispatchService();
        var alarms = new MonitoringAlarmService(workOrderIntake: dispatch);
        var iot = new IotIntegrationService(alarmRecorder: alarms);
        var assets = new AssetMaintenanceService(workOrderIntake: dispatch);
        var service = new HvacService(
            persistence: null,
            iotIntegration: iot,
            assetMaintenance: assets,
            monitoringAlarms: alarms,
            workOrderDispatch: dispatch);

        var detail = service.GetLoopDetail("HVAC-LOOP-B1-CHW");

        Assert.NotNull(detail);
        Assert.Equal("BIM-ENE-B1-CHILLER", detail.Loop.Location.BimElementId);
        Assert.Equal("HVAC-CHW-B1-02", detail.MonitoringPoint?.Point.PointCode);
        Assert.Equal("CHW-B1-02", detail.HvacAsset?.Asset.AssetCode);
        Assert.Contains(detail.MaintenanceTasks, task => task.TaskNo == "MT-20260530-0003");
        Assert.Contains(detail.SourceEvidence, evidence =>
            evidence.Sources.Contains("北建院") &&
            evidence.Sources.Contains("中科医信") &&
            evidence.Sources.Contains("PPT"));
    }
}
