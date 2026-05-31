using SmartHospitalLogistics.Domain;

namespace SmartHospitalLogistics.Application;

public sealed record IotIntegrationPersistenceSnapshot(
    IReadOnlyList<IotSystemProfile> Systems,
    IReadOnlyList<IotMonitoringPoint> Points,
    IReadOnlyList<TelemetryThresholdRule> ThresholdRules,
    IReadOnlyList<TelemetryReading> Readings);

public interface IIotIntegrationPersistence
{
    IotIntegrationPersistenceSnapshot Load();

    void SaveSystem(IotSystemProfile system);

    void SavePoint(IotMonitoringPoint point);

    void SaveThresholdRule(TelemetryThresholdRule rule);

    void SaveReading(TelemetryReading reading);
}
