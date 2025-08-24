using Confluent.Kafka;
using Defender.Kafka.Configuration.Options;
using Microsoft.Extensions.Options;
using System.Text.Json;
using Defender.Kafka.Service;

namespace Defender.Kafka.CorrelatedMessage;

public class KafkaRequestResponseService(
    IKafkaEnvPrefixer kafkaEnvPrefixer,
    IOptions<KafkaOptions> kafkaOptions) : IKafkaRequestResponseService
{
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
            BootstrapServers = kafkaOptions.Value.BootstrapServers
        };

        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = kafkaOptions.Value.BootstrapServers,
            GroupId = kafkaEnvPrefixer.AddEnvPrefix(groupId),
            AutoOffsetReset = AutoOffsetReset.Latest,
            EnableAutoCommit = false
        };

        using var producer = new ProducerBuilder<string, string>
            (producerConfig).Build();
        using var consumer = new ConsumerBuilder<string, string>
            (consumerConfig).Build();

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