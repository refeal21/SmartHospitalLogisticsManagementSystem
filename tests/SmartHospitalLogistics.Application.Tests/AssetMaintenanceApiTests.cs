using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.Testing;
using SmartHospitalLogistics.Domain;

namespace SmartHospitalLogistics.Application.Tests;

public sealed class AssetMaintenanceApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _client;

    public AssetMaintenanceApiTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task MaintenanceBoardApiReturnsAssetLedgerPlansTasksAndEvidence()
    {
        var board = await _client.GetFromJsonAsync<AssetMaintenanceBoard>(
            "/api/operations/asset-maintenance-board",
            JsonOptions);

        Assert.NotNull(board);
        Assert.NotEmpty(board.Assets);
        Assert.NotEmpty(board.Plans);
        Assert.NotEmpty(board.DueTasks);
        Assert.NotEmpty(board.SourceEvidence);
        Assert.True(board.Kpis.RiskAssets >= 1);
    }

    [Fact]
    public async Task AssetMaintenanceDetailApiReturnsBimLocationAndLifecycle()
    {
        var detail = await _client.GetFromJsonAsync<AssetMaintenanceDetail>(
            "/api/operations/assets/MEDGAS-IPD-8F/maintenance",
            JsonOptions);

        Assert.NotNull(detail);
        Assert.Equal("MEDGAS-IPD-8F", detail.Asset.AssetCode);
        Assert.Equal("BIM-IPD-F8-WARD", detail.Asset.Location.BimElementId);
        Assert.NotEmpty(detail.Lifecycle);
    }

    [Fact]
    public async Task CompleteMaintenanceTaskApiCanCreateFollowUpWorkOrder()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/operations/maintenance-tasks/MT-20260530-0001/complete",
            new CompleteMaintenanceTaskCommand(
                "环境监管班组",
                MaintenanceOutcome.Abnormal,
                "医废暂存间负压巡检异常，转维修工单闭环",
                [new InspectionChecklistResult("CHK-NEGATIVE-PRESSURE", "异常", "负压低于阈值")],
                ConvertToWorkOrder: true));

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<MaintenanceTaskOperationResult>(JsonOptions);
        Assert.True(result?.Succeeded);
        Assert.Equal(MaintenanceTaskStatus.ConvertedToWorkOrder, result?.Task?.Status);
        Assert.NotNull(result?.GeneratedWorkOrder);
        Assert.Equal(WorkOrderStatus.New, result.GeneratedWorkOrder.Status);
    }

    [Fact]
    public async Task MaintenanceApisReturn404ForMissingAssetOrTask()
    {
        var missingAsset = await _client.GetAsync("/api/operations/assets/ASSET-NOT-FOUND/maintenance");
        Assert.Equal(HttpStatusCode.NotFound, missingAsset.StatusCode);

        var missingTask = await _client.PostAsJsonAsync(
            "/api/operations/maintenance-tasks/MT-NOT-FOUND/complete",
            new CompleteMaintenanceTaskCommand(
                "调度员",
                MaintenanceOutcome.Normal,
                "不存在的任务",
                [],
                ConvertToWorkOrder: false));
        Assert.Equal(HttpStatusCode.NotFound, missingTask.StatusCode);
    }
}
