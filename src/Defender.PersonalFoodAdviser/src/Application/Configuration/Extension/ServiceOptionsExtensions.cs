using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Defender.PersonalFoodAdviser.Application.Configuration.Options;

namespace Defender.PersonalFoodAdviser.Application.Configuration.Extension;

public static class ServiceOptionsExtensions
{
    public static IServiceCollection AddApplicationOptions(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ServiceOptions>(configuration.GetSection(nameof(ServiceOptions)));

        return services;
    }
}
