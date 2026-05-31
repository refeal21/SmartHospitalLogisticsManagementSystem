using SmartHospitalLogistics.Application;
using SmartHospitalLogistics.Domain;

namespace SmartHospitalLogistics.Application.Tests;

public sealed class SafetyEmergencyServiceTests
{
    [Fact]
    public void BoardAggregatesFireSecurityPointsAssetsWorkOrdersAndTraceability()
    {
        var dispatch = new WorkOrderDispatchService();
        var alarms = new MonitoringAlarmService(workOrderIntake: dispatch);
        var iot = new IotIntegrationService(alarmRecorder: alarms);
        var assets = new AssetMaintenanceService(workOrderIntake: dispatch);
        var service = new SafetyEmergencyService(
            persistence: null,
            iotIntegration: iot,
            assetMaintenance: assets,
            monitoringAlarms: alarms,
            workOrderDispatch: dispatch);

        var ingestion = iot.IngestReading(new TelemetryIngestionCommand(
            "FIRE-SMOKE-OPD-1F-01",
            "smoke_density",
            1.6m,
            "%obs/m",
            new DateTimeOffset(2026, 5, 30, 10, 28, 0, TimeSpan.FromHours(8))));
        Assert.True(ingestion.Succeeded, ingestion.ErrorMessage);

        var alarm = Assert.Single(alarms.GetAlarmBoard().Alarms, item => item.PointCode == "FIRE-SMOKE-OPD-1F-01");
        var converted = alarms.ConvertToWorkOrder(
            alarm.AlarmNo,
            new ConvertAlarmToWorkOrderCommand("fire-duty", "消防值班人员", "启动火警确认与现场处置"));
        Assert.True(converted.Succeeded, converted.ErrorMessage);

        var board = service.GetBoard();

        var fireNode = Assert.Single(board.Nodes, item => item.NodeCode == "SAFE-FIRE-OPD-1F");
        Assert.Equal("FIRE-SMOKE-OPD-1F-01", fireNode.MonitoringPointCode);
        Assert.Equal("FIRE-ALARM-OPD-1F", fireNode.AssetCode);
        Assert.Equal("BIM-SEC-OPD-1F-FIRE", fireNode.Location.BimElementId);
        Assert.Equal(SafetyEmergencyNodeStatus.Critical, fireNode.Status);

        Assert.Contains(board.Nodes, item => item.NodeCode == "SAFE-SEC-ER-ACCESS");
        Assert.Contains(board.MonitoringPoints, point => point.PointCode == "FIRE-SMOKE-OPD-1F-01");
        Assert.Contains(board.MonitoringPoints, point => point.PointCode == "SEC-ACCESS-ER-01");
        Assert.Contains(board.SafetyAssets, asset => asset.AssetCode == "FIRE-ALARM-OPD-1F");
        Assert.Contains(board.ActiveAlarms, item => item.PointCode == "FIRE-SMOKE-OPD-1F-01");
        Assert.Contains(board.OpenWorkOrders, item => item.WorkOrderNo.StartsWith("WO-ALM-", StringComparison.Ordinal));
        Assert.Contains(board.SourceEvidence, evidence =>
            evidence.Sources.Contains("北建院") &&
            evidence.Sources.Contains("PPT"));
    }

    [Fact]
    public void NodeDetailReturnsResponseProcedureBimLocationAndLinkedRecords()
    {
        var dispatch = new WorkOrderDispatchService();
        var alarms = new MonitoringAlarmService(workOrderIntake: dispatch);
        var iot = new IotIntegrationService(alarmRecorder: alarms);
        var assets = new AssetMaintenanceService(workOrderIntake: dispatch);
        var service = new SafetyEmergencyService(
            persistence: null,
            iotIntegration: iot,
            assetMaintenance: assets,
            monitoringAlarms: alarms,
            workOrderDispatch: dispatch);

        var detail = service.GetNodeDetail("SAFE-FIRE-OPD-1F");

        Assert.NotNull(detail);
        Assert.Equal("BIM-SEC-OPD-1F-FIRE", detail.Node.Location.BimElementId);
        Assert.Equal("FIRE-SMOKE-OPD-1F-01", detail.MonitoringPoint?.Point.PointCode);
        Assert.Equal("FIRE-ALARM-OPD-1F", detail.SafetyAsset?.Asset.AssetCode);
        Assert.Contains(detail.ResponseProcedure, step => step.Action.Contains("确认", StringComparison.Ordinal));
        Assert.Contains(detail.SourceEvidence, evidence =>
            evidence.Sources.Contains("北建院") &&
            evidence.Sources.Contains("PPT"));
    }
}
