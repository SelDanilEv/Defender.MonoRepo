using Confluent.Kafka;
using Defender.Kafka.Configuration.Options;
using Microsoft.Extensions.Options;
using System.Text.Json;
using Defender.Kafka.Service;

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
        var producerConfig = new ProducerConfig
        {
            BootstrapServers = _kafkaOptions.Value.BootstrapServers
        };

        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = _kafkaOptions.Value.BootstrapServers,
            GroupId = _kafkaEnvPrefixer.AddEnvPrefix(groupId),
            AutoOffsetReset = AutoOffsetReset.Latest,
            EnableAutoCommit = false
        };

        using var producer = _producerFactory(producerConfig);
        using var consumer = _consumerFactory(consumerConfig);

        var message = new Message<string, string>
        {
            Key = correlatedKafkaRequest.CorrelationId,
            Value = JsonSerializer.Serialize(correlatedKafkaRequest)
        };

        await producer.ProduceAsync(requestTopic, message, cancellationToken);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeout);

        while (!cts.Token.IsCancellationRequested)
        {
            var consumeResult = consumer.Consume(cts.Token);
            if (consumeResult == null) continue;

            try
            {
                var response =
                    JsonSerializer.Deserialize<CorrelatedKafkaResponse<TResponse>>(consumeResult.Message.Value);
                if (consumeResult.Message.Key == correlatedKafkaRequest.CorrelationId)
                {
                    return response!.GetResult;
                }
            }
            catch (JsonException)
            {
                continue;
            }
        }

        throw new TimeoutException("Request timed out.");
    }
}
