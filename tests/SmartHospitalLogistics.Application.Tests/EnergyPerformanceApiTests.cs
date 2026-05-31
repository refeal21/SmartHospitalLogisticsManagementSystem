using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using SmartHospitalLogistics.Domain;

namespace SmartHospitalLogistics.Application.Tests;

public sealed class EnergyPerformanceApiTests : IClassFixture<IsolatedOperationsApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _client;

    public EnergyPerformanceApiTests(IsolatedOperationsApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task EnergyPerformanceBoardApiReturnsAreasMetersRecommendationsAndTraceability()
    {
        var board = await _client.GetFromJsonAsync<EnergyPerformanceBoard>(
            "/api/operations/energy-performance-board",
            JsonOptions);

        Assert.NotNull(board);
        Assert.Contains(board.Areas, area => area.AreaCode == "ENE-POWER-B1");
        Assert.Contains(board.Areas, area => area.AreaCode == "ENE-HVAC-B1");
        Assert.Contains(board.Areas, area => area.AreaCode == "ENE-WATER-B1");
        Assert.Contains(board.MeterPoints, point => point.PointCode == "PWR-LV-B1-IN-01");
        Assert.Contains(board.SavingRecommendations, item => item.RelatedAreaCode == "ENE-HVAC-B1");
        Assert.Contains(board.SourceEvidence, evidence =>
            evidence.Sources.Contains("中科医信") &&
            evidence.Sources.Contains("PPT"));
    }

    [Fact]
    public async Task EnergyPerformanceAreaApiIncludesAlarmContextAfterPowerAnomaly()
    {
        var ingestionResponse = await _client.PostAsJsonAsync(
            "/api/operations/iot-readings",
            new TelemetryIngestionCommand(
                "PWR-LV-B1-IN-01",
                "voltage",
                260m,
                "V",
                new DateTimeOffset(2026, 5, 30, 11, 0, 0, TimeSpan.FromHours(8))),
            JsonOptions);
        ingestionResponse.EnsureSuccessStatusCode();

        var detail = await _client.GetFromJsonAsync<EnergyPerformanceAreaDetail>(
            "/api/operations/energy-performance-areas/ENE-POWER-B1",
            JsonOptions);

        Assert.NotNull(detail);
        Assert.Equal("BIM-ENE-B1-PDU", detail.Area.Location.BimElementId);
        Assert.Contains(detail.ActiveAlarms, alarm => alarm.PointCode == "PWR-LV-B1-IN-01");
        Assert.Contains(detail.Trend, point => point.AreaCode == "ENE-POWER-B1");
    }

    [Fact]
    public async Task EnergyPerformanceAreaApiReturns404ForMissingArea()
    {
        var response = await _client.GetAsync("/api/operations/energy-performance-areas/ENE-NOT-FOUND");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
