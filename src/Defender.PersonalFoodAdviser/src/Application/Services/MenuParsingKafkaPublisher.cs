using Confluent.Kafka;
using Defender.Kafka.Configuration.Options;
using Defender.Kafka.Service;
using Defender.PersonalFoodAdviser.Application.Common.Interfaces.Services;
using Defender.PersonalFoodAdviser.Application.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Defender.PersonalFoodAdviser.Application.Services;

public sealed class MenuParsingKafkaPublisher : IMenuParsingKafkaPublisher, IDisposable
{
    private readonly IProducer<Null, MenuParsingRequestedEvent> _producer;
    private readonly IKafkaEnvPrefixer _kafkaEnvPrefixer;
    private readonly ILogger<MenuParsingKafkaPublisher> _logger;

    public MenuParsingKafkaPublisher(
        IOptions<KafkaOptions> kafkaOptions,
        IKafkaEnvPrefixer kafkaEnvPrefixer,
        ISerializer<MenuParsingRequestedEvent> serializer,
        ILogger<MenuParsingKafkaPublisher> logger)
    {
        _kafkaEnvPrefixer = kafkaEnvPrefixer;
        _logger = logger;

        var config = new ProducerConfig
        {
            BootstrapServers = kafkaOptions.Value.BootstrapServers,
            Acks = Acks.All,
            EnableIdempotence = true
        };

        _producer = new ProducerBuilder<Null, MenuParsingRequestedEvent>(config)
            .SetValueSerializer(serializer)
            .Build();
    }

    public async Task PublishAsync(MenuParsingRequestedEvent evt, CancellationToken cancellationToken = default)
    {
        var topic = _kafkaEnvPrefixer.AddEnvPrefix(KafkaTopicNames.MenuParsingRequested);
        var result = await _producer.ProduceAsync(
            topic,
            new Message<Null, MenuParsingRequestedEvent> { Value = evt },
            cancellationToken);

        _logger.LogInformation(
            "Menu parsing outbox published to {TopicPartitionOffset} for session {SessionId}, imageRefsCount {ImageRefsCount}",
            result.TopicPartitionOffset,
            evt.SessionId,
            evt.ImageRefs.Count);
    }

    public void Dispose()
    {
        _producer.Flush(TimeSpan.FromSeconds(10));
        _producer.Dispose();
    }
}
