using System.Reflection;
using Defender.DistributedCache;
using Defender.Kafka;
using Defender.Kafka.Configuration.Options;
using Defender.Kafka.Default;
using Defender.Kafka.Service;
using Defender.WalletService.Application.Common.Interfaces.Services;
using Defender.WalletService.Application.Services.Background.Kafka;
using Defender.WalletService.Common.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Defender.WalletService.Tests.Services.Background;

public class KafkaServicesTests
{
    [Fact]
    public void CreateKafkaTopicsService_WhenCreated_UsesExpectedTopicsAndSettings()
    {
        var options = Options.Create(new KafkaOptions { BootstrapServers = "localhost:9092" });
        var prefixer = new Mock<IKafkaEnvPrefixer>();
        var logger = new Mock<ILogger<CreateKafkaTopicsService>>();
        var sut = new CreateKafkaTopicsService(options, prefixer.Object, logger.Object);

        var topics = (IEnumerable<string>)typeof(CreateKafkaTopicsService)
            .GetProperty("Topics", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(sut)!;
        var replicationFactor = (short)typeof(CreateKafkaTopicsService)
            .GetProperty("ReplicationFactor", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(sut)!;
        var partitions = (int)typeof(CreateKafkaTopicsService)
            .GetProperty("NumPartitions", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(sut)!;

        Assert.Contains(KafkaTopic.ScheduledTasks.GetName(), topics);
        Assert.Contains(KafkaTopic.TransactionsToProcess.GetName(), topics);
        Assert.Contains(Topic.TransactionStatusUpdates.GetName(), topics);
        Assert.Equal((short)1, replicationFactor);
        Assert.Equal(3, partitions);
    }

    [Fact]
    public async Task HandleStringEvent_WhenCleanupEventReceived_CallsCacheCleanup()
    {
        var cacheCleanup = new Mock<IPostgresCacheCleanupService>();
        cacheCleanup.Setup(x => x.CheckAndRunCleanupAsync()).Returns(Task.CompletedTask);
        var sut = CreateEventListenerService(cacheCleanup);

        await InvokeHandleStringEventAsync(sut, KafkaEvent.StartCacheCleanup.GetName());

        cacheCleanup.Verify(x => x.CheckAndRunCleanupAsync(), Times.Once);
    }

    [Fact]
    public async Task HandleStringEvent_WhenUnknownEventReceived_DoesNotCallCacheCleanup()
    {
        var cacheCleanup = new Mock<IPostgresCacheCleanupService>();
        var sut = CreateEventListenerService(cacheCleanup);

        await InvokeHandleStringEventAsync(sut, "unknown-event");

        cacheCleanup.Verify(x => x.CheckAndRunCleanupAsync(), Times.Never);
    }

    [Fact]
    public async Task HandleStringEvent_WhenCleanupThrowsException_DoesNotRethrow()
    {
        var cacheCleanup = new Mock<IPostgresCacheCleanupService>();
        cacheCleanup.Setup(x => x.CheckAndRunCleanupAsync()).ThrowsAsync(new InvalidOperationException("failed"));
        var sut = CreateEventListenerService(cacheCleanup);

        var exception = await Record.ExceptionAsync(() => InvokeHandleStringEventAsync(sut, KafkaEvent.StartCacheCleanup.GetName()));

        Assert.Null(exception);
    }

    private static EventListenerService CreateEventListenerService(Mock<IPostgresCacheCleanupService> cacheCleanup)
    {
        return new EventListenerService(
            Mock.Of<IDefaultKafkaConsumer<string>>(),
            Mock.Of<IDefaultKafkaConsumer<string>>(),
            cacheCleanup.Object,
            Mock.Of<ITransactionProcessingService>(),
            Mock.Of<ILogger<EventListenerService>>());
    }

    private static async Task InvokeHandleStringEventAsync(EventListenerService service, string @event)
    {
        var method = typeof(EventListenerService)
            .GetMethod("HandleStringEvent", BindingFlags.Instance | BindingFlags.NonPublic)!;

        var task = (Task)method.Invoke(service, [@event])!;
        await task;
    }
}
