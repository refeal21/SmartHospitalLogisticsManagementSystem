using SmartHospitalLogistics.Application;
using SmartHospitalLogistics.Domain;
using SmartHospitalLogistics.Infrastructure;

namespace SmartHospitalLogistics.Application.Tests;

public sealed class IotIntegrationPersistenceTests : IDisposable
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"smart-hospital-iot-{Guid.NewGuid():N}.db");

    [Fact]
    public void SqlitePersistenceRestoresCriticalTelemetryReadingAcrossServiceInstances()
    {
        var service = new IotIntegrationService(new SqliteIotIntegrationPersistence(_dbPath));
        var result = service.IngestReading(new TelemetryIngestionCommand(
            "MEDGAS-O2-8F",
            "pressure",
            0.31m,
            "MPa",
            new DateTimeOffset(2026, 5, 30, 10, 18, 0, TimeSpan.FromHours(8))));

        var reloaded = new IotIntegrationService(new SqliteIotIntegrationPersistence(_dbPath));
        var detail = reloaded.GetPointDetail("MEDGAS-O2-8F");

        Assert.True(result.Succeeded, result.ErrorMessage);
        Assert.NotNull(detail);
        Assert.Equal(0.31m, detail.RecentReadings[0].Value);
        Assert.Equal(TelemetryRiskLevel.Critical, detail.RecentReadings[0].RiskLevel);
        Assert.Contains("低于", detail.RecentReadings[0].RuleSummary);
    }

    [Fact]
    public void SqlitePersistenceRestoresCatalogPointsThresholdsAndSeedReadings()
    {
        _ = new IotIntegrationService(new SqliteIotIntegrationPersistence(_dbPath));

        var reloaded = new IotIntegrationService(new SqliteIotIntegrationPersistence(_dbPath));
        var catalog = reloaded.GetCatalog();
        var point = reloaded.GetPointDetail("SEWAGE-STATION-01");

        Assert.Contains(catalog.Systems, system => system.Category == IotSystemCategory.Sewage);
        Assert.Contains(catalog.Points, item => item.PointCode == "SEWAGE-STATION-01");
        Assert.Contains(catalog.ThresholdRules, rule => rule.PointCode == "SEWAGE-STATION-01" && rule.MetricCode == "cod");
        Assert.NotNull(point);
        Assert.NotEmpty(point.RecentReadings);
        Assert.Contains(point.Point.SourceEvidence, evidence => evidence.Sources.Contains("北建院"));
    }

    public void Dispose()
    {
        if (File.Exists(_dbPath))
        {
            File.Delete(_dbPath);
        }
    }
}
