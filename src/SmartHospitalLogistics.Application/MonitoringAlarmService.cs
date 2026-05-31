using System.Text.RegularExpressions;
using SmartHospitalLogistics.Domain;

namespace SmartHospitalLogistics.Application;

public interface IMonitoringAlarmRecorder
{
    void RecordTelemetryReading(TelemetryReading reading, IotMonitoringPoint point);
}

public interface IMonitoringAlarmService : IMonitoringAlarmRecorder
{
    MonitoringAlarmBoard GetAlarmBoard();

    MonitoringAlarmEvent? GetAlarm(string alarmNo);

    MonitoringAlarmOperationResult Acknowledge(string alarmNo, AcknowledgeAlarmCommand command);

    MonitoringAlarmOperationResult ConvertToWorkOrder(string alarmNo, ConvertAlarmToWorkOrderCommand command);
}

public sealed class MonitoringAlarmService : IMonitoringAlarmService
{
    private static readonly DateTimeOffset SeedTime = new(2026, 5, 30, 9, 30, 0, TimeSpan.FromHours(8));
    private readonly object _sync = new();
    private readonly IMonitoringAlarmPersistence? _persistence;
    private readonly Dictionary<string, MonitoringAlarmEvent> _alarms;

    public MonitoringAlarmService(IMonitoringAlarmPersistence? persistence = null)
    {
        _persistence = persistence;
        var persisted = _persistence?.Load();
        _alarms = persisted?.Alarms.ToDictionary(alarm => alarm.AlarmNo, StringComparer.OrdinalIgnoreCase)
            ?? new Dictionary<string, MonitoringAlarmEvent>(StringComparer.OrdinalIgnoreCase);
    }

    public MonitoringAlarmBoard GetAlarmBoard()
    {
        lock (_sync)
        {
            return new MonitoringAlarmBoard(
                SeedTime,
                _alarms.Values
                    .OrderBy(alarm => alarm.Status == MonitoringAlarmStatus.Closed)
                    .ThenByDescending(alarm => alarm.RiskLevel)
                    .ThenByDescending(alarm => alarm.TriggeredAt)
                    .ToArray(),
                BuildSourceEvidence());
        }
    }

    public MonitoringAlarmEvent? GetAlarm(string alarmNo)
    {
        lock (_sync)
        {
            return _alarms.GetValueOrDefault(alarmNo);
        }
    }

    public void RecordTelemetryReading(TelemetryReading reading, IotMonitoringPoint point)
    {
        if (reading.RiskLevel == TelemetryRiskLevel.Normal)
        {
            return;
        }

        lock (_sync)
        {
            var alarm = new MonitoringAlarmEvent(
                BuildAlarmNo(reading),
                reading.PointCode,
                reading.MetricCode,
                $"{point.Name}{reading.MetricCode}异常",
                reading.RiskLevel,
                MonitoringAlarmStatus.New,
                point.Location,
                reading.Value,
                reading.Unit,
                reading.CollectedAt,
                reading.RuleSummary,
                RecommendedTeam(point.Category),
                LastRemark: "由物联阈值规则自动生成预警事件");

            _persistence?.SaveAlarm(alarm);
            _alarms[alarm.AlarmNo] = alarm;
        }
    }

    public MonitoringAlarmOperationResult Acknowledge(string alarmNo, AcknowledgeAlarmCommand command)
    {
        lock (_sync)
        {
            if (!_alarms.TryGetValue(alarmNo, out var alarm))
            {
                return new MonitoringAlarmOperationResult(false, $"Alarm {alarmNo} was not found.", null, NotFound: true);
            }

            if (alarm.Status is MonitoringAlarmStatus.ConvertedToWorkOrder or MonitoringAlarmStatus.Closed)
            {
                return new MonitoringAlarmOperationResult(false, $"Alarm {alarmNo} is already closed for acknowledgement.", alarm);
            }

            var updated = alarm with
            {
                Status = MonitoringAlarmStatus.Acknowledged,
                AcknowledgedBy = command.Operator,
                AcknowledgedAt = SeedTime.AddMinutes(_alarms.Count + 1),
                LastRemark = command.Remark
            };
            _persistence?.SaveAlarm(updated);
            _alarms[alarmNo] = updated;

            return new MonitoringAlarmOperationResult(true, null, updated);
        }
    }

    public MonitoringAlarmOperationResult ConvertToWorkOrder(string alarmNo, ConvertAlarmToWorkOrderCommand command)
    {
        lock (_sync)
        {
            if (!_alarms.TryGetValue(alarmNo, out var alarm))
            {
                return new MonitoringAlarmOperationResult(false, $"Alarm {alarmNo} was not found.", null, NotFound: true);
            }

            if (alarm.Status == MonitoringAlarmStatus.ConvertedToWorkOrder)
            {
                return new MonitoringAlarmOperationResult(false, $"Alarm {alarmNo} has already been converted to a work order.", alarm);
            }

            if (alarm.Status == MonitoringAlarmStatus.Closed)
            {
                return new MonitoringAlarmOperationResult(false, $"Alarm {alarmNo} is closed.", alarm);
            }

            var updated = alarm with
            {
                Status = MonitoringAlarmStatus.ConvertedToWorkOrder,
                ResponsibleTeam = command.TargetTeam,
                WorkOrderNo = $"WO-ALM-{Sanitize(alarm.AlarmNo).Replace("ALM", string.Empty, StringComparison.OrdinalIgnoreCase)}",
                AcknowledgedBy = alarm.AcknowledgedBy ?? command.Operator,
                AcknowledgedAt = alarm.AcknowledgedAt ?? SeedTime.AddMinutes(_alarms.Count + 2),
                LastRemark = command.Remark
            };
            _persistence?.SaveAlarm(updated);
            _alarms[alarmNo] = updated;

            return new MonitoringAlarmOperationResult(true, null, updated);
        }
    }

    private static string BuildAlarmNo(TelemetryReading reading) =>
        $"ALM-{Sanitize(reading.PointCode)}-{Sanitize(reading.MetricCode)}-{reading.CollectedAt:yyyyMMddHHmmss}";

    private static string Sanitize(string value) =>
        Regex.Replace(value.ToUpperInvariant(), "[^A-Z0-9]+", "-").Trim('-');

    private static string RecommendedTeam(IotSystemCategory category) =>
        category switch
        {
            IotSystemCategory.StrongElectric => "电工班工作人员",
            IotSystemCategory.Hvac => "暖通班工作人员",
            IotSystemCategory.WaterSupplyDrainage or IotSystemCategory.Sewage => "给排水班工作人员",
            IotSystemCategory.MedicalGas => "医气维保人员",
            IotSystemCategory.EnvironmentQuality => "环境监管班组",
            _ => "综合维修班"
        };

    private static FeatureEvidence[] BuildSourceEvidence() =>
    [
        new FeatureEvidence("环境监管预警池", ["PPT", "中科医信"], "PPT 要求点位采集、阈值策略、预警池、报警策略和处置闭环；竞品包含统一报警和专项报警处理。"),
        new FeatureEvidence("客户物联告警联动", ["北建院", "PPT"], "北建院客户数据要求监测点位关联 BIM 空间、设备资产、指标、阈值、告警事件和工单。")
    ];
}
