using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using SmartHospitalLogistics.Domain;

namespace SmartHospitalLogistics.Application.Tests;

public sealed class WaterOperationsApiTests : IClassFixture<IsolatedOperationsApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _client;

    public WaterOperationsApiTests(IsolatedOperationsApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task WaterOperationsBoardApiReturnsWaterSewageUnitsAndTraceability()
    {
        var board = await _client.GetFromJsonAsync<WaterOperationsBoard>(
            "/api/operations/water-operations-board",
            JsonOptions);

        Assert.NotNull(board);
        Assert.Contains(board.Units, unit => unit.UnitCode == "WATER-SYS-B1-PUMP");
        Assert.Contains(board.Units, unit => unit.UnitCode == "SEWAGE-SYS-B1-TREATMENT");
        Assert.Contains(board.MonitoringPoints, point => point.PointCode == "WATER-PUMP-B1-01");
        Assert.Contains(board.MonitoringPoints, point => point.PointCode == "SEWAGE-STATION-01");
        Assert.Contains(board.SourceEvidence, evidence =>
            evidence.Sources.Contains("北建院") &&
            evidence.Sources.Contains("中科医信") &&
            evidence.Sources.Contains("PPT"));
    }

    [Fact]
    public async Task WaterOperationsUnitApiIncludesAlarmContextAfterCriticalSewageIngestion()
    {
        var ingestionResponse = await _client.PostAsJsonAsync(
            "/api/operations/iot-readings",
            new TelemetryIngestionCommand(
                "SEWAGE-STATION-01",
                "cod",
                260m,
                "mg/L",
                new DateTimeOffset(2026, 5, 30, 10, 50, 0, TimeSpan.FromHours(8))),
            JsonOptions);
        ingestionResponse.EnsureSuccessStatusCode();

        var detail = await _client.GetFromJsonAsync<WaterOperationsUnitDetail>(
            "/api/operations/water-operations-units/SEWAGE-SYS-B1-TREATMENT",
            JsonOptions);

        Assert.NotNull(detail);
        Assert.Equal("BIM-LOG-B1-SEWAGE", detail.Unit.Location.BimElementId);
        Assert.Equal("SEWAGE-STATION-01", detail.MonitoringPoint?.Point.PointCode);
        Assert.Contains(detail.ActiveAlarms, alarm => alarm.PointCode == "SEWAGE-STATION-01");
    }

    [Fact]
    public async Task WaterOperationsUnitApiReturns404ForMissingUnit()
    {
        var response = await _client.GetAsync("/api/operations/water-operations-units/WATER-NOT-FOUND");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
