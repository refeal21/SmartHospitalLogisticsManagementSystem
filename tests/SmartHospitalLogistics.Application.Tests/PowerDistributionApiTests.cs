using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using SmartHospitalLogistics.Domain;

namespace SmartHospitalLogistics.Application.Tests;

public sealed class PowerDistributionApiTests : IClassFixture<IsolatedOperationsApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _client;

    public PowerDistributionApiTests(IsolatedOperationsApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task PowerDistributionBoardApiReturnsCircuitPointAssetAndTraceability()
    {
        var board = await _client.GetFromJsonAsync<PowerDistributionBoard>("/api/operations/power-distribution-board", JsonOptions);

        Assert.NotNull(board);
        Assert.Contains(board.Circuits, circuit => circuit.CircuitCode == "PWR-CIRCUIT-B1-LV-IN");
        Assert.Contains(board.MonitoringPoints, point => point.PointCode == "PWR-LV-B1-IN-01");
        Assert.Contains(board.ElectricalAssets, asset => asset.AssetCode == "PWR-LV-B1-IN-CAB");
        Assert.Contains(board.SourceEvidence, evidence =>
            evidence.Sources.Contains("北建院") &&
            evidence.Sources.Contains("中科医信") &&
            evidence.Sources.Contains("PPT"));
    }

    [Fact]
    public async Task PowerDistributionCircuitApiIncludesAlarmContextAfterCriticalVoltageIngestion()
    {
        var ingestionResponse = await _client.PostAsJsonAsync(
            "/api/operations/iot-readings",
            new TelemetryIngestionCommand(
                "PWR-LV-B1-IN-01",
                "voltage",
                260m,
                "V",
                new DateTimeOffset(2026, 5, 30, 10, 25, 0, TimeSpan.FromHours(8))),
            JsonOptions);
        ingestionResponse.EnsureSuccessStatusCode();

        var detail = await _client.GetFromJsonAsync<PowerDistributionCircuitDetail>(
            "/api/operations/power-distribution-circuits/PWR-CIRCUIT-B1-LV-IN",
            JsonOptions);

        Assert.NotNull(detail);
        Assert.Equal("BIM-ENE-B1-PDU", detail.Circuit.Location.BimElementId);
        Assert.Contains(detail.ActiveAlarms, alarm => alarm.PointCode == "PWR-LV-B1-IN-01");
        Assert.Equal("PWR-LV-B1-IN-01", detail.MonitoringPoint?.Point.PointCode);
        Assert.Equal("PWR-LV-B1-IN-CAB", detail.ElectricalAsset?.Asset.AssetCode);
    }

    [Fact]
    public async Task PowerDistributionCircuitApiReturns404ForMissingCircuit()
    {
        var response = await _client.GetAsync("/api/operations/power-distribution-circuits/PWR-CIRCUIT-NOT-FOUND");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
