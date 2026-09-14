using Defender.Portal.Application.Configuration.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Defender.Portal.Application.Configuration.Extension;

public static class ServiceOptionsExtensions
{
    public static IServiceCollection AddApplicationOptions(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<IdentityOptions>(configuration.GetSection(nameof(IdentityOptions)));
        services.Configure<UserManagementOptions>(configuration.GetSection(nameof(UserManagementOptions)));
        services.Configure<WalletOptions>(configuration.GetSection(nameof(WalletOptions)));
        services.Configure<RiskGamesOptions>(configuration.GetSection(nameof(RiskGamesOptions)));
        services.Configure<BudgetTrackerOptions>(configuration.GetSection(nameof(BudgetTrackerOptions)));
        services.Configure<PersonalFoodAdvisorOptions>(configuration.GetSection(nameof(PersonalFoodAdvisorOptions)));
        services.Configure<HealthCareOptions>(configuration.GetSection(nameof(HealthCareOptions)));
        services.Configure<TravelCalendarOptions>(configuration.GetSection(nameof(TravelCalendarOptions)));
        services
            .AddOptions<CarServiceOptions>()
            .Bind(configuration.GetSection(nameof(CarServiceOptions)))
            .Validate(options => Uri.TryCreate(options.Url, UriKind.Absolute, out _), "CarServiceOptions:Url must be an absolute URI.")
            .ValidateOnStart();
        services.PostConfigure<CarServiceOptions>(options => options.Url = options.Url.TrimEnd('/') + "/");

        return services;
    }
}
