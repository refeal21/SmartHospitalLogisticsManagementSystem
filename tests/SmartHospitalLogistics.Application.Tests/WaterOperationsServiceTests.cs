using SmartHospitalLogistics.Application;
using SmartHospitalLogistics.Domain;

namespace SmartHospitalLogistics.Application.Tests;

public sealed class WaterOperationsServiceTests
{
    [Fact]
    public void BoardAggregatesWaterAndSewagePointsAssetsMaintenanceAlarmWorkOrderAndTraceability()
    {
        var dispatch = new WorkOrderDispatchService();
        var alarms = new MonitoringAlarmService(workOrderIntake: dispatch);
        var iot = new IotIntegrationService(alarmRecorder: alarms);
        var assets = new AssetMaintenanceService(workOrderIntake: dispatch);
        var service = new WaterOperationsService(
            persistence: null,
            iotIntegration: iot,
            assetMaintenance: assets,
            monitoringAlarms: alarms,
            workOrderDispatch: dispatch);

        var ingestion = iot.IngestReading(new TelemetryIngestionCommand(
            "WATER-PUMP-B1-01",
            "pressure",
            0.2m,
            "MPa",
            new DateTimeOffset(2026, 5, 30, 10, 45, 0, TimeSpan.FromHours(8))));
        Assert.True(ingestion.Succeeded, ingestion.ErrorMessage);

        var alarm = Assert.Single(alarms.GetAlarmBoard().Alarms, item => item.PointCode == "WATER-PUMP-B1-01");
        var converted = alarms.ConvertToWorkOrder(
            alarm.AlarmNo,
            new ConvertAlarmToWorkOrderCommand("water-dispatcher", "给排水班工作人员", "water pressure critical"));
        Assert.True(converted.Succeeded, converted.ErrorMessage);

        var board = service.GetBoard();

        Assert.Contains(board.Units, item => item.UnitCode == "WATER-SYS-B1-PUMP");
        Assert.Contains(board.Units, item => item.UnitCode == "SEWAGE-SYS-B1-TREATMENT");
        Assert.Contains(board.MonitoringPoints, point => point.PointCode == "WATER-PUMP-B1-01");
        Assert.Contains(board.MonitoringPoints, point => point.PointCode == "SEWAGE-STATION-01");
        Assert.Contains(board.WaterAssets, asset => asset.AssetCode == "WATER-PUMP-B1-01");
        Assert.Contains(board.WaterAssets, asset => asset.AssetCode == "SEWAGE-STATION-01");
        Assert.Contains(board.ActiveAlarms, item => item.PointCode == "WATER-PUMP-B1-01");
        Assert.Contains(board.OpenWorkOrders, item => item.WorkOrderNo.StartsWith("WO-ALM-", StringComparison.Ordinal));
        Assert.Contains(board.DueMaintenanceTasks, task => task.TaskNo == "MT-20260530-0006");
        Assert.Contains(board.DueMaintenanceTasks, task => task.TaskNo == "MT-20260530-0007");
        Assert.Contains(board.SourceEvidence, evidence =>
            evidence.Sources.Contains("北建院") &&
            evidence.Sources.Contains("中科医信") &&
            evidence.Sources.Contains("PPT"));
        Assert.DoesNotContain(board.SourceEvidence, evidence =>
            evidence.Sources.Count == 1 &&
            evidence.Sources[0].Contains("AI", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void UnitDetailReturnsBimLocationSourceTraceabilityAndLinkedOperationalRecords()
    {
        var service = new WaterOperationsService();

        var waterDetail = service.GetUnitDetail("WATER-SYS-B1-PUMP");
        var sewageDetail = service.GetUnitDetail("SEWAGE-SYS-B1-TREATMENT");

        Assert.NotNull(waterDetail);
        Assert.NotNull(sewageDetail);
        Assert.Equal("BIM-ENE-B1-PUMP", waterDetail.Unit.Location.BimElementId);
        Assert.Equal("WATER-PUMP-B1-01", waterDetail.MonitoringPoint?.Point.PointCode);
        Assert.Equal("WATER-PUMP-B1-01", waterDetail.WaterAsset?.Asset.AssetCode);
        Assert.Contains(waterDetail.MaintenanceTasks, task => task.TaskNo == "MT-20260530-0006");
        Assert.Equal("BIM-LOG-B1-SEWAGE", sewageDetail.Unit.Location.BimElementId);
        Assert.Equal("SEWAGE-STATION-01", sewageDetail.MonitoringPoint?.Point.PointCode);
        Assert.Equal("SEWAGE-STATION-01", sewageDetail.WaterAsset?.Asset.AssetCode);
        Assert.Contains(sewageDetail.SourceEvidence, evidence =>
            evidence.Sources.Contains("北建院") &&
            evidence.Sources.Contains("中科医信") &&
            evidence.Sources.Contains("PPT"));
    }
}
