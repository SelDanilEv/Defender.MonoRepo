using Defender.Kafka.Default;
using Defender.PersonalFoodAdviser.Application.Common.Interfaces.Repositories;
using Defender.PersonalFoodAdviser.Application.Kafka;
using Defender.PersonalFoodAdviser.Application.Services;
using Defender.PersonalFoodAdviser.Domain.Entities;
using Defender.PersonalFoodAdviser.Domain.Enums;
using Microsoft.Extensions.Logging;
using Moq;

namespace Defender.PersonalFoodAdviser.Tests;

public class MenuSessionServiceTests
{
    private readonly Mock<IMenuSessionRepository> _repository = new();
    private readonly Mock<IDefaultKafkaProducer<MenuParsingRequestedEvent>> _parsingProducer = new();
    private readonly Mock<IDefaultKafkaProducer<RecommendationsRequestedEvent>> _recommendationsProducer = new();
    private readonly Mock<ILogger<MenuSessionService>> _logger = new();

    private MenuSessionService CreateSut()
        => new(_repository.Object, _parsingProducer.Object, _recommendationsProducer.Object, _logger.Object);

    [Fact]
    public async Task GetByIdAsync_WhenSessionBelongsToDifferentUser_ReturnsNull()
    {
        var sessionId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var anotherUserId = Guid.NewGuid();
        _repository
            .Setup(r => r.GetByIdAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MenuSession { Id = sessionId, UserId = ownerId, Status = MenuSessionStatus.Uploaded });

        var sut = CreateSut();
        var result = await sut.GetByIdAsync(sessionId, anotherUserId);

        Assert.Null(result);
    }

    [Fact]
    public async Task ConfirmAsync_WhenSessionBelongsToUser_UpdatesConfirmedItemsAndStatus()
    {
        var sessionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var session = new MenuSession { Id = sessionId, UserId = userId, Status = MenuSessionStatus.Review };

        _repository
            .Setup(r => r.GetByIdAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);
        _repository
            .Setup(r => r.UpdateAsync(It.IsAny<MenuSession>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MenuSession s, CancellationToken _) => s);

        var sut = CreateSut();
        var result = await sut.ConfirmAsync(sessionId, userId, ["Ramen", "Curry"], true);

        Assert.NotNull(result);
        Assert.Equal(MenuSessionStatus.Confirmed, result!.Status);
        Assert.True(result.TrySomethingNew);
        Assert.Equal(["Ramen", "Curry"], result.ConfirmedItems);
    }

    [Fact]
    public async Task RequestRecommendationsAsync_WhenSessionBelongsToDifferentUser_DoesNotProduce()
    {
        var sessionId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var anotherUserId = Guid.NewGuid();

        _repository
            .Setup(r => r.GetByIdAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MenuSession { Id = sessionId, UserId = ownerId });

        var sut = CreateSut();
        await sut.RequestRecommendationsAsync(sessionId, anotherUserId);

        _recommendationsProducer.Verify(
            p => p.ProduceAsync(
                It.IsAny<string>(),
                It.IsAny<RecommendationsRequestedEvent>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
