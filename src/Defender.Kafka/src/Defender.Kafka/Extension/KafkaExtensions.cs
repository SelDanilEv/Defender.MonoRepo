using Confluent.Kafka;
using Defender.Kafka.Configuration.Options;
using Defender.Kafka.CorrelatedMessage;
using Defender.Kafka.Default;
using Defender.Kafka.Serialization;
using Defender.Kafka.Service;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Defender.Kafka.Extension;

public static class KafkaExtensions
{
    public static IServiceCollection AddKafka(
        this IServiceCollection services,
        Action<KafkaOptions> configureOptions)
    {
        services.Configure(configureOptions);
        services.AddHealthChecks()
            .AddCheck<KafkaHealthCheck>("kafka", tags: ["ready"]);
        services.AddSingleton(typeof(ISerializer<>), typeof(JsonSerializer<>));
        services.AddSingleton(typeof(IDeserializer<>), typeof(JsonSerializer<>));

        services.AddSingleton<IKafkaEnvPrefixer, KafkaEnvPrefixer>();

        services.AddTransient<IKafkaRequestResponseService, KafkaRequestResponseService>();
        services.AddTransient(typeof(IDefaultKafkaProducer<>), typeof(DefaultKafkaProducer<>));
        services.AddTransient(typeof(IDefaultKafkaConsumer<>), typeof(DefaultKafkaConsumer<>));

        return services;
    }

    private sealed class KafkaHealthCheck(IOptions<KafkaOptions> options) : IHealthCheck
    {
        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(options.Value.BootstrapServers))
            {
                return Task.FromResult(HealthCheckResult.Unhealthy("Kafka bootstrap servers are not configured."));
            }

            try
            {
                using var client = new AdminClientBuilder(new AdminClientConfig
                {
                    BootstrapServers = options.Value.BootstrapServers
                }).Build();
                var metadata = client.GetMetadata(TimeSpan.FromSeconds(5));

                return metadata.Brokers.Count > 0
                    ? Task.FromResult(HealthCheckResult.Healthy())
                    : Task.FromResult(HealthCheckResult.Unhealthy("Kafka has no available brokers."));
            }
            catch (Exception exception)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy("Kafka is unavailable.", exception));
            }
        }
    }
}
