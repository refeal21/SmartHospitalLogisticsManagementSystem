using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.Testing;
using SmartHospitalLogistics.Application;
using SmartHospitalLogistics.Domain;

namespace SmartHospitalLogistics.Application.Tests;

public sealed class OperationsApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _client;

    public OperationsApiTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task DispatchBoardApiReturnsWorkOrderPoolTeamLoadsAndSlaRisk()
    {
        var board = await _client.GetFromJsonAsync<DispatchBoard>("/api/operations/dispatch-board", JsonOptions);

        Assert.NotNull(board);
        Assert.NotEmpty(board.WorkOrders);
        Assert.NotEmpty(board.TeamLoads);
        Assert.NotEmpty(board.Recommendations);
        Assert.Equal("High", board.SlaRisk.HighestRiskLevel);
    }

    [Fact]
    public async Task DispatchAndTransitionApisReturnUpdatedWorkOrderDetail()
    {
        var dispatchResponse = await _client.PostAsJsonAsync(
            "/api/operations/work-orders/WO-20260530-0001/dispatch",
            new DispatchWorkOrderCommand("环境监管班组", "调度员", "医废负压风险优先派工"));
        dispatchResponse.EnsureSuccessStatusCode();

        var dispatched = await dispatchResponse.Content.ReadFromJsonAsync<WorkOrderDetail>(JsonOptions);
        Assert.Equal(WorkOrderStatus.Dispatched, dispatched?.WorkOrder.Status);

        var transitionResponse = await _client.PostAsJsonAsync(
            "/api/operations/work-orders/WO-20260530-0001/transition",
            new TransitionWorkOrderCommand(WorkOrderTransitionAction.Accept, "环境监管班组", "已到现场"));
        transitionResponse.EnsureSuccessStatusCode();

        var accepted = await transitionResponse.Content.ReadFromJsonAsync<WorkOrderDetail>(JsonOptions);
        Assert.Equal(WorkOrderStatus.Accepted, accepted?.WorkOrder.Status);
        Assert.Contains(accepted!.Timeline, entry => entry.Action == "接单");
    }

    [Fact]
    public async Task WorkOrderApisReturn404ForMissingOrderAnd400ForInvalidTransition()
    {
        var missing = await _client.GetAsync("/api/operations/work-orders/WO-NOT-FOUND");
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);

        var invalid = await _client.PostAsJsonAsync(
            "/api/operations/work-orders/WO-20260530-0002/transition",
            new TransitionWorkOrderCommand(WorkOrderTransitionAction.Evaluate, "护士站", "不能提前评价", Rating: 3));

        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
    }
}
