using Confluent.Kafka;
using Defender.Kafka.Configuration.Options;
using Defender.Kafka.Service;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Defender.Kafka.Default;

public class DefaultKafkaProducer<TValue> : IDefaultKafkaProducer<TValue>, IDisposable
{
    private readonly IProducer<Null, TValue> _producer;
    private readonly ILogger<DefaultKafkaProducer<TValue>> _logger;
    private readonly IKafkaEnvPrefixer _kafkaEnvPrefixer;

    public DefaultKafkaProducer(
        IOptions<KafkaOptions> kafkaOptions,
        ILogger<DefaultKafkaProducer<TValue>> logger,
        IKafkaEnvPrefixer kafkaEnvPrefixer,
        ISerializer<TValue> valueSerializer)
    {
        if (kafkaOptions?.Value?.BootstrapServers == null)
        {
            throw new ArgumentNullException(nameof(kafkaOptions), "Kafka bootstrap servers cannot be null.");
        }

        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _kafkaEnvPrefixer = kafkaEnvPrefixer ?? throw new ArgumentNullException(nameof(kafkaEnvPrefixer));

        var config = new ProducerConfig
        {
            BootstrapServers = kafkaOptions.Value.BootstrapServers,
            Acks = Acks.All,
            EnableIdempotence = true
        };

        _producer = new ProducerBuilder<Null, TValue>(config)
            .SetValueSerializer(valueSerializer)
            .SetErrorHandler((_, error) => OnError(error))
            .Build();
    }

    public async Task ProduceAsync(
        string topic,
        TValue value,
        CancellationToken cancellationToken)
    {
        topic = _kafkaEnvPrefixer.AddEnvPrefix(topic);

        try
        {
            var message = new Message<Null, TValue>
            {
                Value = value
            };

            var deliveryResult = await _producer.ProduceAsync(topic, message, cancellationToken);
            OnDeliveryReport(deliveryResult);
        }
        catch (ProduceException<Null, TValue> ex)
        {
            OnProduceError(ex);
        }
    }

    public void Flush() => _producer.Flush(TimeSpan.FromSeconds(10));

    private void OnError(Error error)
    {
        _logger.LogError("Kafka Producer Error: {Reason}", error.Reason);
    }

    private void OnDeliveryReport(DeliveryResult<Null, TValue> deliveryResult)
    {
        _logger.LogInformation("Message delivered to {TopicPartitionOffset}", deliveryResult.TopicPartitionOffset);
    }

    private void OnProduceError(ProduceException<Null, TValue> ex)
    {
        _logger.LogError("Produce error: {Reason}", ex.Error.Reason);
    }

    public void Dispose()
    {
        Flush();
        _producer.Dispose();
    }
}
