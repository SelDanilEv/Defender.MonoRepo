using Defender.Kafka;
using Defender.Kafka.BackgroundServices;
using Defender.Kafka.Configuration.Options;
using Defender.Kafka.Service;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Defender.JobSchedulerService.Application.Services.Background.Kafka;

public class CreateKafkaTopicsService(
    IOptions<KafkaOptions> kafkaOptions,
    IKafkaEnvPrefixer kafkaEnvPrefixer,
    ILogger<CreateKafkaTopicsService> logger)
    : EnsureTopicsCreatedService(kafkaOptions, kafkaEnvPrefixer, logger)
{
    protected override IEnumerable<string> Topics =>
        [
            Topic.TransactionStatusUpdates.GetName(),
            Topic.DistributedCache.GetName()
        ];

    protected override short ReplicationFactor => 1;

    protected override int NumPartitions => 3;
}
