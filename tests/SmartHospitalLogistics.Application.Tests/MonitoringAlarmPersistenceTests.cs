using SmartHospitalLogistics.Application;
using SmartHospitalLogistics.Domain;
using SmartHospitalLogistics.Infrastructure;

namespace SmartHospitalLogistics.Application.Tests;

public sealed class MonitoringAlarmPersistenceTests : IDisposable
{
    private readonly string _alarmDbPath = Path.Combine(Path.GetTempPath(), $"smart-hospital-alarms-{Guid.NewGuid():N}.db");
    private readonly string _iotDbPath = Path.Combine(Path.GetTempPath(), $"smart-hospital-alarm-iot-{Guid.NewGuid():N}.db");

    [Fact]
    public void CriticalTelemetryReadingCreatesPersistedAlarmEvent()
    {
        var alarmService = new MonitoringAlarmService(new SqliteMonitoringAlarmPersistence(_alarmDbPath));
        var iotService = new IotIntegrationService(new SqliteIotIntegrationPersistence(_iotDbPath), alarmService);

        var result = iotService.IngestReading(new TelemetryIngestionCommand(
            "MEDGAS-O2-8F",
            "pressure",
            0.31m,
            "MPa",
            new DateTimeOffset(2026, 5, 30, 10, 28, 0, TimeSpan.FromHours(8))));

        var reloaded = new MonitoringAlarmService(new SqliteMonitoringAlarmPersistence(_alarmDbPath));
        var board = reloaded.GetAlarmBoard();

        Assert.True(result.Succeeded, result.ErrorMessage);
        Assert.Single(board.Alarms);
        Assert.Equal("MEDGAS-O2-8F", board.Alarms[0].PointCode);
        Assert.Equal("pressure", board.Alarms[0].MetricCode);
        Assert.Equal(TelemetryRiskLevel.Critical, board.Alarms[0].RiskLevel);
        Assert.Equal(MonitoringAlarmStatus.New, board.Alarms[0].Status);
        Assert.Equal("BIM-IPD-F8-WARD", board.Alarms[0].Location.BimElementId);
    }

    [Fact]
    public void AlarmAcknowledgementAndWorkOrderConversionArePersisted()
    {
        var alarmService = new MonitoringAlarmService(new SqliteMonitoringAlarmPersistence(_alarmDbPath));
        alarmService.RecordTelemetryReading(
            new TelemetryReading(
                "SEWAGE-STATION-01",
                "cod",
                240m,
                "mg/L",
                new DateTimeOffset(2026, 5, 30, 10, 35, 0, TimeSpan.FromHours(8)),
                TelemetryRiskLevel.Critical,
                "医疗废水 COD 超阈值需污水站处置；当前值 240 高于阈值"),
            new IotMonitoringPoint(
                "SEWAGE-STATION-01",
                "污水处理站综合水质点",
                IotSystemCategory.Sewage,
                new SpatialLocation("同仁亦庄院区", "后勤楼", "B1", "污水处理站", "BIM-LOG-B1-SEWAGE"),
                "SEWAGE-001",
                "opcua-adapter",
                [new IotMetricDefinition("cod", "COD", "mg/L", "decimal", "医疗废水指标")],
                []));
        var alarmNo = alarmService.GetAlarmBoard().Alarms[0].AlarmNo;

        var acknowledged = alarmService.Acknowledge(alarmNo, new AcknowledgeAlarmCommand("给排水班工作人员", "已确认污水站 COD 超限"));
        var converted = alarmService.ConvertToWorkOrder(alarmNo, new ConvertAlarmToWorkOrderCommand("调度员", "给排水班工作人员", "转入污水站处置工单"));

        var reloaded = new MonitoringAlarmService(new SqliteMonitoringAlarmPersistence(_alarmDbPath));
        var alarm = reloaded.GetAlarm(alarmNo);

        Assert.True(acknowledged.Succeeded, acknowledged.ErrorMessage);
        Assert.True(converted.Succeeded, converted.ErrorMessage);
        Assert.NotNull(alarm);
        Assert.Equal(MonitoringAlarmStatus.ConvertedToWorkOrder, alarm.Status);
        Assert.StartsWith("WO-ALM-", alarm.WorkOrderNo);
        Assert.Contains("转入污水站", alarm.LastRemark);
    }

    public void Dispose()
    {
        if (File.Exists(_alarmDbPath))
        {
            File.Delete(_alarmDbPath);
        }

        if (File.Exists(_iotDbPath))
        {
            File.Delete(_iotDbPath);
        }
    }
}
