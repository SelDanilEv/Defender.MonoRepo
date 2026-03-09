using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Defender.PersonalFoodAdvisor.Application.Configuration.Options;

namespace Defender.PersonalFoodAdvisor.Application.Configuration.Extension;

public static class ServiceOptionsExtensions
{
    public static IServiceCollection AddApplicationOptions(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ServiceOptions>(configuration.GetSection(nameof(ServiceOptions)));

        return services;
    }
}
