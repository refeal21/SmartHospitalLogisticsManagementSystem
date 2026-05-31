using SmartHospitalLogistics.Application;
using SmartHospitalLogistics.Infrastructure;

namespace SmartHospitalLogistics.Application.Tests;

public sealed class SafetyEmergencyPersistenceTests
{
    [Fact]
    public void SqlitePersistenceRestoresSeededSafetyEmergencyNodes()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"smart-hospital-safety-{Guid.NewGuid():N}.db");
        try
        {
            var first = BuildService(new SqliteSafetyEmergencyPersistence(dbPath));
            var firstDetail = first.GetNodeDetail("SAFE-FIRE-OPD-1F");
            Assert.NotNull(firstDetail);

            var second = BuildService(new SqliteSafetyEmergencyPersistence(dbPath));
            var reloaded = second.GetNodeDetail("SAFE-FIRE-OPD-1F");

            Assert.NotNull(reloaded);
            Assert.Equal("FIRE-SMOKE-OPD-1F-01", reloaded.Node.MonitoringPointCode);
            Assert.Equal("FIRE-ALARM-OPD-1F", reloaded.Node.AssetCode);
            Assert.Equal("BIM-SEC-OPD-1F-FIRE", reloaded.Node.Location.BimElementId);
            Assert.Contains(reloaded.SourceEvidence, evidence =>
                evidence.Sources.Contains("北建院") &&
                evidence.Sources.Contains("PPT"));
        }
        finally
        {
            if (File.Exists(dbPath))
            {
                File.Delete(dbPath);
            }
        }
    }

    private static SafetyEmergencyService BuildService(ISafetyEmergencyPersistence persistence)
    {
        var dispatch = new WorkOrderDispatchService();
        var alarms = new MonitoringAlarmService(workOrderIntake: dispatch);
        var iot = new IotIntegrationService(alarmRecorder: alarms);
        var assets = new AssetMaintenanceService(workOrderIntake: dispatch);
        return new SafetyEmergencyService(
            persistence,
            iot,
            assets,
            alarms,
            dispatch);
    }
}
