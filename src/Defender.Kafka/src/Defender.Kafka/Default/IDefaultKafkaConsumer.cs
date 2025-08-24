namespace Defender.Kafka.Default;

public interface IDefaultKafkaConsumer<out TValue>
{
    public Task StartConsuming(
        string topic,
        string groupId,
        Func<TValue, Task> handleMessage,
        CancellationToken cancellationToken);
}
