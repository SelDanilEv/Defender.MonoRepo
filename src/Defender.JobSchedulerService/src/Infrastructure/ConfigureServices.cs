using System.Reflection;
using Defender.JobSchedulerService.Application.Common.Interfaces.Repositories;
using Defender.JobSchedulerService.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Defender.JobSchedulerService.Infrastructure;

public static class ConfigureServices
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddAutoMapper(Assembly.GetExecutingAssembly());

        RegisterRepositories(services);

        return services;
    }

    private static IServiceCollection RegisterRepositories(IServiceCollection services)
    {
        services.AddSingleton<IScheduledJobRepository, ScheduledJobRepository>();

        return services;
    }


}
