using Defender.Kafka.BackgroundServices;
using Defender.Kafka.Configuration.Options;
using Defender.Kafka.Service;
using Defender.PersonalFoodAdvisor.Application.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Defender.PersonalFoodAdvisor.Application.Services.Background.Kafka;

public class CreateKafkaTopicsService(
    IOptions<KafkaOptions> kafkaOptions,
    IKafkaEnvPrefixer kafkaEnvPrefixer,
    ILogger<CreateKafkaTopicsService> logger)
    : EnsureTopicsCreatedService(kafkaOptions, kafkaEnvPrefixer, logger)
{
    protected override IEnumerable<string> Topics =>
    [
        KafkaTopicNames.MenuParsingRequested,
        KafkaTopicNames.MenuParsed,
        KafkaTopicNames.RecommendationsRequested,
        KafkaTopicNames.RecommendationsGenerated,
    ];

    protected override short ReplicationFactor => 1;
    protected override int NumPartitions => 1;
}
