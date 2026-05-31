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
        services.AddSingleton<IWorkOrderDispatchService, WorkOrderDispatchService>();
        services.AddSingleton<IAssetMaintenanceService, AssetMaintenanceService>();
        services.AddSingleton<IIotIntegrationService, IotIntegrationService>();
        return services;
    }
}
