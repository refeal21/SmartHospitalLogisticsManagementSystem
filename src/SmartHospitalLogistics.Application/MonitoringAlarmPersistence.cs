using SmartHospitalLogistics.Domain;

namespace SmartHospitalLogistics.Application;

public sealed record MonitoringAlarmPersistenceSnapshot(
    IReadOnlyList<MonitoringAlarmEvent> Alarms);

public interface IMonitoringAlarmPersistence
{
    MonitoringAlarmPersistenceSnapshot Load();

    void SaveAlarm(MonitoringAlarmEvent alarm);
}
