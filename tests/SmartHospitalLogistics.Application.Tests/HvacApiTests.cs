using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using SmartHospitalLogistics.Domain;

namespace SmartHospitalLogistics.Application.Tests;

public sealed class HvacApiTests : IClassFixture<IsolatedOperationsApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _client;

    public HvacApiTests(IsolatedOperationsApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task HvacBoardApiReturnsLoopPointAssetAndTraceability()
    {
        var board = await _client.GetFromJsonAsync<HvacBoard>("/api/operations/hvac-board", JsonOptions);

        Assert.NotNull(board);
        Assert.Contains(board.Loops, loop => loop.LoopCode == "HVAC-LOOP-B1-CHW");
        Assert.Contains(board.MonitoringPoints, point => point.PointCode == "HVAC-CHW-B1-02");
        Assert.Contains(board.HvacAssets, asset => asset.AssetCode == "CHW-B1-02");
        Assert.Contains(board.SourceEvidence, evidence =>
            evidence.Sources.Contains("北建院") &&
            evidence.Sources.Contains("中科医信") &&
            evidence.Sources.Contains("PPT"));
    }

    [Fact]
    public async Task HvacLoopApiIncludesAlarmContextAfterCriticalPressureIngestion()
    {
        var ingestionResponse = await _client.PostAsJsonAsync(
            "/api/operations/iot-readings",
            new TelemetryIngestionCommand(
                "HVAC-CHW-B1-02",
                "pressure",
                0.8m,
                "MPa",
                new DateTimeOffset(2026, 5, 30, 10, 40, 0, TimeSpan.FromHours(8))),
            JsonOptions);
        ingestionResponse.EnsureSuccessStatusCode();

        var detail = await _client.GetFromJsonAsync<HvacLoopDetail>(
            "/api/operations/hvac-cooling-loops/HVAC-LOOP-B1-CHW",
            JsonOptions);

        Assert.NotNull(detail);
        Assert.Equal("BIM-ENE-B1-CHILLER", detail.Loop.Location.BimElementId);
        Assert.Contains(detail.ActiveAlarms, alarm => alarm.PointCode == "HVAC-CHW-B1-02");
        Assert.Equal("HVAC-CHW-B1-02", detail.MonitoringPoint?.Point.PointCode);
        Assert.Equal("CHW-B1-02", detail.HvacAsset?.Asset.AssetCode);
    }

    [Fact]
    public async Task HvacLoopApiReturns404ForMissingLoop()
    {
        var response = await _client.GetAsync("/api/operations/hvac-cooling-loops/HVAC-LOOP-NOT-FOUND");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
