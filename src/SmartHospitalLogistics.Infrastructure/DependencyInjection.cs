using Microsoft.Extensions.DependencyInjection;
using SmartHospitalLogistics.Application;

namespace SmartHospitalLogistics.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IOperationsDashboardService, OperationsDashboardService>();
        return services;
    }
}
