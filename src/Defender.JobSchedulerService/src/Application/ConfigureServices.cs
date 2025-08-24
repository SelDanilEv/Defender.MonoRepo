using System.Reflection;
using Defender.JobSchedulerService.Application.Common.Interfaces.Services;
using Defender.JobSchedulerService.Application.Services;
using Defender.JobSchedulerService.Application.Services.Background;
using Defender.JobSchedulerService.Application.Services.Background.Kafka;
using Defender.Kafka.Configuration.Options;
using Defender.Kafka.Extension;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Defender.JobSchedulerService.Application;

public static class ConfigureServices
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAutoMapper(Assembly.GetExecutingAssembly());
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

        services.RegisterKafkaServices(configuration)
            .RegisterServices()
            .RegisterHostedServices();

        return services;
    }

    private static IServiceCollection RegisterKafkaServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddKafka(options =>
        {
            configuration.GetSection(nameof(KafkaOptions)).Bind(options);
        });

        return services;
    }

    private static IServiceCollection RegisterServices(this IServiceCollection services)
    {
        services.AddTransient<IJobManagementService, JobManagementService>();
        services.AddTransient<IJobRunningService, JobManagementService>();

        return services;
    }

    private static IServiceCollection RegisterHostedServices(this IServiceCollection services)
    {
        services.AddHostedService<CreateKafkaTopicsService>();
        services.AddHostedService<JobRunningBackgroundService>();

        return services;
    }
}
