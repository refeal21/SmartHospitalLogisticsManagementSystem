using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using SmartHospitalLogistics.Domain;

namespace SmartHospitalLogistics.Application.Tests;

public sealed class IotIntegrationApiTests : IClassFixture<IsolatedOperationsApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _client;

    public IotIntegrationApiTests(IsolatedOperationsApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task IotCatalogApiReturnsPointsAndSourceEvidence()
    {
        var catalog = await _client.GetFromJsonAsync<IotIntegrationCatalog>(
            "/api/operations/iot-catalog",
            JsonOptions);

        Assert.NotNull(catalog);
        Assert.NotEmpty(catalog.Systems);
        Assert.NotEmpty(catalog.Points);
        Assert.Contains(catalog.SourceEvidence, evidence => evidence.Sources.Contains("北建院"));
    }

    [Fact]
    public async Task IotPointDetailApiReturnsMetricHistoryAndThresholds()
    {
        var detail = await _client.GetFromJsonAsync<IotPointDetail>(
            "/api/operations/iot-points/MEDGAS-O2-8F",
            JsonOptions);

        Assert.NotNull(detail);
        Assert.Equal("MEDGAS-O2-8F", detail.Point.PointCode);
        Assert.NotEmpty(detail.ThresholdRules);
        Assert.NotEmpty(detail.RecentReadings);
    }

    [Fact]
    public async Task IotReadingApiEvaluatesCriticalRisk()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/operations/iot-readings",
            new TelemetryIngestionCommand(
                "ENV-WARD-CO2-8F",
                "co2",
                1300m,
                "ppm",
                new DateTimeOffset(2026, 5, 30, 10, 10, 0, TimeSpan.FromHours(8))));

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<TelemetryIngestionResult>(JsonOptions);
        Assert.True(result?.Succeeded);
        Assert.Equal(TelemetryRiskLevel.Critical, result?.RiskLevel);
    }

    [Fact]
    public async Task CriticalIotReadingApiCreatesActionableMonitoringAlarm()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/operations/iot-readings",
            new TelemetryIngestionCommand(
                "MEDGAS-O2-8F",
                "pressure",
                0.31m,
                "MPa",
                new DateTimeOffset(2026, 5, 30, 10, 28, 0, TimeSpan.FromHours(8))));
        response.EnsureSuccessStatusCode();

        var board = await _client.GetFromJsonAsync<MonitoringAlarmBoard>(
            "/api/operations/monitoring-alarms",
            JsonOptions);
        var alarm = Assert.Single(board!.Alarms, item =>
            item.PointCode == "MEDGAS-O2-8F" &&
            item.MetricCode == "pressure");
        Assert.Equal(MonitoringAlarmStatus.New, alarm.Status);
        Assert.Equal("BIM-IPD-F8-WARD", alarm.Location.BimElementId);

        var acknowledgedResponse = await _client.PostAsJsonAsync(
            $"/api/operations/monitoring-alarms/{alarm.AlarmNo}/acknowledge",
            new AcknowledgeAlarmCommand("医气维保人员", "已确认氧气压力异常"));
        acknowledgedResponse.EnsureSuccessStatusCode();

        var convertResponse = await _client.PostAsJsonAsync(
            $"/api/operations/monitoring-alarms/{alarm.AlarmNo}/convert-to-work-order",
            new ConvertAlarmToWorkOrderCommand("调度员", "医气维保人员", "转入医气专项处置工单"));
        convertResponse.EnsureSuccessStatusCode();

        var converted = await convertResponse.Content.ReadFromJsonAsync<MonitoringAlarmEvent>(JsonOptions);
        Assert.Equal(MonitoringAlarmStatus.ConvertedToWorkOrder, converted?.Status);
        Assert.StartsWith("WO-ALM-", converted?.WorkOrderNo);

        var workOrder = await _client.GetFromJsonAsync<WorkOrderDetail>(
            $"/api/operations/work-orders/{converted!.WorkOrderNo}",
            JsonOptions);
        Assert.NotNull(workOrder);
        Assert.Contains(workOrder.SourceEvidence, evidence => evidence.FeatureName == "客户物联告警联动");

        var dispatchResponse = await _client.PostAsJsonAsync(
            $"/api/operations/work-orders/{converted.WorkOrderNo}/dispatch",
            new DispatchWorkOrderCommand("医气维保人员", "调度员", "按医气压力告警派工"));
        dispatchResponse.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task IotApisReturn404ForMissingPointAnd400ForUnknownMetric()
    {
        var missingPoint = await _client.GetAsync("/api/operations/iot-points/POINT-NOT-FOUND");
        Assert.Equal(HttpStatusCode.NotFound, missingPoint.StatusCode);

        var unknownMetric = await _client.PostAsJsonAsync(
            "/api/operations/iot-readings",
            new TelemetryIngestionCommand(
                "MEDGAS-O2-8F",
                "not-a-metric",
                1m,
                "MPa",
                DateTimeOffset.UtcNow));
        Assert.Equal(HttpStatusCode.BadRequest, unknownMetric.StatusCode);
    }
}
