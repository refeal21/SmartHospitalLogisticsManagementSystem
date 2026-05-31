using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using SmartHospitalLogistics.Domain;

namespace SmartHospitalLogistics.Application.Tests;

public sealed class PlatformGovernanceApiTests : IClassFixture<IsolatedOperationsApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _client;

    public PlatformGovernanceApiTests(IsolatedOperationsApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GovernanceBoardApiReturnsControlsPerformanceAndTraceability()
    {
        var board = await _client.GetFromJsonAsync<PlatformGovernanceBoard>(
            "/api/operations/platform-governance-board",
            JsonOptions);

        Assert.NotNull(board);
        Assert.Contains(board.Controls, control => control.ControlCode == "GOV-SLA-ONE-STOP");
        Assert.Contains(board.Controls, control => control.ControlCode == "GOV-ENERGY-COST");
        Assert.Contains(board.PerformanceMetrics, metric => metric.MetricCode == "governance-closure-rate");
        Assert.Contains(board.SourceEvidence, evidence =>
            evidence.Sources.Contains("PPT") &&
            evidence.Sources.Count >= 2);
    }

    [Fact]
    public async Task GovernanceActionApiRecordsActionAndReturnsUpdatedDetail()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/operations/platform-governance-controls/GOV-SLA-ONE-STOP/actions",
            new CreateGovernanceActionCommand(
                "登记 SLA 复盘整改动作",
                "后勤调度员",
                "后勤管理部"),
            JsonOptions);

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<PlatformGovernanceActionResult>(JsonOptions);

        Assert.NotNull(result);
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Detail);
        Assert.Contains(result.Detail.Actions, action => action.Title.Contains("SLA"));
        Assert.Contains(result.Detail.AuditTrail, entry => entry.Operation == "RecordAction");
    }

    [Fact]
    public async Task GovernanceControlApiReturns404ForMissingControl()
    {
        var response = await _client.GetAsync("/api/operations/platform-governance-controls/GOV-NOT-FOUND");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
