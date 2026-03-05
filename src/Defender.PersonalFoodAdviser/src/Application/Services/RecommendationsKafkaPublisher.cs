using Confluent.Kafka;
using Defender.Kafka.Configuration.Options;
using Defender.Kafka.Service;
using Defender.PersonalFoodAdviser.Application.Common.Interfaces.Services;
using Defender.PersonalFoodAdviser.Application.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Defender.PersonalFoodAdviser.Application.Services;

public sealed class RecommendationsKafkaPublisher : IRecommendationsKafkaPublisher, IDisposable
{
    private readonly IProducer<Null, RecommendationsRequestedEvent> _producer;
    private readonly IKafkaEnvPrefixer _kafkaEnvPrefixer;
    private readonly ILogger<RecommendationsKafkaPublisher> _logger;

    public RecommendationsKafkaPublisher(
        IOptions<KafkaOptions> kafkaOptions,
        IKafkaEnvPrefixer kafkaEnvPrefixer,
        ISerializer<RecommendationsRequestedEvent> serializer,
        ILogger<RecommendationsKafkaPublisher> logger)
    {
        _kafkaEnvPrefixer = kafkaEnvPrefixer;
        _logger = logger;

        var config = new ProducerConfig
        {
            BootstrapServers = kafkaOptions.Value.BootstrapServers,
            Acks = Acks.All,
            EnableIdempotence = true
        };

        _producer = new ProducerBuilder<Null, RecommendationsRequestedEvent>(config)
            .SetValueSerializer(serializer)
            .Build();
    }

    public async Task PublishAsync(RecommendationsRequestedEvent evt, CancellationToken cancellationToken = default)
    {
        var topic = _kafkaEnvPrefixer.AddEnvPrefix(KafkaTopicNames.RecommendationsRequested);
        var result = await _producer.ProduceAsync(
            topic,
            new Message<Null, RecommendationsRequestedEvent> { Value = evt },
            cancellationToken);

        _logger.LogInformation(
            "Recommendations outbox published to {TopicPartitionOffset} for session {SessionId}, attempt {Attempt}",
            result.TopicPartitionOffset,
            evt.SessionId,
            evt.Attempt);
    }

    public void Dispose()
    {
        _producer.Flush(TimeSpan.FromSeconds(10));
        _producer.Dispose();
    }
}
