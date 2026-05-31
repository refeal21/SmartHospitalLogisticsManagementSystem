using SmartHospitalLogistics.Application;
using SmartHospitalLogistics.Domain;
using SmartHospitalLogistics.Infrastructure;

namespace SmartHospitalLogistics.Application.Tests;

public sealed class MedicalGasPersistenceTests
{
    [Fact]
    public void SqlitePersistenceRestoresSeededMedicalGasZones()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"smart-hospital-medgas-{Guid.NewGuid():N}.db");
        try
        {
            var first = BuildService(new SqliteMedicalGasPersistence(dbPath));
            var firstDetail = first.GetZoneDetail("MG-ZONE-IPD-8F");
            Assert.NotNull(firstDetail);

            var second = BuildService(new SqliteMedicalGasPersistence(dbPath));
            var reloaded = second.GetZoneDetail("MG-ZONE-IPD-8F");

            Assert.NotNull(reloaded);
            Assert.Equal("MEDGAS-O2-8F", reloaded.Zone.PressurePointCode);
            Assert.Equal("MEDGAS-IPD-8F", reloaded.Zone.ValveAssetCode);
            Assert.Contains(MedicalGasSupplyType.CompressedAir, reloaded.Zone.SupplyTypes);
            Assert.Contains(reloaded.SourceEvidence, evidence => evidence.Sources.Contains("PPT"));
        }
        finally
        {
            if (File.Exists(dbPath))
            {
                File.Delete(dbPath);
            }
        }
    }

    private static MedicalGasService BuildService(IMedicalGasPersistence persistence)
    {
        var dispatch = new WorkOrderDispatchService();
        var alarms = new MonitoringAlarmService(workOrderIntake: dispatch);
        var iot = new IotIntegrationService(alarmRecorder: alarms);
        var assets = new AssetMaintenanceService(workOrderIntake: dispatch);
        return new MedicalGasService(
            persistence,
            iot,
            assets,
            alarms,
            dispatch);
    }
}
