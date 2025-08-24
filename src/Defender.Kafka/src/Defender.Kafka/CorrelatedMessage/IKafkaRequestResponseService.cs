namespace Defender.Kafka.CorrelatedMessage;

public interface IKafkaRequestResponseService
{
    Task<TResponse> SendAsync<TRequest, TResponse>(
        string requestTopic,
        string responseTopic,
        string groupId,
        CorrelatedKafkaRequest<TRequest> correlatedKafkaRequest,
        TimeSpan timeout,
        CancellationToken cancellationToken = default);
}

