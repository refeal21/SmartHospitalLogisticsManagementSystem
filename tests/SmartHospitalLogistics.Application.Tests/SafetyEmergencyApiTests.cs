using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using SmartHospitalLogistics.Domain;

namespace SmartHospitalLogistics.Application.Tests;

public sealed class SafetyEmergencyApiTests : IClassFixture<IsolatedOperationsApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _client;

    public SafetyEmergencyApiTests(IsolatedOperationsApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task SafetyEmergencyBoardApiReturnsNodesPointsAssetsAndTraceability()
    {
        var board = await _client.GetFromJsonAsync<SafetyEmergencyBoard>(
            "/api/operations/safety-emergency-board",
            JsonOptions);

        Assert.NotNull(board);
        Assert.Contains(board.Nodes, node => node.NodeCode == "SAFE-FIRE-OPD-1F");
        Assert.Contains(board.Nodes, node => node.NodeCode == "SAFE-SEC-ER-ACCESS");
        Assert.Contains(board.MonitoringPoints, point => point.PointCode == "FIRE-SMOKE-OPD-1F-01");
        Assert.Contains(board.SafetyAssets, asset => asset.AssetCode == "FIRE-ALARM-OPD-1F");
        Assert.Contains(board.SourceEvidence, evidence =>
            evidence.Sources.Contains("北建院") &&
            evidence.Sources.Contains("PPT"));
    }

    [Fact]
    public async Task SafetyEmergencyNodeApiIncludesAlarmContextAfterFireSignal()
    {
        var ingestionResponse = await _client.PostAsJsonAsync(
            "/api/operations/iot-readings",
            new TelemetryIngestionCommand(
                "FIRE-SMOKE-OPD-1F-01",
                "smoke_density",
                1.7m,
                "%obs/m",
                new DateTimeOffset(2026, 5, 30, 10, 35, 0, TimeSpan.FromHours(8))),
            JsonOptions);
        ingestionResponse.EnsureSuccessStatusCode();

        var detail = await _client.GetFromJsonAsync<SafetyEmergencyNodeDetail>(
            "/api/operations/safety-emergency-nodes/SAFE-FIRE-OPD-1F",
            JsonOptions);

        Assert.NotNull(detail);
        Assert.Equal("BIM-SEC-OPD-1F-FIRE", detail.Node.Location.BimElementId);
        Assert.Equal("FIRE-SMOKE-OPD-1F-01", detail.MonitoringPoint?.Point.PointCode);
        Assert.Contains(detail.ActiveAlarms, alarm => alarm.PointCode == "FIRE-SMOKE-OPD-1F-01");
        Assert.Contains(detail.ResponseProcedure, step => step.Action.Contains("确认", StringComparison.Ordinal));
    }

    [Fact]
    public async Task SafetyEmergencyNodeApiReturns404ForMissingNode()
    {
        var response = await _client.GetAsync("/api/operations/safety-emergency-nodes/SAFE-NOT-FOUND");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
