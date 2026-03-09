using System.Reflection;
using Defender.Common.Clients.Identity;
using Defender.PersonalFoodAdvisor.Application.Common.Interfaces.Repositories;
using Defender.PersonalFoodAdvisor.Application.Common.Interfaces.Services;
using Defender.PersonalFoodAdvisor.Application.Common.Interfaces.Wrapper;
using Defender.PersonalFoodAdvisor.Application.Helpers.LocalSecretHelper;
using Defender.PersonalFoodAdvisor.Application.Configuration.Options;
using Defender.PersonalFoodAdvisor.Infrastructure.Clients.Service;
using Defender.PersonalFoodAdvisor.Infrastructure.Configuration.Options;
using Defender.PersonalFoodAdvisor.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Defender.PersonalFoodAdvisor.Infrastructure;

public static class ConfigureServices
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddAutoMapper(cfg => cfg.AddMaps(Assembly.GetExecutingAssembly()));

        services
            .RegisterRepositories()
            .RegisterApiClients(configuration)
            .RegisterClientWrappers();

        return services;
    }

    private static IServiceCollection RegisterClientWrappers(this IServiceCollection services)
    {
        services.AddSingleton<TimeProvider>(TimeProvider.System);
        services.AddTransient<IServiceWrapper, ServiceWrapper>();
        services.AddSingleton<Clients.Gemini.IGeminiModelFallbackService, Clients.Gemini.GeminiModelFallbackService>();
        services.AddHostedService<Clients.Gemini.GeminiModelLoopMaintenanceService>();

        return services;
    }

    private static IServiceCollection RegisterRepositories(this IServiceCollection services)
    {
        services.AddSingleton<IDomainModelRepository, DomainModelRepository>();
        services.AddSingleton<IUserPreferencesRepository, UserPreferencesRepository>();
        services.AddSingleton<IMenuSessionRepository, MenuSessionRepository>();
        services.AddSingleton<IDishRatingRepository, DishRatingRepository>();
        services.AddSingleton<IImageBlobRepository, ImageBlobRepository>();
        services.AddSingleton<IMenuParsingOutboxRepository, MenuParsingOutboxRepository>();
        services.AddSingleton<IRecommendationsOutboxRepository, RecommendationsOutboxRepository>();
        services.AddSingleton<IGeminiModelLoopStateRepository, GeminiModelLoopStateRepository>();

        return services;
    }

    private static IServiceCollection RegisterApiClients(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<MenuIntelligenceOptions>(configuration.GetSection(MenuIntelligenceOptions.SectionName));
        services.Configure<HuggingFaceOptions>(configuration.GetSection(HuggingFaceOptions.SectionName));
        services.Configure<GeminiOptions>(configuration.GetSection(GeminiOptions.SectionName));

        services.RegisterIdentityClient(
            (serviceProvider, client) =>
            {
                client.BaseAddress = new Uri(serviceProvider.GetRequiredService<IOptions<ServiceOptions>>().Value.Url);
            });

        services.AddHttpClient<Clients.HuggingFace.HuggingFaceClient>((sp, client) =>
        {
            var opts = sp.GetRequiredService<IOptions<HuggingFaceOptions>>().Value;
            client.BaseAddress = new Uri(opts.BaseUrl.TrimEnd('/') + "/");
        });

        services.AddHttpClient<Clients.Gemini.GeminiClient>((sp, client) =>
        {
            var opts = sp.GetRequiredService<IOptions<GeminiOptions>>().Value;
            client.BaseAddress = new Uri(opts.BaseUrl.TrimEnd('/') + "/");
        });

        services.AddTransient<IMenuIntelligenceClient>(sp =>
        {
            var provider = sp.GetRequiredService<IOptions<MenuIntelligenceOptions>>().Value.Provider;

            return provider switch
            {
                MenuIntelligenceProvider.Gemini => sp.GetRequiredService<Clients.Gemini.GeminiClient>(),
                _ => sp.GetRequiredService<Clients.HuggingFace.HuggingFaceClient>()
            };
        });

        return services;
    }
}
