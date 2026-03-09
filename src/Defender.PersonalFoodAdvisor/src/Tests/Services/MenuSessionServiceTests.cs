using Defender.PersonalFoodAdvisor.Application.Common.Interfaces.Repositories;
using Defender.PersonalFoodAdvisor.Application.Common.Interfaces.Services;
using Defender.PersonalFoodAdvisor.Application.Kafka;
using Defender.PersonalFoodAdvisor.Application.Services;
using Defender.PersonalFoodAdvisor.Domain.Entities;
using Defender.PersonalFoodAdvisor.Domain.Enums;
using Microsoft.Extensions.Logging;
using Moq;

namespace Defender.PersonalFoodAdvisor.Tests.Services;

public class MenuSessionServiceTests
{
    private readonly Mock<IMenuSessionRepository> _repository = new();
    private readonly Mock<IImageBlobRepository> _imageBlobRepository = new();
    private readonly Mock<IDishRatingRepository> _dishRatingRepository = new();
    private readonly Mock<IMenuParsingOutboxRepository> _menuParsingOutboxRepository = new();
    private readonly Mock<IRecommendationsOutboxRepository> _recommendationsOutboxRepository = new();
    private readonly Mock<IMenuParsingOutboxService> _menuParsingOutboxService = new();
    private readonly Mock<IRecommendationsOutboxService> _recommendationsOutboxService = new();
    private readonly Mock<ILogger<MenuSessionService>> _logger = new();

    private MenuSessionService CreateSut()
        => new(
            _repository.Object,
            _imageBlobRepository.Object,
            _dishRatingRepository.Object,
            _menuParsingOutboxRepository.Object,
            _recommendationsOutboxRepository.Object,
            _menuParsingOutboxService.Object,
            _recommendationsOutboxService.Object,
            _logger.Object);

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
    public async Task GetByUserIdAsync_WhenSessionsExist_ReturnsSessionsFromRepository()
    {
        var userId = Guid.NewGuid();
        var sessions = new List<MenuSession>
        {
            new() { Id = Guid.NewGuid(), UserId = userId, Status = MenuSessionStatus.Review },
            new() { Id = Guid.NewGuid(), UserId = userId, Status = MenuSessionStatus.Confirmed }
        };

        _repository
            .Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sessions);

        var sut = CreateSut();
        var result = await sut.GetByUserIdAsync(userId);

        Assert.Equal(2, result.Count);
        Assert.Equal(sessions[0].Id, result[0].Id);
        Assert.Equal(sessions[1].Id, result[1].Id);
    }

    [Fact]
    public async Task ConfirmAsync_WhenSessionBelongsToUser_UpdatesConfirmedItemsAndStatus()
    {
        var sessionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var session = new MenuSession
        {
            Id = sessionId,
            UserId = userId,
            Status = MenuSessionStatus.Review,
            RankedItems = ["Old result"]
        };

        _repository
            .Setup(r => r.GetByIdAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);
        _repository
            .Setup(r => r.UpdateAsync(It.IsAny<MenuSession>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MenuSession s, CancellationToken _) => s);

        var sut = CreateSut();
        var result = await sut.ConfirmAsync(sessionId, userId, [" Ramen ", "Curry", "ramen"], true);

        Assert.NotNull(result);
        Assert.Equal(MenuSessionStatus.Confirmed, result!.Status);
        Assert.True(result.TrySomethingNew);
        Assert.Equal(["Ramen", "Curry"], result.ConfirmedItems);
        Assert.Empty(result.RankedItems);
    }

    [Fact]
    public async Task DeleteAsync_WhenSessionBelongsToUser_DeletesSessionAndAssociatedBlobs()
    {
        var sessionId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _repository
            .Setup(r => r.GetByIdAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MenuSession { Id = sessionId, UserId = userId, Status = MenuSessionStatus.Uploaded });

        var sut = CreateSut();
        var result = await sut.DeleteAsync(sessionId, userId);

        Assert.True(result);
        _repository.Verify(r => r.DeleteAsync(sessionId, It.IsAny<CancellationToken>()), Times.Once);
        _dishRatingRepository.Verify(r => r.DeleteBySessionIdAsync(sessionId, It.IsAny<CancellationToken>()), Times.Once);
        _imageBlobRepository.Verify(r => r.DeleteBySessionIdAsync(sessionId, It.IsAny<CancellationToken>()), Times.Once);
        _menuParsingOutboxRepository.Verify(r => r.DeleteBySessionIdAsync(sessionId, It.IsAny<CancellationToken>()), Times.Once);
        _recommendationsOutboxRepository.Verify(r => r.DeleteBySessionIdAsync(sessionId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenSessionBelongsToDifferentUser_DoesNotDelete()
    {
        var sessionId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var anotherUserId = Guid.NewGuid();

        _repository
            .Setup(r => r.GetByIdAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MenuSession { Id = sessionId, UserId = ownerId, Status = MenuSessionStatus.Uploaded });

        var sut = CreateSut();
        var result = await sut.DeleteAsync(sessionId, anotherUserId);

        Assert.False(result);
        _repository.Verify(r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _dishRatingRepository.Verify(r => r.DeleteBySessionIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _imageBlobRepository.Verify(r => r.DeleteBySessionIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _menuParsingOutboxRepository.Verify(r => r.DeleteBySessionIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _recommendationsOutboxRepository.Verify(r => r.DeleteBySessionIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RequestParsingAsync_WhenSessionBelongsToDifferentUser_DoesNotEnqueueOutboxMessage()
    {
        var sessionId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var anotherUserId = Guid.NewGuid();

        _repository
            .Setup(r => r.GetByIdAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MenuSession { Id = sessionId, UserId = ownerId });

        var sut = CreateSut();
        await sut.RequestParsingAsync(sessionId, anotherUserId);

        _menuParsingOutboxService.Verify(
            p => p.EnqueueAsync(
                It.IsAny<MenuParsingRequestedEvent>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RequestParsingAsync_WhenSessionBelongsToUser_EnqueuesOutboxMessage()
    {
        var sessionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var session = new MenuSession
        {
            Id = sessionId,
            UserId = userId,
            ImageRefs = ["img-1", "img-2"]
        };

        _repository
            .Setup(r => r.GetByIdAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        var sut = CreateSut();
        await sut.RequestParsingAsync(sessionId, userId);

        _menuParsingOutboxService.Verify(
            p => p.EnqueueAsync(
                It.Is<MenuParsingRequestedEvent>(evt =>
                    evt.SessionId == sessionId &&
                    evt.UserId == userId &&
                    evt.ImageRefs.SequenceEqual(new[] { "img-1", "img-2" })),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RequestRecommendationsAsync_WhenSessionBelongsToDifferentUser_DoesNotEnqueueOutboxMessage()
    {
        var sessionId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var anotherUserId = Guid.NewGuid();

        _repository
            .Setup(r => r.GetByIdAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MenuSession { Id = sessionId, UserId = ownerId });

        var sut = CreateSut();
        await sut.RequestRecommendationsAsync(sessionId, anotherUserId);

        _recommendationsOutboxService.Verify(
            p => p.EnqueueAsync(
                It.IsAny<RecommendationsRequestedEvent>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RequestRecommendationsAsync_WhenSessionBelongsToUser_ClearsWarningAndEnqueuesOutboxMessage()
    {
        var sessionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var session = new MenuSession
        {
            Id = sessionId,
            UserId = userId,
            ConfirmedItems = ["Kebab box"],
            TrySomethingNew = true,
            RecommendationWarningCode = "ProviderBusy",
            RecommendationWarningMessage = "Recommendation provider rate limit reached. Click Refresh to retry manually."
        };

        _repository
            .Setup(r => r.GetByIdAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);
        _repository
            .Setup(r => r.UpdateAsync(It.IsAny<MenuSession>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MenuSession s, CancellationToken _) => s);

        var sut = CreateSut();
        await sut.RequestRecommendationsAsync(sessionId, userId);

        _repository.Verify(
            r => r.UpdateAsync(
                It.Is<MenuSession>(s =>
                    s.Id == sessionId &&
                    s.RecommendationWarningCode == null &&
                    s.RecommendationWarningMessage == null),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _recommendationsOutboxService.Verify(
            p => p.EnqueueAsync(
                It.Is<RecommendationsRequestedEvent>(evt =>
                    evt.SessionId == sessionId &&
                    evt.UserId == userId &&
                    evt.TrySomethingNew &&
                    evt.Attempt == 0 &&
                    evt.ConfirmedItems.SequenceEqual(new[] { "Kebab box" })),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateImageRefsAsync_WhenRefsChange_ResetsDerivedStateAndNormalizesRefs()
    {
        var sessionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var session = new MenuSession
        {
            Id = sessionId,
            UserId = userId,
            Status = MenuSessionStatus.Confirmed,
            ImageRefs = ["old-ref"],
            ParsedItems = ["Pasta"],
            ConfirmedItems = ["Pasta"],
            RankedItems = ["Pasta"],
            TrySomethingNew = true
        };

        _repository
            .Setup(r => r.GetByIdAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);
        _repository
            .Setup(r => r.UpdateAsync(It.IsAny<MenuSession>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MenuSession s, CancellationToken _) => s);

        var sut = CreateSut();
        var result = await sut.UpdateImageRefsAsync(sessionId, userId, [" ref-1 ", "ref-1", "ref-2"]);

        Assert.NotNull(result);
        Assert.Equal(MenuSessionStatus.Uploaded, result!.Status);
        Assert.Equal(["ref-1", "ref-2"], result.ImageRefs);
        Assert.Empty(result.ParsedItems);
        Assert.Empty(result.ConfirmedItems);
        Assert.Empty(result.RankedItems);
        Assert.False(result.TrySomethingNew);
    }
}
