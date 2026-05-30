using SmartHospitalLogistics.Application;
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

api.MapGet("/work-orders/active", (IOperationsDashboardService service) => service.GetActiveWorkOrders())
    .WithName("GetActiveWorkOrders");

api.MapGet("/assets/risk", (IOperationsDashboardService service) => service.GetRiskAssets())
    .WithName("GetRiskAssets");

app.Run();
