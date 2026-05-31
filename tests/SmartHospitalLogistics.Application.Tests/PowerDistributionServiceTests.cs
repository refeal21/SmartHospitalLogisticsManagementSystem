using SmartHospitalLogistics.Application;
using SmartHospitalLogistics.Domain;

namespace SmartHospitalLogistics.Application.Tests;

public sealed class PowerDistributionServiceTests
{
    [Fact]
    public void BoardAggregatesStrongElectricPointAssetMaintenanceAlarmWorkOrderAndTraceability()
    {
        var dispatch = new WorkOrderDispatchService();
        var alarms = new MonitoringAlarmService(workOrderIntake: dispatch);
        var iot = new IotIntegrationService(alarmRecorder: alarms);
        var assets = new AssetMaintenanceService(workOrderIntake: dispatch);
        var service = new PowerDistributionService(
            persistence: null,
            iotIntegration: iot,
            assetMaintenance: assets,
            monitoringAlarms: alarms,
            workOrderDispatch: dispatch);

        var ingestion = iot.IngestReading(new TelemetryIngestionCommand(
            "PWR-LV-B1-IN-01",
            "voltage",
            260m,
            "V",
            new DateTimeOffset(2026, 5, 30, 10, 20, 0, TimeSpan.FromHours(8))));
        Assert.True(ingestion.Succeeded, ingestion.ErrorMessage);

        var alarm = Assert.Single(alarms.GetAlarmBoard().Alarms, item => item.PointCode == "PWR-LV-B1-IN-01");
        var converted = alarms.ConvertToWorkOrder(
            alarm.AlarmNo,
            new ConvertAlarmToWorkOrderCommand("power-dispatcher", "电工班工作人员", "voltage critical"));
        Assert.True(converted.Succeeded, converted.ErrorMessage);

        var board = service.GetBoard();

        var circuit = Assert.Single(board.Circuits, item => item.CircuitCode == "PWR-CIRCUIT-B1-LV-IN");
        Assert.Equal("PWR-LV-B1-IN-01", circuit.MeterPointCode);
        Assert.Equal("PWR-LV-B1-IN-CAB", circuit.AssetCode);
        Assert.Equal("BIM-ENE-B1-PDU", circuit.Location.BimElementId);
        Assert.Equal("电工班工作人员", circuit.ResponsibleTeam);
        Assert.Contains(circuit.SourceEvidence, evidence =>
            evidence.Sources.Contains("北建院") &&
            evidence.Sources.Contains("中科医信") &&
            evidence.Sources.Contains("PPT"));

        Assert.Contains(board.MonitoringPoints, point => point.PointCode == "PWR-LV-B1-IN-01");
        Assert.Contains(board.ElectricalAssets, asset => asset.AssetCode == "PWR-LV-B1-IN-CAB");
        Assert.Contains(board.ActiveAlarms, item => item.PointCode == "PWR-LV-B1-IN-01");
        Assert.Contains(board.OpenWorkOrders, item => item.WorkOrderNo.StartsWith("WO-ALM-", StringComparison.Ordinal));
        Assert.Contains(board.DueMaintenanceTasks, task => task.TaskNo == "MT-20260530-0005");
        Assert.True(board.Kpis.AbnormalCircuits >= 1);
        Assert.True(board.Kpis.ActiveAlarms >= 1);
        Assert.DoesNotContain(board.SourceEvidence, evidence =>
            evidence.Sources.Count == 1 &&
            evidence.Sources[0].Contains("AI", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CircuitDetailReturnsBimLocationSourceTraceabilityAndLinkedOperationalRecords()
    {
        var dispatch = new WorkOrderDispatchService();
        var alarms = new MonitoringAlarmService(workOrderIntake: dispatch);
        var iot = new IotIntegrationService(alarmRecorder: alarms);
        var assets = new AssetMaintenanceService(workOrderIntake: dispatch);
        var service = new PowerDistributionService(
            persistence: null,
            iotIntegration: iot,
            assetMaintenance: assets,
            monitoringAlarms: alarms,
            workOrderDispatch: dispatch);

        var detail = service.GetCircuitDetail("PWR-CIRCUIT-B1-LV-IN");

        Assert.NotNull(detail);
        Assert.Equal("BIM-ENE-B1-PDU", detail.Circuit.Location.BimElementId);
        Assert.Equal("PWR-LV-B1-IN-01", detail.MonitoringPoint?.Point.PointCode);
        Assert.Equal("PWR-LV-B1-IN-CAB", detail.ElectricalAsset?.Asset.AssetCode);
        Assert.Contains(detail.MaintenanceTasks, task => task.TaskNo == "MT-20260530-0005");
        Assert.Contains(detail.SourceEvidence, evidence =>
            evidence.Sources.Contains("北建院") &&
            evidence.Sources.Contains("中科医信") &&
            evidence.Sources.Contains("PPT"));
    }
}
