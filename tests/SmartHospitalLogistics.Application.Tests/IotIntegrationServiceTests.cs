using SmartHospitalLogistics.Application;
using SmartHospitalLogistics.Domain;

namespace SmartHospitalLogistics.Application.Tests;

public sealed class IotIntegrationServiceTests
{
    private readonly IotIntegrationService _service = new();

    [Fact]
    public void CatalogCoversCustomerSystemCategoriesAndTraceableFields()
    {
        var catalog = _service.GetCatalog();

        Assert.Contains(catalog.Systems, system => system.Category == IotSystemCategory.StrongElectric);
        Assert.Contains(catalog.Systems, system => system.Category == IotSystemCategory.Hvac);
        Assert.Contains(catalog.Systems, system => system.Category == IotSystemCategory.WaterSupplyDrainage);
        Assert.Contains(catalog.Systems, system => system.Category == IotSystemCategory.MedicalGas);
        Assert.Contains(catalog.Systems, system => system.Category == IotSystemCategory.EnvironmentQuality);
        Assert.Contains(catalog.Systems, system => system.Category == IotSystemCategory.Sewage);
        Assert.Contains(catalog.Points, point =>
            point.PointCode == "PWR-LV-B1-IN-01" &&
            point.Metrics.Any(metric => metric.Code == "voltage") &&
            point.SourceEvidence.Any(evidence => evidence.Sources.Contains("北建院")));
        Assert.DoesNotContain(catalog.SourceEvidence, evidence =>
            evidence.Sources.Count == 1 && evidence.Sources.Contains("AI补充"));
    }

    [Fact]
    public void PointDetailReturnsLocationMetricsThresholdsAndHistory()
    {
        var detail = _service.GetPointDetail("MEDGAS-O2-8F");

        Assert.NotNull(detail);
        Assert.Equal("BIM-IPD-F8-WARD", detail.Point.Location.BimElementId);
        Assert.Contains(detail.Point.Metrics, metric => metric.Code == "pressure");
        Assert.Contains(detail.ThresholdRules, rule => rule.MetricCode == "pressure");
        Assert.NotEmpty(detail.RecentReadings);
    }

    [Fact]
    public void IngestReadingEvaluatesThresholdsAndStoresHistory()
    {
        var result = _service.IngestReading(new TelemetryIngestionCommand(
            "MEDGAS-O2-8F",
            "pressure",
            0.31m,
            "MPa",
            new DateTimeOffset(2026, 5, 30, 10, 0, 0, TimeSpan.FromHours(8))));

        Assert.True(result.Succeeded, result.ErrorMessage);
        Assert.Equal(TelemetryRiskLevel.Critical, result.RiskLevel);
        Assert.Contains("低于", result.RuleSummary);

        var detail = _service.GetPointDetail("MEDGAS-O2-8F");
        Assert.Equal(0.31m, detail!.RecentReadings[0].Value);
        Assert.Equal(TelemetryRiskLevel.Critical, detail.RecentReadings[0].RiskLevel);
    }

    [Fact]
    public void IngestReadingRejectsUnknownPointOrMetricWithoutMutation()
    {
        var missingPoint = _service.IngestReading(new TelemetryIngestionCommand(
            "POINT-NOT-FOUND",
            "pressure",
            1m,
            "MPa",
            DateTimeOffset.UtcNow));

        var missingMetric = _service.IngestReading(new TelemetryIngestionCommand(
            "MEDGAS-O2-8F",
            "not-a-metric",
            1m,
            "MPa",
            DateTimeOffset.UtcNow));

        Assert.False(missingPoint.Succeeded);
        Assert.True(missingPoint.NotFound);
        Assert.False(missingMetric.Succeeded);
        Assert.False(missingMetric.NotFound);
    }
}
