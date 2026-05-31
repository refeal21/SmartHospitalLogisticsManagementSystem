using SmartHospitalLogistics.Application;
using SmartHospitalLogistics.Infrastructure;

namespace SmartHospitalLogistics.Application.Tests;

public sealed class EnergyPerformancePersistenceTests
{
    [Fact]
    public void SqlitePersistenceRestoresSeededEnergyPerformanceAreas()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"smart-hospital-energy-{Guid.NewGuid():N}.db");
        try
        {
            var first = new EnergyPerformanceService(new SqliteEnergyPerformancePersistence(dbPath));
            var firstDetail = first.GetAreaDetail("ENE-POWER-B1");
            Assert.NotNull(firstDetail);

            var second = new EnergyPerformanceService(new SqliteEnergyPerformancePersistence(dbPath));
            var reloaded = second.GetAreaDetail("ENE-POWER-B1");

            Assert.NotNull(reloaded);
            Assert.Equal("PWR-LV-B1-IN-01", reloaded.Area.PrimaryMeterPointCode);
            Assert.Equal("BIM-ENE-B1-PDU", reloaded.Area.Location.BimElementId);
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
}
