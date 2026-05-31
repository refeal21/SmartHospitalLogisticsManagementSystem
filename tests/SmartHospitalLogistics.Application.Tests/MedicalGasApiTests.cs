using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using SmartHospitalLogistics.Domain;

namespace SmartHospitalLogistics.Application.Tests;

public sealed class MedicalGasApiTests : IClassFixture<IsolatedOperationsApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _client;

    public MedicalGasApiTests(IsolatedOperationsApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task MedicalGasBoardApiReturnsZonePointAssetAndTraceability()
    {
        var board = await _client.GetFromJsonAsync<MedicalGasBoard>("/api/operations/medical-gas-board", JsonOptions);

        Assert.NotNull(board);
        Assert.Contains(board.Zones, zone => zone.ZoneCode == "MG-ZONE-IPD-8F");
        Assert.Contains(board.MonitoringPoints, point => point.PointCode == "MEDGAS-O2-8F");
        Assert.Contains(board.ValveAssets, asset => asset.AssetCode == "MEDGAS-IPD-8F");
        Assert.Contains(board.SourceEvidence, evidence => evidence.Sources.Contains("PPT"));
    }

    [Fact]
    public async Task MedicalGasZoneApiIncludesAlarmContextAfterCriticalTelemetryIngestion()
    {
        var ingestionResponse = await _client.PostAsJsonAsync(
            "/api/operations/iot-readings",
            new TelemetryIngestionCommand(
                "MEDGAS-O2-8F",
                "pressure",
                0.31m,
                "MPa",
                new DateTimeOffset(2026, 5, 30, 10, 15, 0, TimeSpan.FromHours(8))),
            JsonOptions);
        ingestionResponse.EnsureSuccessStatusCode();

        var detail = await _client.GetFromJsonAsync<MedicalGasZoneDetail>(
            "/api/operations/medical-gas-zones/MG-ZONE-IPD-8F",
            JsonOptions);

        Assert.NotNull(detail);
        Assert.Equal("BIM-IPD-F8-WARD", detail.Zone.Location.BimElementId);
        Assert.Contains(detail.ActiveAlarms, alarm => alarm.PointCode == "MEDGAS-O2-8F");
        Assert.Equal("MEDGAS-O2-8F", detail.MonitoringPoint?.Point.PointCode);
        Assert.Equal("MEDGAS-IPD-8F", detail.ValveAsset?.Asset.AssetCode);
    }

    [Fact]
    public async Task MedicalGasZoneApiReturns404ForMissingZone()
    {
        var response = await _client.GetAsync("/api/operations/medical-gas-zones/MG-ZONE-NOT-FOUND");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
