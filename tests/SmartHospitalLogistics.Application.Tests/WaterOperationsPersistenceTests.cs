using SmartHospitalLogistics.Application;
using SmartHospitalLogistics.Domain;
using SmartHospitalLogistics.Infrastructure;

namespace SmartHospitalLogistics.Application.Tests;

public sealed class WaterOperationsPersistenceTests
{
    [Fact]
    public void SqlitePersistenceRestoresSeededWaterOperationUnits()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"smart-hospital-water-{Guid.NewGuid():N}.db");
        try
        {
            var first = BuildService(new SqliteWaterOperationsPersistence(dbPath));
            var firstDetail = first.GetUnitDetail("WATER-SYS-B1-PUMP");
            Assert.NotNull(firstDetail);

            var second = BuildService(new SqliteWaterOperationsPersistence(dbPath));
            var water = second.GetUnitDetail("WATER-SYS-B1-PUMP");
            var sewage = second.GetUnitDetail("SEWAGE-SYS-B1-TREATMENT");

            Assert.NotNull(water);
            Assert.NotNull(sewage);
            Assert.Equal("WATER-PUMP-B1-01", water.Unit.MonitoringPointCode);
            Assert.Equal("SEWAGE-STATION-01", sewage.Unit.MonitoringPointCode);
            Assert.Equal("BIM-ENE-B1-PUMP", water.Unit.Location.BimElementId);
            Assert.Equal("BIM-LOG-B1-SEWAGE", sewage.Unit.Location.BimElementId);
            Assert.Contains(water.SourceEvidence, evidence =>
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

    private static WaterOperationsService BuildService(IWaterOperationsPersistence persistence)
    {
        var dispatch = new WorkOrderDispatchService();
        var alarms = new MonitoringAlarmService(workOrderIntake: dispatch);
        var iot = new IotIntegrationService(alarmRecorder: alarms);
        var assets = new AssetMaintenanceService(workOrderIntake: dispatch);
        return new WaterOperationsService(
            persistence,
            iot,
            assets,
            alarms,
            dispatch);
    }
}
