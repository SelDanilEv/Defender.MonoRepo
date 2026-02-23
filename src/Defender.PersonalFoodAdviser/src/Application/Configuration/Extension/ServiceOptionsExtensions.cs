using Defender.PersonalFoodAdviser.Application.Configuration.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Defender.PersonalFoodAdviser.Application.Configuration.Extension;

public static class ServiceOptionsExtensions
{
    public static IServiceCollection AddApplicationOptions(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ServiceOptions>(configuration.GetSection(nameof(ServiceOptions)));
        services.Configure<HuggingFaceOptions>(configuration.GetSection(HuggingFaceOptions.SectionName));

        return services;
    }
}