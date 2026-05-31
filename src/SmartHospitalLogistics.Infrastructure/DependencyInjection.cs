using Microsoft.Extensions.DependencyInjection;
using SmartHospitalLogistics.Application;

namespace SmartHospitalLogistics.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IOperationsDashboardService, OperationsDashboardService>();
        services.AddSingleton<IWorkOrderPersistence>(_ =>
        {
            var dbPath = Environment.GetEnvironmentVariable("SMART_HOSPITAL_LOGISTICS_DB");
            if (string.IsNullOrWhiteSpace(dbPath))
            {
                dbPath = Path.Combine(AppContext.BaseDirectory, "data", "smart-hospital-logistics.db");
            }

            return new SqliteWorkOrderPersistence(dbPath);
        });
        services.AddSingleton<IAssetMaintenancePersistence>(_ =>
        {
            var dbPath = Environment.GetEnvironmentVariable("SMART_HOSPITAL_LOGISTICS_ASSET_DB");
            if (string.IsNullOrWhiteSpace(dbPath))
            {
                dbPath = Path.Combine(AppContext.BaseDirectory, "data", "smart-hospital-assets.db");
            }

            return new SqliteAssetMaintenancePersistence(dbPath);
        });
        services.AddSingleton<IIotIntegrationPersistence>(_ =>
        {
            var dbPath = Environment.GetEnvironmentVariable("SMART_HOSPITAL_LOGISTICS_IOT_DB");
            if (string.IsNullOrWhiteSpace(dbPath))
            {
                dbPath = Path.Combine(AppContext.BaseDirectory, "data", "smart-hospital-iot.db");
            }

            return new SqliteIotIntegrationPersistence(dbPath);
        });
        services.AddSingleton<IMonitoringAlarmPersistence>(_ =>
        {
            var dbPath = Environment.GetEnvironmentVariable("SMART_HOSPITAL_LOGISTICS_ALARM_DB");
            if (string.IsNullOrWhiteSpace(dbPath))
            {
                dbPath = Path.Combine(AppContext.BaseDirectory, "data", "smart-hospital-alarms.db");
            }

            return new SqliteMonitoringAlarmPersistence(dbPath);
        });
        services.AddSingleton<WorkOrderDispatchService>();
        services.AddSingleton<IWorkOrderDispatchService>(provider => provider.GetRequiredService<WorkOrderDispatchService>());
        services.AddSingleton<IWorkOrderIntakeService>(provider => provider.GetRequiredService<WorkOrderDispatchService>());
        services.AddSingleton<IAssetMaintenanceService, AssetMaintenanceService>();
        services.AddSingleton<MonitoringAlarmService>();
        services.AddSingleton<IMonitoringAlarmService>(provider => provider.GetRequiredService<MonitoringAlarmService>());
        services.AddSingleton<IMonitoringAlarmRecorder>(provider => provider.GetRequiredService<MonitoringAlarmService>());
        services.AddSingleton<IIotIntegrationService, IotIntegrationService>();
        return services;
    }
}
