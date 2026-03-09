using System.Reflection;
using Defender.Kafka.Configuration.Options;
using Defender.Kafka.Extension;
using Defender.PersonalFoodAdvisor.Application.Common.Interfaces.Services;
using Defender.PersonalFoodAdvisor.Application.Services;
using Defender.PersonalFoodAdvisor.Application.Services.Background.Kafka;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Defender.PersonalFoodAdvisor.Application;

public static class ConfigureServices
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAutoMapper(cfg => cfg.AddMaps(Assembly.GetExecutingAssembly()));

        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

        services.RegisterKafka(configuration);
        services.RegisterServices();
        services.RegisterBackgroundServices();

        return services;
    }

    private static IServiceCollection RegisterKafka(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddKafka(options => configuration.GetSection(nameof(KafkaOptions)).Bind(options));
        return services;
    }

    private static IServiceCollection RegisterBackgroundServices(this IServiceCollection services)
    {
        services.AddHostedService<CreateKafkaTopicsService>();
        services.AddHostedService<FoodAdvisorEventListenerService>();
        services.AddHostedService<MenuParsingOutboxPublisherService>();
        services.AddHostedService<RecommendationsOutboxPublisherService>();
        return services;
    }

    private static IServiceCollection RegisterServices(this IServiceCollection services)
    {
        services.AddTransient<IService, Service>();
        services.AddTransient<IPreferencesService, PreferencesService>();
        services.AddTransient<IMenuSessionService, MenuSessionService>();
        services.AddTransient<IRatingService, RatingService>();
        services.AddTransient<IImageUploadService, ImageUploadService>();
        services.AddTransient<IMenuParsingProcessor, MenuParsingProcessor>();
        services.AddTransient<IRecommendationProcessor, RecommendationProcessor>();
        services.AddSingleton<IMenuParsingOutboxService, MenuParsingOutboxService>();
        services.AddSingleton<IMenuParsingKafkaPublisher, MenuParsingKafkaPublisher>();
        services.AddSingleton<IRecommendationsOutboxService, RecommendationsOutboxService>();
        services.AddSingleton<IRecommendationsKafkaPublisher, RecommendationsKafkaPublisher>();

        return services;
    }
}
