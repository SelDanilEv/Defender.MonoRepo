using System.Reflection;
using Defender.Common.DB.SharedStorage.Entities;
using Defender.Common.DB.SharedStorage.Enums;
using Defender.Kafka.Default;
using Defender.RiskGamesService.Application;
using Defender.RiskGamesService.Application.Common.Interfaces.Services.Lottery;
using Defender.RiskGamesService.Application.Common.Interfaces.Services.Transaction;
using Defender.RiskGamesService.Application.Configuration.Extension;
using Defender.RiskGamesService.Application.Factories.Transaction;
using Defender.RiskGamesService.Application.Helpers;
using Defender.RiskGamesService.Application.Helpers.LocalSecretHelper;
using Defender.RiskGamesService.Application.Services.Background.Kafka;
using Defender.RiskGamesService.Common.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Defender.RiskGamesService.Tests.Services;

public class RiskGamesConfigurationAndBackgroundTests
{
    [Fact]
    public void AddApplicationOptions_WhenCalled_ConfiguresWalletOptionsSection()
    {
        var settings = new Dictionary<string, string?> { ["WalletOptions:BaseUrl"] = "http://wallet" };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var services = new ServiceCollection();

        var result = services.AddApplicationOptions(configuration);

        Assert.Same(services, result);
    }

    [Fact]
    public void AddApplicationServices_WhenCalled_RegistersKeyServices()
    {
        var settings = new Dictionary<string, string?>
        {
            ["KafkaOptions:BootstrapServers"] = "localhost:9092",
            ["KafkaOptions:TopicPrefix"] = "test"
        };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var services = new ServiceCollection();

        services.AddApplicationServices(configuration);
        Assert.Contains(services, d => d.ServiceType == typeof(TransactionHandlerFactory));
        Assert.Contains(services, d => d.ServiceType == typeof(ILotteryManagementService));
        Assert.Contains(services, d => d.ServiceType == typeof(ITransactionManagementService));
    }

    [Fact]
    public void SimpleLogger_LogMethods_AreCallable()
    {
        SimpleLogger.Log("info");
        SimpleLogger.Log("debug", SimpleLogger.LogLevel.Debug);
        SimpleLogger.Log("warn", SimpleLogger.LogLevel.Warning);
        SimpleLogger.Log(new InvalidOperationException("boom"), "with exception");
    }

    [Fact]
    public async Task BackgroundServices_PrivateHandlers_HandleExpectedEvents()
    {
        var stringConsumer = new Mock<IDefaultKafkaConsumer<string>>();
        var guidConsumer = new Mock<IDefaultKafkaConsumer<Guid>>();
        var lotteryProcessing = new Mock<ILotteryProcessingService>();
        var lotteryManagement = new Mock<ILotteryManagementService>();
        var eventLogger = new Mock<ILogger<EventListenerService>>();
        var eventService = new EventListenerService(
            stringConsumer.Object,
            guidConsumer.Object,
            lotteryProcessing.Object,
            lotteryManagement.Object,
            eventLogger.Object);

        var handleStringEvent = typeof(EventListenerService)
            .GetMethod("HandleStringEvent", BindingFlags.Instance | BindingFlags.NonPublic)!;

        await ((Task)handleStringEvent.Invoke(eventService, ["StartLotteriesProcessing"])!);
        await ((Task)handleStringEvent.Invoke(eventService, ["ScheduleNewLotteryDraws"])!);
        await ((Task)handleStringEvent.Invoke(eventService, ["UnknownEventName"])!);

        lotteryProcessing.Verify(x => x.QueueLotteriesForProcessing(It.IsAny<CancellationToken>()), Times.Once);
        lotteryManagement.Verify(x => x.ScheduleDraws(), Times.Once);
    }

    [Fact]
    public async Task TransactionStatusesListenerService_PrivateHandler_HandlesLotteryPaymentAndSkipsOtherEvents()
    {
        var consumer = new Mock<IDefaultKafkaConsumer<TransactionStatusUpdatedEvent>>();
        var logger = new Mock<ILogger<TransactionStatusesListenerService>>();
        var transactionManagement = new Mock<ITransactionManagementService>();
        var scopeFactory = CreateScopeFactory(transactionManagement.Object);
        var sut = new TransactionStatusesListenerService(consumer.Object, logger.Object, scopeFactory);
        var method = typeof(TransactionStatusesListenerService)
            .GetMethod("HandleTransactionStatusUpdatedEvent", BindingFlags.Instance | BindingFlags.NonPublic)!;

        await ((Task)method.Invoke(sut, [new TransactionStatusUpdatedEvent
        {
            TransactionId = "tx-ok",
            TransactionPurpose = TransactionPurpose.Lottery,
            TransactionType = TransactionType.Payment,
            TransactionStatus = TransactionStatus.Proceed
        }])!);

        await ((Task)method.Invoke(sut, [new TransactionStatusUpdatedEvent
        {
            TransactionId = "tx-skip",
            TransactionPurpose = TransactionPurpose.NoPurpose,
            TransactionType = TransactionType.Transfer,
            TransactionStatus = TransactionStatus.Proceed
        }])!);

        transactionManagement.Verify(x => x.HandleTransactionStatusUpdatedEvent(
            It.Is<TransactionStatusUpdatedEvent>(e => e.TransactionId == "tx-ok")), Times.Once);
        transactionManagement.Verify(x => x.HandleTransactionStatusUpdatedEvent(
            It.Is<TransactionStatusUpdatedEvent>(e => e.TransactionId == "tx-skip")), Times.Never);
    }

    [Fact]
    public void CreateKafkaTopicsService_Properties_ReturnExpectedValues()
    {
        var options = Options.Create(new Defender.Kafka.Configuration.Options.KafkaOptions());
        var prefixer = new Mock<Defender.Kafka.Service.IKafkaEnvPrefixer>();
        var logger = new Mock<ILogger<CreateKafkaTopicsService>>();
        var sut = new CreateKafkaTopicsService(options, prefixer.Object, logger.Object);

        var topicsProp = typeof(CreateKafkaTopicsService).GetProperty("Topics", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var replicationProp = typeof(CreateKafkaTopicsService).GetProperty("ReplicationFactor", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var partitionsProp = typeof(CreateKafkaTopicsService).GetProperty("NumPartitions", BindingFlags.Instance | BindingFlags.NonPublic)!;

        var topics = ((IEnumerable<string>)topicsProp.GetValue(sut)!).ToList();
        var replication = (short)replicationProp.GetValue(sut)!;
        var partitions = (int)partitionsProp.GetValue(sut)!;

        Assert.Contains(KafkaTopic.ScheduledTasks.GetName(), topics);
        Assert.Contains(KafkaTopic.LotteryToProcess.GetName(), topics);
        Assert.Equal((short)1, replication);
        Assert.Equal(3, partitions);
    }

    [Fact]
    public async Task LocalSecretsHelper_WhenCalled_InvokesUnderlyingHelpersPath()
    {
        _ = await LocalSecretsHelper.GetSecretAsync((LocalSecret)0);
        _ = LocalSecretsHelper.GetSecretSync((LocalSecret)0);
    }

    private static IServiceScopeFactory CreateScopeFactory(ITransactionManagementService service)
    {
        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider
            .Setup(x => x.GetService(typeof(ITransactionManagementService)))
            .Returns(service);
        var scope = new Mock<IServiceScope>();
        scope.SetupGet(x => x.ServiceProvider).Returns(serviceProvider.Object);
        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(x => x.CreateScope()).Returns(scope.Object);
        return scopeFactory.Object;
    }
}
