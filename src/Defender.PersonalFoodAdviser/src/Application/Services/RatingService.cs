using Defender.PersonalFoodAdviser.Application.Common.Interfaces.Repositories;
using Defender.PersonalFoodAdviser.Application.Common.Interfaces.Services;
using Defender.PersonalFoodAdviser.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Defender.PersonalFoodAdviser.Application.Services;

public class RatingService(
    IDishRatingRepository repository,
    ILogger<RatingService> logger) : IRatingService
{
    public async Task SubmitRatingAsync(Guid userId, string dishName, int rating, Guid? sessionId, CancellationToken cancellationToken = default)
    {
        var clampedRating = Math.Clamp(rating, 1, 5);
        if (rating != clampedRating)
            logger.LogWarning("Rating value out of range for user {UserId}, session {SessionId}: input {InputRating}, clamped {ClampedRating}", userId, sessionId, rating, clampedRating);

        logger.LogInformation("Submitting rating for user {UserId}, session {SessionId}, hasDishName {HasDishName}, rating {Rating}", userId, sessionId, !string.IsNullOrWhiteSpace(dishName), clampedRating);
        var entity = new DishRating
        {
            UserId = userId,
            DishName = dishName ?? string.Empty,
            Rating = clampedRating,
            SessionId = sessionId
        };
        await repository.CreateAsync(entity, cancellationToken);
        logger.LogInformation("Submitted rating for user {UserId}, session {SessionId}, dishName {DishName}", userId, sessionId, entity.DishName);
    }
}
