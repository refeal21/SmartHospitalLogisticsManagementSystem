using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SmartHospitalLogistics.Application;
using SmartHospitalLogistics.Infrastructure;

namespace SmartHospitalLogistics.Application.Tests;

public sealed class IsolatedOperationsApiFactory : WebApplicationFactory<Program>
{
    private readonly string _dbDirectory = Path.Combine(Path.GetTempPath(), $"smart-hospital-api-{Guid.NewGuid():N}");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IWorkOrderPersistence>();
            services.RemoveAll<IAssetMaintenancePersistence>();
            services.RemoveAll<IIotIntegrationPersistence>();
            services.RemoveAll<IMonitoringAlarmPersistence>();
            services.RemoveAll<IWorkOrderDispatchService>();
            services.RemoveAll<IAssetMaintenanceService>();
            services.RemoveAll<MonitoringAlarmService>();
            services.RemoveAll<IMonitoringAlarmService>();
            services.RemoveAll<IMonitoringAlarmRecorder>();
            services.RemoveAll<IIotIntegrationService>();

            services.AddSingleton<IWorkOrderPersistence>(_ =>
                new SqliteWorkOrderPersistence(Path.Combine(_dbDirectory, "work-orders.db")));
            services.AddSingleton<IAssetMaintenancePersistence>(_ =>
                new SqliteAssetMaintenancePersistence(Path.Combine(_dbDirectory, "assets.db")));
            services.AddSingleton<IIotIntegrationPersistence>(_ =>
                new SqliteIotIntegrationPersistence(Path.Combine(_dbDirectory, "iot.db")));
            services.AddSingleton<IMonitoringAlarmPersistence>(_ =>
                new SqliteMonitoringAlarmPersistence(Path.Combine(_dbDirectory, "alarms.db")));

            services.AddSingleton<IWorkOrderDispatchService, WorkOrderDispatchService>();
            services.AddSingleton<IAssetMaintenanceService, AssetMaintenanceService>();
            services.AddSingleton<MonitoringAlarmService>();
            services.AddSingleton<IMonitoringAlarmService>(provider => provider.GetRequiredService<MonitoringAlarmService>());
            services.AddSingleton<IMonitoringAlarmRecorder>(provider => provider.GetRequiredService<MonitoringAlarmService>());
            services.AddSingleton<IIotIntegrationService, IotIntegrationService>();
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (Directory.Exists(_dbDirectory))
        {
            Directory.Delete(_dbDirectory, recursive: true);
        }
    }
}
