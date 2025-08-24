using Confluent.Kafka;
using Defender.Kafka.Configuration.Options;
using Defender.Kafka.CorrelatedMessage;
using Defender.Kafka.Default;
using Defender.Kafka.Serialization;
using Defender.Kafka.Service;
using Microsoft.Extensions.DependencyInjection;

namespace Defender.Kafka.Extension;

public static class KafkaExtensions
{
    public static IServiceCollection AddKafka(
        this IServiceCollection services,
        Action<KafkaOptions> configureOptions)
    {
        services.Configure(configureOptions);
        services.AddSingleton(typeof(ISerializer<>), typeof(JsonSerializer<>));
        services.AddSingleton(typeof(IDeserializer<>), typeof(JsonSerializer<>));

        services.AddSingleton<IKafkaEnvPrefixer, KafkaEnvPrefixer>();

        services.AddTransient<IKafkaRequestResponseService, KafkaRequestResponseService>();
        services.AddTransient(typeof(IDefaultKafkaProducer<>), typeof(DefaultKafkaProducer<>));
        services.AddTransient(typeof(IDefaultKafkaConsumer<>), typeof(DefaultKafkaConsumer<>));

        return services;
    }
}
