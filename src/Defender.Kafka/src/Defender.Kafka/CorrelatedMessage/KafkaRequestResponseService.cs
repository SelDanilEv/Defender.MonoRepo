using Confluent.Kafka;
using Defender.Kafka.Configuration.Options;
using Defender.Kafka.Service;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Text.Json;

namespace Defender.Kafka.CorrelatedMessage;

public class KafkaRequestResponseService : IKafkaRequestResponseService
{
    private readonly IKafkaEnvPrefixer _kafkaEnvPrefixer;
    private readonly IOptions<KafkaOptions> _kafkaOptions;
    private readonly Func<ProducerConfig, IProducer<string, string>> _producerFactory;
    private readonly Func<ConsumerConfig, IConsumer<string, string>> _consumerFactory;

    public KafkaRequestResponseService(
        IKafkaEnvPrefixer kafkaEnvPrefixer,
        IOptions<KafkaOptions> kafkaOptions)
        : this(
            kafkaEnvPrefixer,
            kafkaOptions,
            config => new ProducerBuilder<string, string>(config).Build(),
            config => new ConsumerBuilder<string, string>(config).Build())
    {
    }

    internal KafkaRequestResponseService(
        IKafkaEnvPrefixer kafkaEnvPrefixer,
        IOptions<KafkaOptions> kafkaOptions,
        Func<ProducerConfig, IProducer<string, string>> producerFactory,
        Func<ConsumerConfig, IConsumer<string, string>> consumerFactory)
    {
        _kafkaEnvPrefixer = kafkaEnvPrefixer ?? throw new ArgumentNullException(nameof(kafkaEnvPrefixer));
        _kafkaOptions = kafkaOptions ?? throw new ArgumentNullException(nameof(kafkaOptions));
        _producerFactory = producerFactory ?? throw new ArgumentNullException(nameof(producerFactory));
        _consumerFactory = consumerFactory ?? throw new ArgumentNullException(nameof(consumerFactory));
    }

    public async Task<TResponse> SendAsync<TRequest, TResponse>(
        string requestTopic,
        string responseTopic,
        string groupId,
        CorrelatedKafkaRequest<TRequest> correlatedKafkaRequest,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var maxConsumeWait = TimeSpan.FromMilliseconds(250);
        var responseConsumerGroupId = $"{_kafkaEnvPrefixer.AddEnvPrefix(groupId)}-{correlatedKafkaRequest.CorrelationId}";
        var producerConfig = new ProducerConfig
        {
            BootstrapServers = _kafkaOptions.Value.BootstrapServers
        };

        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = _kafkaOptions.Value.BootstrapServers,
            GroupId = responseConsumerGroupId,
            AutoOffsetReset = AutoOffsetReset.Latest,
            EnableAutoCommit = false
        };

        using var producer = _producerFactory(producerConfig);
        using var consumer = _consumerFactory(consumerConfig);

        requestTopic = _kafkaEnvPrefixer.AddEnvPrefix(requestTopic);
        responseTopic = _kafkaEnvPrefixer.AddEnvPrefix(responseTopic);
        consumer.Subscribe(responseTopic);

        while (consumer.Assignment?.Count is not > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var remaining = timeout - stopwatch.Elapsed;
            if (remaining <= TimeSpan.Zero)
            {
                throw new TimeoutException("Request timed out while waiting for the response consumer assignment.");
            }

            var consumeTimeout = remaining < maxConsumeWait ? remaining : maxConsumeWait;
            consumer.Consume(consumeTimeout);
        }

        var message = new Message<string, string>
        {
            Key = correlatedKafkaRequest.CorrelationId,
            Value = JsonSerializer.Serialize(correlatedKafkaRequest)
        };

        await producer.ProduceAsync(requestTopic, message, cancellationToken);

        while (stopwatch.Elapsed < timeout)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var remaining = timeout - stopwatch.Elapsed;
            var consumeTimeout = remaining < maxConsumeWait ? remaining : maxConsumeWait;
            var consumeResult = consumer.Consume(consumeTimeout);
            if (consumeResult?.Message == null)
            {
                continue;
            }

            if (!string.Equals(consumeResult.Message.Key, correlatedKafkaRequest.CorrelationId, StringComparison.Ordinal))
            {
                continue;
            }

            try
            {
                var response =
                    JsonSerializer.Deserialize<CorrelatedKafkaResponse<TResponse>>(consumeResult.Message.Value);
                if (response == null)
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(response.CorrelationId)
                    && !string.Equals(response.CorrelationId, correlatedKafkaRequest.CorrelationId, StringComparison.Ordinal))
                {
                    continue;
                }

                return response.GetResult;
            }
            catch (JsonException)
            {
                continue;
            }
        }

        throw new TimeoutException("Request timed out.");
    }
}
