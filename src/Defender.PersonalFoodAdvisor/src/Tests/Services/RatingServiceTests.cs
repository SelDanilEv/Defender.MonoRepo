using Defender.PersonalFoodAdvisor.Application.Common.Interfaces.Repositories;
using Defender.PersonalFoodAdvisor.Application.Services;
using Defender.PersonalFoodAdvisor.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Defender.PersonalFoodAdvisor.Tests.Services;

public class RatingServiceTests
{
    private readonly Mock<IDishRatingRepository> _repository = new();
    private readonly Mock<ILogger<RatingService>> _logger = new();

    private RatingService CreateSut()
        => new(_repository.Object, _logger.Object);

    [Fact]
    public async Task SubmitRatingAsync_WhenDishHasNoExistingRating_CreatesNewRating()
    {
        var userId = Guid.NewGuid();

        _repository
            .Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _repository
            .Setup(r => r.CreateAsync(It.IsAny<DishRating>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DishRating rating, CancellationToken _) => rating);

        var sut = CreateSut();
        await sut.SubmitRatingAsync(userId, " Kebab box ", 5, Guid.NewGuid());

        _repository.Verify(r => r.CreateAsync(
            It.Is<DishRating>(rating =>
                rating.UserId == userId &&
                rating.DishName == "Kebab box" &&
                rating.Rating == 5),
            It.IsAny<CancellationToken>()), Times.Once);
        _repository.Verify(r => r.UpdateAsync(It.IsAny<DishRating>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SubmitRatingAsync_WhenDishAlreadyRated_UpdatesExistingRating()
    {
        var userId = Guid.NewGuid();
        var existingRating = new DishRating
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            DishName = "Kebab box",
            Rating = 1,
            CreatedAtUtc = DateTime.UtcNow.AddDays(-1)
        };
        var sessionId = Guid.NewGuid();

        _repository
            .Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([existingRating]);
        _repository
            .Setup(r => r.UpdateAsync(It.IsAny<DishRating>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DishRating rating, CancellationToken _) => rating);

        var sut = CreateSut();
        await sut.SubmitRatingAsync(userId, " kebab BOX ", 4, sessionId);

        _repository.Verify(r => r.UpdateAsync(
            It.Is<DishRating>(rating =>
                rating.Id == existingRating.Id &&
                rating.DishName == "kebab BOX" &&
                rating.Rating == 4 &&
                rating.SessionId == sessionId),
            It.IsAny<CancellationToken>()), Times.Once);
        _repository.Verify(r => r.CreateAsync(It.IsAny<DishRating>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetRatingsAsync_ReturnsRepositoryRatings()
    {
        var userId = Guid.NewGuid();
        var ratings = new List<DishRating>
        {
            new() { Id = Guid.NewGuid(), UserId = userId, DishName = "Baklawa", Rating = 5, CreatedAtUtc = DateTime.UtcNow }
        };

        _repository
            .Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ratings);

        var sut = CreateSut();
        var result = await sut.GetRatingsAsync(userId);

        Assert.Single(result);
        Assert.Equal("Baklawa", result[0].DishName);
        Assert.Equal(5, result[0].Rating);
    }
}
