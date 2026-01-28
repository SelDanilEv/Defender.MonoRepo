using System.Reflection;
using Defender.SecretManagementService.Application.Common.Interfaces.Repositories;
using Defender.SecretManagementService.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Defender.SecretManagementService.Infrastructure;

public static class ConfigureServices
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddAutoMapper(_ => { }, Assembly.GetExecutingAssembly());

        RegisterRepositories(services);

        RegisterApiClients(services, configuration);

        RegisterClientWrappers(services);

        return services;
    }

    private static void RegisterClientWrappers(IServiceCollection services)
    {
        //services.AddTransient<IServiceWrapper, ServiceWrapper>();
    }

    private static void RegisterRepositories(IServiceCollection services)
    {
        services.AddSingleton<ISecretRepository, SecretRepository>();
    }

    private static void RegisterApiClients(
        IServiceCollection services,
        IConfiguration configuration)
    {

    }

}
