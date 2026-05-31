using SmartHospitalLogistics.Application;
using SmartHospitalLogistics.Domain;

namespace SmartHospitalLogistics.Application.Tests;

public sealed class MedicalGasServiceTests
{
    [Fact]
    public void BoardAggregatesMedicalGasZoneIotAssetMaintenanceAlarmAndWorkOrderContext()
    {
        var dispatch = new WorkOrderDispatchService();
        var alarms = new MonitoringAlarmService(workOrderIntake: dispatch);
        var iot = new IotIntegrationService(alarmRecorder: alarms);
        var assets = new AssetMaintenanceService(workOrderIntake: dispatch);
        var service = new MedicalGasService(
            persistence: null,
            iotIntegration: iot,
            assetMaintenance: assets,
            monitoringAlarms: alarms,
            workOrderDispatch: dispatch);

        var ingestion = iot.IngestReading(new TelemetryIngestionCommand(
            "MEDGAS-O2-8F",
            "pressure",
            0.31m,
            "MPa",
            new DateTimeOffset(2026, 5, 30, 10, 0, 0, TimeSpan.FromHours(8))));
        Assert.True(ingestion.Succeeded, ingestion.ErrorMessage);

        var alarm = Assert.Single(alarms.GetAlarmBoard().Alarms, item => item.PointCode == "MEDGAS-O2-8F");
        var converted = alarms.ConvertToWorkOrder(
            alarm.AlarmNo,
            new ConvertAlarmToWorkOrderCommand("medical-gas-dispatcher", "医气维保人员", "oxygen pressure critical"));
        Assert.True(converted.Succeeded, converted.ErrorMessage);

        var board = service.GetBoard();

        var zone = Assert.Single(board.Zones, item => item.ZoneCode == "MG-ZONE-IPD-8F");
        Assert.Contains(MedicalGasSupplyType.Oxygen, zone.SupplyTypes);
        Assert.Equal("MEDGAS-O2-8F", zone.PressurePointCode);
        Assert.Equal("MEDGAS-IPD-8F", zone.ValveAssetCode);
        Assert.Contains(zone.SourceEvidence, evidence => evidence.Sources.Contains("PPT"));

        Assert.Contains(board.MonitoringPoints, point => point.PointCode == "MEDGAS-O2-8F");
        Assert.Contains(board.ValveAssets, asset => asset.AssetCode == "MEDGAS-IPD-8F");
        Assert.Contains(board.ActiveAlarms, item => item.PointCode == "MEDGAS-O2-8F");
        Assert.Contains(board.OpenWorkOrders, item => item.WorkOrderNo.StartsWith("WO-ALM-", StringComparison.Ordinal));
        Assert.Contains(board.DueMaintenanceTasks, task => task.TaskNo == "MT-20260530-0002");
        Assert.Equal(1, board.Kpis.AbnormalZones);
        Assert.Equal(1, board.Kpis.ActiveAlarms);
        Assert.DoesNotContain(board.SourceEvidence, evidence => evidence.Sources.Count == 1 && evidence.Sources[0].Contains("AI", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ZoneDetailReturnsBimLocationSourceTraceabilityAndLinkedOperationalRecords()
    {
        var dispatch = new WorkOrderDispatchService();
        var alarms = new MonitoringAlarmService(workOrderIntake: dispatch);
        var iot = new IotIntegrationService(alarmRecorder: alarms);
        var assets = new AssetMaintenanceService(workOrderIntake: dispatch);
        var service = new MedicalGasService(
            persistence: null,
            iotIntegration: iot,
            assetMaintenance: assets,
            monitoringAlarms: alarms,
            workOrderDispatch: dispatch);

        var detail = service.GetZoneDetail("MG-ZONE-IPD-8F");

        Assert.NotNull(detail);
        Assert.Equal("BIM-IPD-F8-WARD", detail.Zone.Location.BimElementId);
        Assert.Equal("MEDGAS-O2-8F", detail.MonitoringPoint?.Point.PointCode);
        Assert.Equal("MEDGAS-IPD-8F", detail.ValveAsset?.Asset.AssetCode);
        Assert.Contains(detail.MaintenanceTasks, task => task.TaskNo == "MT-20260530-0002");
        Assert.Contains(detail.SourceEvidence, evidence => evidence.Sources.Contains("PPT"));
    }
}
