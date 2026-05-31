using SmartHospitalLogistics.Application;
using SmartHospitalLogistics.Domain;
using SmartHospitalLogistics.Infrastructure;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
builder.Services.AddOpenApi();
builder.Services.AddCors(options =>
{
    options.AddPolicy("DevelopmentCors", policy =>
    {
        policy
            .WithOrigins("http://localhost:5173", "http://127.0.0.1:5173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});
builder.Services.AddInfrastructure();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseCors("DevelopmentCors");
}

app.UseHttpsRedirection();

var api = app.MapGroup("/api/operations").WithTags("Operations");
var logistics = app.MapGroup("/api/logistics").WithTags("Logistics");

logistics.MapGet("/blueprint", (IOperationsDashboardService service) => service.GetLogisticsBlueprint())
    .WithName("GetLogisticsBlueprint");

api.MapGet("/dashboard", (IOperationsDashboardService service) => service.GetDashboard())
    .WithName("GetOperationsDashboard");

api.MapGet("/dispatch-board", (IWorkOrderDispatchService service) => service.GetDispatchBoard())
    .WithName("GetDispatchBoard");

api.MapGet("/work-orders/active", (IOperationsDashboardService service) => service.GetActiveWorkOrders())
    .WithName("GetActiveWorkOrders");

api.MapGet("/work-orders/{workOrderNo}", (string workOrderNo, IWorkOrderDispatchService service) =>
    service.GetWorkOrderDetail(workOrderNo) is { } detail
        ? Results.Ok(detail)
        : Results.NotFound(new { error = $"Work order {workOrderNo} was not found." }))
    .WithName("GetWorkOrderDetail");

api.MapPost("/work-orders/{workOrderNo}/dispatch", (
        string workOrderNo,
        DispatchWorkOrderCommand command,
        IWorkOrderDispatchService service) =>
    {
        var result = service.Dispatch(workOrderNo, command);
        return ToHttpResult(result);
    })
    .WithName("DispatchWorkOrder");

api.MapPost("/work-orders/{workOrderNo}/transition", (
        string workOrderNo,
        TransitionWorkOrderCommand command,
        IWorkOrderDispatchService service) =>
    {
        var result = service.Transition(workOrderNo, command);
        return ToHttpResult(result);
    })
    .WithName("TransitionWorkOrder");

api.MapGet("/assets/risk", (IOperationsDashboardService service) => service.GetRiskAssets())
    .WithName("GetRiskAssets");

api.MapGet("/asset-maintenance-board", (IAssetMaintenanceService service) => service.GetMaintenanceBoard())
    .WithName("GetAssetMaintenanceBoard");

api.MapGet("/assets/{assetCode}/maintenance", (string assetCode, IAssetMaintenanceService service) =>
    service.GetAssetMaintenanceDetail(assetCode) is { } detail
        ? Results.Ok(detail)
        : Results.NotFound(new { error = $"Asset {assetCode} was not found." }))
    .WithName("GetAssetMaintenanceDetail");

api.MapPost("/maintenance-tasks/{taskNo}/complete", (
        string taskNo,
        CompleteMaintenanceTaskCommand command,
        IAssetMaintenanceService service) =>
    {
        var result = service.CompleteTask(taskNo, command);
        return ToMaintenanceHttpResult(result);
    })
    .WithName("CompleteMaintenanceTask");

app.Run();

static IResult ToHttpResult(DispatchOperationResult result)
{
    if (result.Succeeded && result.Detail is not null)
    {
        return Results.Ok(result.Detail);
    }

    return result.NotFound
        ? Results.NotFound(new { error = result.ErrorMessage })
        : Results.BadRequest(new { error = result.ErrorMessage });
}

static IResult ToMaintenanceHttpResult(MaintenanceTaskOperationResult result)
{
    if (result.Succeeded)
    {
        return Results.Ok(result);
    }

    return result.NotFound
        ? Results.NotFound(new { error = result.ErrorMessage })
        : Results.BadRequest(new { error = result.ErrorMessage });
}

public partial class Program;
