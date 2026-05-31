using SmartHospitalLogistics.Application;
using SmartHospitalLogistics.Domain;
using SmartHospitalLogistics.Infrastructure;

namespace SmartHospitalLogistics.Application.Tests;

public sealed class CrossModuleWorkOrderIntakeTests : IDisposable
{
    private readonly string _workOrderDbPath = Path.Combine(Path.GetTempPath(), $"smart-hospital-cross-wo-{Guid.NewGuid():N}.db");
    private readonly string _alarmDbPath = Path.Combine(Path.GetTempPath(), $"smart-hospital-cross-alarm-{Guid.NewGuid():N}.db");
    private readonly string _assetDbPath = Path.Combine(Path.GetTempPath(), $"smart-hospital-cross-asset-{Guid.NewGuid():N}.db");
    private readonly string _iotDbPath = Path.Combine(Path.GetTempPath(), $"smart-hospital-cross-iot-{Guid.NewGuid():N}.db");

    [Fact]
    public void AlarmConversionCreatesRealDispatchableWorkOrder()
    {
        var workOrders = new WorkOrderDispatchService(new SqliteWorkOrderPersistence(_workOrderDbPath));
        var alarms = new MonitoringAlarmService(new SqliteMonitoringAlarmPersistence(_alarmDbPath), workOrders);
        var iot = new IotIntegrationService(new SqliteIotIntegrationPersistence(_iotDbPath), alarms);
        iot.IngestReading(new TelemetryIngestionCommand(
            "MEDGAS-O2-8F",
            "pressure",
            0.31m,
            "MPa",
            new DateTimeOffset(2026, 5, 30, 10, 28, 0, TimeSpan.FromHours(8))));
        var alarmNo = alarms.GetAlarmBoard().Alarms[0].AlarmNo;

        var converted = alarms.ConvertToWorkOrder(
            alarmNo,
            new ConvertAlarmToWorkOrderCommand("调度员", "医气维保人员", "转入医气专项处置工单"));
        var convertedAgain = alarms.ConvertToWorkOrder(
            alarmNo,
            new ConvertAlarmToWorkOrderCommand("调度员", "医气维保人员", "重复点击不应重复生成"));
        var workOrderNo = converted.Alarm!.WorkOrderNo!;
        var detail = workOrders.GetWorkOrderDetail(workOrderNo);
        var dispatchResult = workOrders.Dispatch(workOrderNo, new DispatchWorkOrderCommand("医气维保人员", "调度员", "按医气压力告警派工"));

        Assert.True(converted.Succeeded, converted.ErrorMessage);
        Assert.True(convertedAgain.Succeeded, convertedAgain.ErrorMessage);
        Assert.Equal(workOrderNo, convertedAgain.Alarm?.WorkOrderNo);
        Assert.NotNull(detail);
        Assert.Equal(WorkOrderStatus.New, detail.WorkOrder.Status);
        Assert.Equal("BIM-IPD-F8-WARD", detail.Location.BimElementId);
        Assert.Contains(detail.SourceEvidence, evidence =>
            evidence.FeatureName == "客户物联告警联动" &&
            evidence.Sources.Contains("北建院") &&
            evidence.Sources.Contains("PPT") &&
            evidence.Sources.Contains("中科医信"));
        Assert.Single(workOrders.GetDispatchBoard().WorkOrders, order => order.WorkOrderNo == workOrderNo);
        Assert.True(dispatchResult.Succeeded, dispatchResult.ErrorMessage);
        Assert.Equal(WorkOrderStatus.Dispatched, dispatchResult.Detail?.WorkOrder.Status);
    }

    [Fact]
    public void AbnormalMaintenanceTaskCreatesRealDispatchableWorkOrder()
    {
        var workOrders = new WorkOrderDispatchService(new SqliteWorkOrderPersistence(_workOrderDbPath));
        var assets = new AssetMaintenanceService(new SqliteAssetMaintenancePersistence(_assetDbPath), workOrders);

        var result = assets.CompleteTask(
            "MT-20260530-0002",
            new CompleteMaintenanceTaskCommand(
                "医气维保人员",
                MaintenanceOutcome.Abnormal,
                "8F 医气分区阀箱压力波动，需要现场维修",
                [
                    new InspectionChecklistResult("CHK-PRESSURE", "异常", "氧气压力低于下限"),
                    new InspectionChecklistResult("CHK-VALVE", "正常", "阀门状态正常")
                ],
                ConvertToWorkOrder: true));
        var workOrderNo = result.GeneratedWorkOrder!.WorkOrderNo;
        var detail = workOrders.GetWorkOrderDetail(workOrderNo);
        var dispatchResult = workOrders.Dispatch(workOrderNo, new DispatchWorkOrderCommand("医气维保人员", "调度员", "按巡检异常派工"));

        Assert.True(result.Succeeded, result.ErrorMessage);
        Assert.NotNull(detail);
        Assert.Equal(WorkOrderStatus.New, detail.WorkOrder.Status);
        Assert.Equal("BIM-IPD-F8-WARD", detail.Location.BimElementId);
        Assert.Contains(detail.SourceEvidence, evidence =>
            evidence.FeatureName == "巡检保养异常转工单" &&
            evidence.Sources.Contains("中科医信") &&
            evidence.Sources.Contains("PPT") &&
            evidence.Sources.Contains("北建院"));
        Assert.Single(workOrders.GetDispatchBoard().WorkOrders, order => order.WorkOrderNo == workOrderNo);
        Assert.True(dispatchResult.Succeeded, dispatchResult.ErrorMessage);
        Assert.Equal(WorkOrderStatus.Dispatched, dispatchResult.Detail?.WorkOrder.Status);
    }

    public void Dispose()
    {
        foreach (var dbPath in new[] { _workOrderDbPath, _alarmDbPath, _assetDbPath, _iotDbPath })
        {
            if (File.Exists(dbPath))
            {
                File.Delete(dbPath);
            }
        }
    }
}
