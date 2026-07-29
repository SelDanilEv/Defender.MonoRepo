using Defender.Kafka.Default;
using Defender.PersonalFoodAdvisor.Application.Common.Interfaces.Services;
using Defender.PersonalFoodAdvisor.Application.Kafka;
using Defender.PersonalFoodAdvisor.Application.Services.Background.Kafka;
using Microsoft.Extensions.Logging.Abstractions;

namespace Defender.PersonalFoodAdvisor.Tests.Services;

public class FoodAdvisorEventListenerServiceTests
{
    [Fact]
    public async Task MenuParsingConsumerCallback_WhenProcessorFails_RethrowsProcessorException()
    {
        var menuParsingConsumer = new Mock<IDefaultKafkaConsumer<MenuParsingRequestedEvent>>();
        var recommendationsConsumer = new Mock<IDefaultKafkaConsumer<RecommendationsRequestedEvent>>();
        var menuParsingProcessor = new Mock<IMenuParsingProcessor>();
        var recommendationProcessor = new Mock<IRecommendationProcessor>();
        var callbackSource = new TaskCompletionSource<Func<MenuParsingRequestedEvent, Task>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var expectedException = new InvalidOperationException("menu parsing failed");

        menuParsingConsumer
            .Setup(x => x.StartConsuming(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Func<MenuParsingRequestedEvent, Task>>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, string, Func<MenuParsingRequestedEvent, Task>, CancellationToken>(
                (_, _, callback, _) => callbackSource.TrySetResult(callback))
            .Returns(Task.CompletedTask);
        recommendationsConsumer
            .Setup(x => x.StartConsuming(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Func<RecommendationsRequestedEvent, Task>>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        menuParsingProcessor
            .Setup(x => x.ProcessAsync(It.IsAny<MenuParsingRequestedEvent>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        var sut = new FoodAdvisorEventListenerService(
            menuParsingConsumer.Object,
            recommendationsConsumer.Object,
            menuParsingProcessor.Object,
            recommendationProcessor.Object,
            NullLogger<FoodAdvisorEventListenerService>.Instance);

        await sut.StartAsync(CancellationToken.None);
        var callback = await callbackSource.Task.WaitAsync(TimeSpan.FromSeconds(10));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => callback(new MenuParsingRequestedEvent(Guid.NewGuid(), Guid.NewGuid(), ["image-ref"])));

        Assert.Same(expectedException, exception);
    }

    [Fact]
    public async Task RecommendationsConsumerCallback_WhenProcessorFails_RethrowsProcessorException()
    {
        var menuParsingConsumer = new Mock<IDefaultKafkaConsumer<MenuParsingRequestedEvent>>();
        var recommendationsConsumer = new Mock<IDefaultKafkaConsumer<RecommendationsRequestedEvent>>();
        var menuParsingProcessor = new Mock<IMenuParsingProcessor>();
        var recommendationProcessor = new Mock<IRecommendationProcessor>();
        var callbackSource = new TaskCompletionSource<Func<RecommendationsRequestedEvent, Task>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var expectedException = new InvalidOperationException("recommendations failed");

        menuParsingConsumer
            .Setup(x => x.StartConsuming(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Func<MenuParsingRequestedEvent, Task>>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        recommendationsConsumer
            .Setup(x => x.StartConsuming(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Func<RecommendationsRequestedEvent, Task>>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, string, Func<RecommendationsRequestedEvent, Task>, CancellationToken>(
                (_, _, callback, _) => callbackSource.TrySetResult(callback))
            .Returns(Task.CompletedTask);
        recommendationProcessor
            .Setup(x => x.ProcessAsync(It.IsAny<RecommendationsRequestedEvent>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        var sut = new FoodAdvisorEventListenerService(
            menuParsingConsumer.Object,
            recommendationsConsumer.Object,
            menuParsingProcessor.Object,
            recommendationProcessor.Object,
            NullLogger<FoodAdvisorEventListenerService>.Instance);

        await sut.StartAsync(CancellationToken.None);
        var callback = await callbackSource.Task.WaitAsync(TimeSpan.FromSeconds(10));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => callback(new RecommendationsRequestedEvent(Guid.NewGuid(), Guid.NewGuid(), ["dish"], false)));

        Assert.Same(expectedException, exception);
    }
}
