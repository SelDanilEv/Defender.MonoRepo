using Defender.PersonalFoodAdvisor.Application.Common.Interfaces.Repositories;
using Defender.PersonalFoodAdvisor.Application.Common.Interfaces.Services;
using Defender.PersonalFoodAdvisor.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Defender.PersonalFoodAdvisor.Application.Services;

public class RatingService(
    IDishRatingRepository repository,
    ILogger<RatingService> logger) : IRatingService
{
    public async Task<IReadOnlyList<DishRating>> GetRatingsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Loading dish ratings for user {UserId}", userId);
        var ratings = await repository.GetByUserIdAsync(userId, cancellationToken);
        logger.LogInformation("Loaded {Count} dish ratings for user {UserId}", ratings.Count, userId);
        return ratings;
    }

    public async Task SubmitRatingAsync(Guid userId, string dishName, int rating, Guid? sessionId, CancellationToken cancellationToken = default)
    {
        var normalizedDishName = NormalizeDishName(dishName);
        var clampedRating = Math.Clamp(rating, 1, 5);
        if (rating != clampedRating)
            logger.LogWarning("Rating value out of range for user {UserId}, session {SessionId}: input {InputRating}, clamped {ClampedRating}", userId, sessionId, rating, clampedRating);

        logger.LogInformation("Submitting rating for user {UserId}, session {SessionId}, hasDishName {HasDishName}, rating {Rating}", userId, sessionId, !string.IsNullOrWhiteSpace(normalizedDishName), clampedRating);
        var existingRating = (await repository.GetByUserIdAsync(userId, cancellationToken))
            .FirstOrDefault(existing => string.Equals(
                NormalizeDishName(existing.DishName),
                normalizedDishName,
                StringComparison.OrdinalIgnoreCase));

        if (existingRating != null)
        {
            existingRating.DishName = normalizedDishName;
            existingRating.Rating = clampedRating;
            existingRating.SessionId = sessionId;
            await repository.UpdateAsync(existingRating, cancellationToken);
            logger.LogInformation("Updated rating for user {UserId}, session {SessionId}, dishName {DishName}", userId, sessionId, existingRating.DishName);
            return;
        }

        var entity = new DishRating
        {
            UserId = userId,
            DishName = normalizedDishName,
            Rating = clampedRating,
            SessionId = sessionId
        };
        await repository.CreateAsync(entity, cancellationToken);
        logger.LogInformation("Created rating for user {UserId}, session {SessionId}, dishName {DishName}", userId, sessionId, entity.DishName);
    }

    private static string NormalizeDishName(string? dishName)
        => dishName?.Trim() ?? string.Empty;
}
