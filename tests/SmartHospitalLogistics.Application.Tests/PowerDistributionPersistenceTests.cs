using SmartHospitalLogistics.Application;
using SmartHospitalLogistics.Domain;
using SmartHospitalLogistics.Infrastructure;

namespace SmartHospitalLogistics.Application.Tests;

public sealed class PowerDistributionPersistenceTests
{
    [Fact]
    public void SqlitePersistenceRestoresSeededPowerDistributionCircuits()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"smart-hospital-power-{Guid.NewGuid():N}.db");
        try
        {
            var first = BuildService(new SqlitePowerDistributionPersistence(dbPath));
            var firstDetail = first.GetCircuitDetail("PWR-CIRCUIT-B1-LV-IN");
            Assert.NotNull(firstDetail);

            var second = BuildService(new SqlitePowerDistributionPersistence(dbPath));
            var reloaded = second.GetCircuitDetail("PWR-CIRCUIT-B1-LV-IN");

            Assert.NotNull(reloaded);
            Assert.Equal("PWR-LV-B1-IN-01", reloaded.Circuit.MeterPointCode);
            Assert.Equal("PWR-LV-B1-IN-CAB", reloaded.Circuit.AssetCode);
            Assert.Equal("BIM-ENE-B1-PDU", reloaded.Circuit.Location.BimElementId);
            Assert.Contains(reloaded.SourceEvidence, evidence =>
                evidence.Sources.Contains("北建院") &&
                evidence.Sources.Contains("中科医信") &&
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

    private static PowerDistributionService BuildService(IPowerDistributionPersistence persistence)
    {
        var dispatch = new WorkOrderDispatchService();
        var alarms = new MonitoringAlarmService(workOrderIntake: dispatch);
        var iot = new IotIntegrationService(alarmRecorder: alarms);
        var assets = new AssetMaintenanceService(workOrderIntake: dispatch);
        return new PowerDistributionService(
            persistence,
            iot,
            assets,
            alarms,
            dispatch);
    }
}
