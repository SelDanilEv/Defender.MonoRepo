using Defender.PersonalFoodAdviser.Application.Common.Interfaces.Repositories;
using Defender.PersonalFoodAdviser.Application.Common.Interfaces.Services;
using Defender.PersonalFoodAdviser.Application.Kafka;
using Defender.PersonalFoodAdviser.Application.Services;
using Defender.PersonalFoodAdviser.Domain.Entities;
using Microsoft.Extensions.Logging;
using System.Net;

namespace Defender.PersonalFoodAdviser.Tests.Services;

public class RecommendationProcessorTests
{
    private readonly Mock<IMenuSessionRepository> _menuSessionRepository = new();
    private readonly Mock<IUserPreferencesRepository> _userPreferencesRepository = new();
    private readonly Mock<IDishRatingRepository> _dishRatingRepository = new();
    private readonly Mock<IMenuIntelligenceClient> _menuIntelligenceClient = new();
    private readonly Mock<IRecommendationsOutboxService> _recommendationsOutboxService = new();
    private readonly Mock<ILogger<RecommendationProcessor>> _logger = new();

    private RecommendationProcessor CreateSut()
        => new(
            _menuSessionRepository.Object,
            _userPreferencesRepository.Object,
            _dishRatingRepository.Object,
            _menuIntelligenceClient.Object,
            _recommendationsOutboxService.Object,
            _logger.Object);

    [Fact]
    public async Task ProcessAsync_WhenEventUserDoesNotOwnSession_DoesNotProcess()
    {
        var sessionId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var evt = new RecommendationsRequestedEvent(sessionId, Guid.NewGuid(), ["Dish"], false);

        _menuSessionRepository
            .Setup(r => r.GetByIdAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MenuSession { Id = sessionId, UserId = ownerId, ConfirmedItems = ["Dish"] });

        var sut = CreateSut();
        await sut.ProcessAsync(evt);

        _menuIntelligenceClient.Verify(h => h.GetRankedRecommendationsAsync(
            It.IsAny<IReadOnlyList<string>>(),
            It.IsAny<IReadOnlyList<string>>(),
            It.IsAny<IReadOnlyList<string>>(),
            It.IsAny<IReadOnlyList<(string DishName, int Rating)>>(),
            It.IsAny<bool>(),
            It.IsAny<int>(),
            It.IsAny<CancellationToken>()), Times.Never);
        _menuSessionRepository.Verify(r => r.UpdateAsync(It.IsAny<MenuSession>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessAsync_WhenSessionHasNoConfirmedItems_ClearsRankingsWithoutCallingModel()
    {
        var sessionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var session = new MenuSession
        {
            Id = sessionId,
            UserId = userId,
            ConfirmedItems = [],
            RankedItems = ["Old result"]
        };
        var evt = new RecommendationsRequestedEvent(sessionId, userId, ["Old event item"], true);

        _menuSessionRepository
            .Setup(r => r.GetByIdAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);
        _menuSessionRepository
            .Setup(r => r.UpdateAsync(It.IsAny<MenuSession>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MenuSession s, CancellationToken _) => s);
        _userPreferencesRepository
            .Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserPreferences?)null);
        _dishRatingRepository
            .Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var sut = CreateSut();
        await sut.ProcessAsync(evt);

        _menuIntelligenceClient.Verify(h => h.GetRankedRecommendationsAsync(
            It.IsAny<IReadOnlyList<string>>(),
            It.IsAny<IReadOnlyList<string>>(),
            It.IsAny<IReadOnlyList<string>>(),
            It.IsAny<IReadOnlyList<(string DishName, int Rating)>>(),
            It.IsAny<bool>(),
            It.IsAny<int>(),
            It.IsAny<CancellationToken>()), Times.Never);
        _menuSessionRepository.Verify(r => r.UpdateAsync(It.Is<MenuSession>(s => s.RankedItems.Count == 0), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessAsync_WhenEventPayloadIsStale_UsesPersistedSessionChoices()
    {
        var sessionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var session = new MenuSession
        {
            Id = sessionId,
            UserId = userId,
            ConfirmedItems = [" Ramen ", "Curry", "ramen"],
            TrySomethingNew = false
        };
        var evt = new RecommendationsRequestedEvent(sessionId, userId, ["Stale Dish"], true);

        _menuSessionRepository
            .Setup(r => r.GetByIdAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);
        _menuSessionRepository
            .Setup(r => r.UpdateAsync(It.IsAny<MenuSession>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MenuSession s, CancellationToken _) => s);
        _userPreferencesRepository
            .Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserPreferences { UserId = userId, Likes = ["Spicy"] });
        _dishRatingRepository
            .Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _menuIntelligenceClient
            .Setup(h => h.GetRankedRecommendationsAsync(
                It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<IReadOnlyList<(string DishName, int Rating)>>(),
                It.IsAny<bool>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([" Curry ", "Ramen", "curry"]);
        var expectedConfirmedItems = new[] { "Ramen", "Curry" };
        var expectedLikes = new[] { "Spicy" };
        var expectedRankedItems = new[] { "Curry", "Ramen" };

        var sut = CreateSut();
        await sut.ProcessAsync(evt);

        _menuIntelligenceClient.Verify(h => h.GetRankedRecommendationsAsync(
            It.Is<IReadOnlyList<string>>(items => items.SequenceEqual(expectedConfirmedItems)),
            It.Is<IReadOnlyList<string>>(likes => likes.SequenceEqual(expectedLikes)),
            It.IsAny<IReadOnlyList<string>>(),
            It.IsAny<IReadOnlyList<(string DishName, int Rating)>>(),
            false,
            10,
            It.IsAny<CancellationToken>()), Times.Once);
        _menuSessionRepository.Verify(r => r.UpdateAsync(
            It.Is<MenuSession>(s => s.RankedItems.SequenceEqual(expectedRankedItems)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessAsync_WhenModelReturnsTooManyRequests_SchedulesAutomaticRetry()
    {
        var sessionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var session = new MenuSession
        {
            Id = sessionId,
            UserId = userId,
            ConfirmedItems = ["Kebab box"],
            RankedItems = ["Old result"]
        };
        var evt = new RecommendationsRequestedEvent(sessionId, userId, ["Kebab box"], false);

        _menuSessionRepository
            .Setup(r => r.GetByIdAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);
        _menuSessionRepository
            .Setup(r => r.UpdateAsync(It.IsAny<MenuSession>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MenuSession s, CancellationToken _) => s);
        _userPreferencesRepository
            .Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserPreferences?)null);
        _dishRatingRepository
            .Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _menuIntelligenceClient
            .Setup(h => h.GetRankedRecommendationsAsync(
                It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<IReadOnlyList<(string DishName, int Rating)>>(),
                It.IsAny<bool>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("rate limited", null, HttpStatusCode.TooManyRequests));
        _recommendationsOutboxService
            .Setup(x => x.ScheduleRetryAsync(evt, HttpStatusCode.TooManyRequests, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var sut = CreateSut();
        await sut.ProcessAsync(evt);

        _recommendationsOutboxService.Verify(
            x => x.ScheduleRetryAsync(evt, HttpStatusCode.TooManyRequests, It.IsAny<CancellationToken>()),
            Times.Once);
        _menuSessionRepository.Verify(r => r.UpdateAsync(It.IsAny<MenuSession>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessAsync_WhenModelReturnsTooManyRequestsAfterRetryBudget_SetsManualRefreshWarningAndClearsRankings()
    {
        var sessionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var session = new MenuSession
        {
            Id = sessionId,
            UserId = userId,
            ConfirmedItems = ["Kebab box"],
            RankedItems = ["Old result"]
        };
        var evt = new RecommendationsRequestedEvent(sessionId, userId, ["Kebab box"], false, 10);

        _menuSessionRepository
            .Setup(r => r.GetByIdAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);
        _menuSessionRepository
            .Setup(r => r.UpdateAsync(It.IsAny<MenuSession>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MenuSession s, CancellationToken _) => s);
        _userPreferencesRepository
            .Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserPreferences?)null);
        _dishRatingRepository
            .Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _menuIntelligenceClient
            .Setup(h => h.GetRankedRecommendationsAsync(
                It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<IReadOnlyList<(string DishName, int Rating)>>(),
                It.IsAny<bool>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("rate limited", null, HttpStatusCode.TooManyRequests));
        _recommendationsOutboxService
            .Setup(x => x.ScheduleRetryAsync(evt, HttpStatusCode.TooManyRequests, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var sut = CreateSut();
        await sut.ProcessAsync(evt);

        _menuSessionRepository.Verify(r => r.UpdateAsync(
            It.Is<MenuSession>(s =>
                s.RankedItems.Count == 0 &&
                s.RecommendationWarningCode == "ProviderBusy" &&
                s.RecommendationWarningMessage != null &&
                s.RecommendationWarningMessage.Contains("Refresh", StringComparison.Ordinal)),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
