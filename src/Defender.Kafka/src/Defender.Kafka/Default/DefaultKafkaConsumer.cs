using Confluent.Kafka;
using Defender.Kafka.Configuration.Options;
using Defender.Kafka.Service;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Defender.Kafka.Default;

public class DefaultKafkaConsumer<TValue> : IDefaultKafkaConsumer<TValue>
{
    private static readonly TimeSpan DefaultFailureRetryDelay = TimeSpan.FromSeconds(5);
    private readonly KafkaOptions _kafkaOptions;
    private readonly IDeserializer<TValue> _valueSerializer;
    private readonly ILogger<DefaultKafkaConsumer<TValue>> _logger;
    private readonly IKafkaEnvPrefixer _kafkaEnvPrefixer;
    private readonly Func<ConsumerConfig, IDeserializer<TValue>, Action<Error>, IConsumer<Ignore, TValue>> _consumerFactory;
    private readonly TimeSpan _failureRetryDelay;

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
        _consumerFactory = (config, deserializer, onError) =>
            new ConsumerBuilder<Ignore, TValue>(config)
                .SetValueDeserializer(deserializer)
                .SetErrorHandler((_, error) => onError(error))
                .Build();
        _failureRetryDelay = DefaultFailureRetryDelay;
    }

    internal DefaultKafkaConsumer(
        IOptions<KafkaOptions> kafkaOptions,
        ILogger<DefaultKafkaConsumer<TValue>> logger,
        IKafkaEnvPrefixer kafkaEnvPrefixer,
        IDeserializer<TValue> valueSerializer,
        Func<ConsumerConfig, IDeserializer<TValue>, Action<Error>, IConsumer<Ignore, TValue>> consumerFactory,
        TimeSpan? failureRetryDelay = null)
    {
        if (string.IsNullOrWhiteSpace(kafkaOptions?.Value?.BootstrapServers))
        {
            throw new ArgumentException("BootstrapServers must be provided in KafkaOptions.", nameof(kafkaOptions));
        }

        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _kafkaEnvPrefixer = kafkaEnvPrefixer ?? throw new ArgumentNullException(nameof(kafkaEnvPrefixer));
        _kafkaOptions = kafkaOptions.Value ?? throw new ArgumentNullException(nameof(kafkaOptions));
        _valueSerializer = valueSerializer ?? throw new ArgumentNullException(nameof(valueSerializer));
        _consumerFactory = consumerFactory ?? throw new ArgumentNullException(nameof(consumerFactory));
        _failureRetryDelay = failureRetryDelay ?? DefaultFailureRetryDelay;
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
            EnableAutoCommit = false
        };
        
        using var consumer = _consumerFactory(config, _valueSerializer, OnError);
        
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
                    ConsumeResult<Ignore, TValue>? consumeResult = null;

                    try
                    {
                        consumeResult = consumer.Consume(cancellationToken);
                        _logger.LogInformation("New message to consume {ConsumeResult}.", consumeResult);

                        if (consumeResult is null || consumeResult.Message.Value is null)
                        {
                            continue;
                        }

                        await handleMessage(consumeResult.Message.Value);
                        consumer.Commit(consumeResult);
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(
                            ex,
                            "Error consuming message from topic {Topic} at offset {Offset}.",
                            topic,
                            consumeResult?.TopicPartitionOffset);

                        if (consumeResult is not null)
                        {
                            try
                            {
                                consumer.Seek(consumeResult.TopicPartitionOffset);
                            }
                            catch (Exception seekException)
                            {
                                _logger.LogError(
                                    seekException,
                                    "Failed to seek topic {Topic} back to offset {Offset}. Stopping consumer to avoid committing past the failed message.",
                                    topic,
                                    consumeResult.TopicPartitionOffset);
                                throw;
                            }
                        }

                        if (_failureRetryDelay > TimeSpan.Zero)
                        {
                            await Task.Delay(_failureRetryDelay, cancellationToken);
                        }
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
