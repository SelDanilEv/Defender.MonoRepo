using System.Reflection;
using Defender.Common.Clients.Identity;
using Defender.PersonalFoodAdviser.Application.Common.Interfaces.Repositories;
using Defender.PersonalFoodAdviser.Application.Common.Interfaces.Wrapper;
using Defender.PersonalFoodAdviser.Application.Configuration.Options;
using Defender.PersonalFoodAdviser.Application.Helpers.LocalSecretHelper;
using Defender.PersonalFoodAdviser.Infrastructure.Clients.Service;
using Defender.PersonalFoodAdviser.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Defender.PersonalFoodAdviser.Infrastructure;

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
        services.AddTransient<IServiceWrapper, ServiceWrapper>();

        return services;
    }

    private static IServiceCollection RegisterRepositories(this IServiceCollection services)
    {
        services.AddSingleton<IDomainModelRepository, DomainModelRepository>();
        services.AddSingleton<IUserPreferencesRepository, UserPreferencesRepository>();
        services.AddSingleton<IMenuSessionRepository, MenuSessionRepository>();
        services.AddSingleton<IDishRatingRepository, DishRatingRepository>();
        services.AddSingleton<IImageBlobRepository, ImageBlobRepository>();

        return services;
    }

    private static IServiceCollection RegisterApiClients(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.PostConfigure<HuggingFaceOptions>(opts =>
        {
            opts.ApiKey = LocalSecretsHelper.GetSecretSync(LocalSecret.HuggingFaceApiKey);
        });

        services.RegisterIdentityClient(
            (serviceProvider, client) =>
            {
                client.BaseAddress = new Uri(serviceProvider.GetRequiredService<IOptions<ServiceOptions>>().Value.Url);
            });

        services.AddHttpClient<Application.Common.Interfaces.Services.IHuggingFaceClient, Clients.HuggingFace.HuggingFaceClient>((sp, client) =>
        {
            var opts = sp.GetRequiredService<IOptions<Application.Configuration.Options.HuggingFaceOptions>>().Value;
            client.BaseAddress = new Uri(opts.BaseUrl.TrimEnd('/') + "/");
        });

        return services;
    }
}
