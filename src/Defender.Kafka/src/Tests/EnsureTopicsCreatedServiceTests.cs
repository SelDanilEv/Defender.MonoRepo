using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Defender.Kafka.BackgroundServices;
using Defender.Kafka.Service;
using Microsoft.Extensions.Logging;

namespace Defender.Kafka.Tests;

public class EnsureTopicsCreatedServiceTests
{
    [Fact]
    public async Task ExecuteAsync_WhenTopicMissing_CreatesMissingTopicWithPrefix()
    {
        var adminClient = new Mock<IAdminClient>();
        adminClient
            .Setup(x => x.GetMetadata(It.IsAny<TimeSpan>()))
            .Returns(CreateMetadata("local_existing-topic"));
        adminClient
            .Setup(x => x.CreateTopicsAsync(
                It.Is<IEnumerable<TopicSpecification>>(topics =>
                    topics.Any(t => t.Name == "local_new-topic" && t.NumPartitions == 3 && t.ReplicationFactor == 2)),
                It.IsAny<CreateTopicsOptions>()))
            .Returns(Task.CompletedTask);
        var prefixer = new Mock<IKafkaEnvPrefixer>();
        prefixer.Setup(x => x.AddEnvPrefix("existing-topic")).Returns("local_existing-topic");
        prefixer.Setup(x => x.AddEnvPrefix("new-topic")).Returns("local_new-topic");
        var logger = Mock.Of<ILogger<EnsureTopicsCreatedService>>();
        var service = new TestEnsureTopicsCreatedService(
            adminClient.Object,
            prefixer.Object,
            logger,
            ["existing-topic", "new-topic"],
            replicationFactor: 2,
            numPartitions: 3);

        await service.RunAsync(CancellationToken.None);

        adminClient.VerifyAll();
    }

    [Fact]
    public async Task ExecuteAsync_WhenAllTopicsExist_DoesNotCreateTopics()
    {
        var adminClient = new Mock<IAdminClient>();
        adminClient
            .Setup(x => x.GetMetadata(It.IsAny<TimeSpan>()))
            .Returns(CreateMetadata("local_topic-a", "local_topic-b"));
        var prefixer = new Mock<IKafkaEnvPrefixer>();
        prefixer.Setup(x => x.AddEnvPrefix("topic-a")).Returns("local_topic-a");
        prefixer.Setup(x => x.AddEnvPrefix("topic-b")).Returns("local_topic-b");
        var logger = Mock.Of<ILogger<EnsureTopicsCreatedService>>();
        var service = new TestEnsureTopicsCreatedService(
            adminClient.Object,
            prefixer.Object,
            logger,
            ["topic-a", "topic-b"]);

        await service.RunAsync(CancellationToken.None);

        adminClient.Verify(x => x.CreateTopicsAsync(It.IsAny<IEnumerable<TopicSpecification>>(), It.IsAny<CreateTopicsOptions>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WhenCreateTopicsFails_ThrowsCreateTopicsException()
    {
        var adminClient = new Mock<IAdminClient>();
        adminClient
            .Setup(x => x.GetMetadata(It.IsAny<TimeSpan>()))
            .Returns(CreateMetadata());
        adminClient
            .Setup(x => x.CreateTopicsAsync(It.IsAny<IEnumerable<TopicSpecification>>(), It.IsAny<CreateTopicsOptions>()))
            .ThrowsAsync(new CreateTopicsException([]));
        var prefixer = new Mock<IKafkaEnvPrefixer>();
        prefixer.Setup(x => x.AddEnvPrefix(It.IsAny<string>())).Returns((string topic) => $"local_{topic}");
        var logger = Mock.Of<ILogger<EnsureTopicsCreatedService>>();
        var service = new TestEnsureTopicsCreatedService(
            adminClient.Object,
            prefixer.Object,
            logger,
            ["missing-topic"]);

        await Assert.ThrowsAsync<CreateTopicsException>(() => service.RunAsync(CancellationToken.None));
    }

    private static Metadata CreateMetadata(params string[] topics)
    {
        var topicMetadata = topics
            .Select(topic => new TopicMetadata(
                topic,
                [new PartitionMetadata(0, 0, [0], [0], new Error(ErrorCode.NoError))],
                new Error(ErrorCode.NoError)))
            .ToList();

        return new Metadata(
            [new BrokerMetadata(0, "localhost", 9092)],
            topicMetadata,
            0,
            "localhost:9092");
    }

    private sealed class TestEnsureTopicsCreatedService(
        IAdminClient adminClient,
        IKafkaEnvPrefixer kafkaEnvPrefixer,
        ILogger<EnsureTopicsCreatedService> logger,
        IEnumerable<string> topics,
        short replicationFactor = 1,
        int numPartitions = 1)
        : EnsureTopicsCreatedService(adminClient, kafkaEnvPrefixer, logger)
    {
        protected override IEnumerable<string> Topics => topics;
        protected override short ReplicationFactor => replicationFactor;
        protected override int NumPartitions => numPartitions;

        public Task RunAsync(CancellationToken cancellationToken)
            => ExecuteAsync(cancellationToken);
    }
}
