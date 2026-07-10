using Defender.TravelCalendarService.Application.Common.Interfaces.Services;
using Defender.TravelCalendarService.Application.Defaults;
using Microsoft.Extensions.DependencyInjection;

namespace Defender.TravelCalendarService.Application;

public static class ConfigureServices
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<TravelCalendarDefaultsFactory>();
        services.AddScoped<ITravelCalendarService, Services.TravelCalendarService>();
        return services;
    }
}
