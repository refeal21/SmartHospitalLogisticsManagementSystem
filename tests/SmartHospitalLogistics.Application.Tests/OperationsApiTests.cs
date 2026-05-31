using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using SmartHospitalLogistics.Application;
using SmartHospitalLogistics.Domain;

namespace SmartHospitalLogistics.Application.Tests;

public sealed class OperationsApiTests : IClassFixture<IsolatedOperationsApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _client;

    public OperationsApiTests(IsolatedOperationsApiFactory factory)
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

    [Fact]
    public async Task ServiceRequestApisCreateRequestAndConvertToWorkOrder()
    {
        var createResponse = await _client.PostAsJsonAsync(
            "/api/operations/service-requests",
            new CreateServiceRequestCommand(
                "Manual",
                "门诊护士站",
                "门诊部",
                "综合维修",
                Priority.High,
                "门诊大厅空调异常，候诊区温度偏高",
                new SpatialLocation("同仁亦庄院区", "门诊医技楼", "F1", "共享大厅", "BIM-OPD-F1-HALL")),
            JsonOptions);
        createResponse.EnsureSuccessStatusCode();

        var request = await createResponse.Content.ReadFromJsonAsync<ServiceRequest>(JsonOptions);
        Assert.Equal(ServiceRequestStatus.Accepted, request?.Status);

        var convertResponse = await _client.PostAsJsonAsync(
            $"/api/operations/service-requests/{request!.RequestNo}/convert",
            new ConvertServiceRequestCommand("一站式受理员", "信息完整，生成待派工单"),
            JsonOptions);
        convertResponse.EnsureSuccessStatusCode();

        var detail = await convertResponse.Content.ReadFromJsonAsync<WorkOrderDetail>(JsonOptions);
        Assert.NotNull(detail);
        Assert.StartsWith("WO-SR-", detail.WorkOrder.WorkOrderNo);
        Assert.Equal(WorkOrderStatus.New, detail.WorkOrder.Status);
    }
}
