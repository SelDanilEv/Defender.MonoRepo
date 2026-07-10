using Defender.TravelCalendarService.Application.Common.Interfaces.Repositories;
using Defender.TravelCalendarService.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace Defender.TravelCalendarService.Infrastructure;

public static class ConfigureServices
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        services.AddSingleton<ITravelCalendarRepository, TravelCalendarRepository>();
        services.AddSingleton<ITravelEventRepository, TravelEventRepository>();
        return services;
    }
}
