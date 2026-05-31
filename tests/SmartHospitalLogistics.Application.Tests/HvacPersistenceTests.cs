using SmartHospitalLogistics.Application;
using SmartHospitalLogistics.Domain;
using SmartHospitalLogistics.Infrastructure;

namespace SmartHospitalLogistics.Application.Tests;

public sealed class HvacPersistenceTests
{
    [Fact]
    public void SqlitePersistenceRestoresSeededHvacLoops()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"smart-hospital-hvac-{Guid.NewGuid():N}.db");
        try
        {
            var first = BuildService(new SqliteHvacPersistence(dbPath));
            var firstDetail = first.GetLoopDetail("HVAC-LOOP-B1-CHW");
            Assert.NotNull(firstDetail);

            var second = BuildService(new SqliteHvacPersistence(dbPath));
            var reloaded = second.GetLoopDetail("HVAC-LOOP-B1-CHW");

            Assert.NotNull(reloaded);
            Assert.Equal("HVAC-CHW-B1-02", reloaded.Loop.MonitoringPointCode);
            Assert.Equal("CHW-B1-02", reloaded.Loop.AssetCode);
            Assert.Equal("BIM-ENE-B1-CHILLER", reloaded.Loop.Location.BimElementId);
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

    private static HvacService BuildService(IHvacPersistence persistence)
    {
        var dispatch = new WorkOrderDispatchService();
        var alarms = new MonitoringAlarmService(workOrderIntake: dispatch);
        var iot = new IotIntegrationService(alarmRecorder: alarms);
        var assets = new AssetMaintenanceService(workOrderIntake: dispatch);
        return new HvacService(
            persistence,
            iot,
            assets,
            alarms,
            dispatch);
    }
}
