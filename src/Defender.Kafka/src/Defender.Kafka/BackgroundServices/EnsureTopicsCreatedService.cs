using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Defender.Kafka.Configuration.Options;
using Defender.Kafka.Service;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Defender.Kafka.BackgroundServices;

public abstract class EnsureTopicsCreatedService : BackgroundService
{
    private readonly IAdminClient _adminClient;
    private readonly ILogger<EnsureTopicsCreatedService> _logger;
    protected readonly IKafkaEnvPrefixer KafkaEnvPrefixer;
    protected abstract IEnumerable<string> Topics { get; }
    protected abstract short ReplicationFactor { get; }
    protected abstract int NumPartitions { get; }

    protected EnsureTopicsCreatedService(
        IOptions<KafkaOptions> kafkaOptions,
        IKafkaEnvPrefixer kafkaEnvPrefixer,
        ILogger<EnsureTopicsCreatedService> logger)
        : this(
            new AdminClientBuilder(new AdminClientConfig
            {
                BootstrapServers = kafkaOptions.Value.BootstrapServers
            }).Build(),
            kafkaEnvPrefixer,
            logger)
    {
    }

    protected EnsureTopicsCreatedService(
        IAdminClient adminClient,
        IKafkaEnvPrefixer kafkaEnvPrefixer,
        ILogger<EnsureTopicsCreatedService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _adminClient = adminClient ?? throw new ArgumentNullException(nameof(adminClient));
        
        KafkaEnvPrefixer = kafkaEnvPrefixer ?? throw new ArgumentNullException(nameof(kafkaEnvPrefixer));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("Starting Kafka background service.");

            await EnsureTopicsExistAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred in the Kafka background service.");
            throw;
        }
    }

    private async Task EnsureTopicsExistAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Ensuring topics exist: {Topics}", string.Join(", ", Topics));

            var metadata = _adminClient.GetMetadata(TimeSpan.FromSeconds(60));
            var existingTopics = metadata.Topics.Select(t => t.Topic).ToHashSet();

            var topicsToCreate = Topics
                .Select(topic => KafkaEnvPrefixer.AddEnvPrefix(topic))
                .Where(topic => !existingTopics.Contains(topic))
                .Select(topic => new TopicSpecification
                {
                    Name = topic,
                    NumPartitions = NumPartitions,
                    ReplicationFactor = ReplicationFactor
                })
                .ToList();

            if (topicsToCreate.Count != 0)
            {
                _logger.LogInformation("Creating missing topics: {Topics}", string.Join(", ", topicsToCreate.Select(t => t.Name)));

                await _adminClient.CreateTopicsAsync(topicsToCreate);
                _logger.LogInformation("Topics created successfully.");
            }
            else
            {
                _logger.LogInformation("All topics already exist.");
            }
        }
        catch (CreateTopicsException ex)
        {
            _logger.LogError(ex, "An error occurred while creating Kafka topics.");
            throw;
        }
    }

}

