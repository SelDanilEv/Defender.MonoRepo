using Confluent.Kafka;
using Defender.Kafka.Configuration.Options;
using Defender.Kafka.Service;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Defender.Kafka.Default;

public class DefaultKafkaConsumer<TValue> : IDefaultKafkaConsumer<TValue>
{
    private readonly KafkaOptions _kafkaOptions;
    private readonly IDeserializer<TValue> _valueSerializer;
    private readonly ILogger<DefaultKafkaConsumer<TValue>> _logger;
    private readonly IKafkaEnvPrefixer _kafkaEnvPrefixer;

    public DefaultKafkaConsumer(
        IOptions<KafkaOptions> kafkaOptions,
        ILogger<DefaultKafkaConsumer<TValue>> logger,
        IKafkaEnvPrefixer kafkaEnvPrefixer,
        IDeserializer<TValue> valueSerializer)
    {
        if (string.IsNullOrWhiteSpace(kafkaOptions?.Value?.BootstrapServers))
        {
            throw new ArgumentException("BootstrapServers must be provided in KafkaOptions.", nameof(kafkaOptions));
        }

        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _kafkaEnvPrefixer = kafkaEnvPrefixer ?? throw new ArgumentNullException(nameof(kafkaEnvPrefixer));
        _kafkaOptions = kafkaOptions.Value ?? throw new ArgumentNullException(nameof(kafkaOptions));
        _valueSerializer = valueSerializer ?? throw new ArgumentNullException(nameof(valueSerializer));
    }

    public async Task StartConsuming(
        string topic,
        string groupId,
        Func<TValue, Task> handleMessage,
        CancellationToken cancellationToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _kafkaOptions.BootstrapServers,
            GroupId = _kafkaEnvPrefixer.AddEnvPrefix(groupId),
            AutoOffsetReset = AutoOffsetReset.Latest,
            EnableAutoCommit = true
        };
        
        using var consumer = new ConsumerBuilder<Ignore, TValue>(config)
            .SetValueDeserializer(_valueSerializer)
            .SetErrorHandler((_, error) => OnError(error))
            .Build();
        
        topic = _kafkaEnvPrefixer.AddEnvPrefix(topic);

        if (string.IsNullOrWhiteSpace(topic))
        {
            throw new ArgumentException("Topic name cannot be null or empty.", nameof(topic));
        }

        if (handleMessage == null)
        {
            throw new ArgumentNullException(nameof(handleMessage), "Message handler cannot be null.");
        }

        consumer.Subscribe(topic);
        _logger.LogInformation("Subscribed to topic: {Topic}", topic);

        await Task.Run(async () =>
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        var consumeResult = consumer.Consume(cancellationToken);

                        _logger.LogInformation("New message to consume {ConsumeResult}.", consumeResult);

                        if (consumeResult is not null && consumeResult.Message.Value is not null)
                            await handleMessage(consumeResult.Message.Value);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error consuming message from topic {Topic}.", topic);
                    }
                }
            }
            finally
            {
                consumer.Close();
                _logger.LogInformation("Kafka consumer closed.");
            }
        }, cancellationToken);
    }

    private void OnError(Error error)
    {
        _logger.LogError("Kafka Consumer Error: {Reason}", error.Reason);
    }
}
