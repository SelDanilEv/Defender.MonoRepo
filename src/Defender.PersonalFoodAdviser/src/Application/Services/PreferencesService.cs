using Defender.PersonalFoodAdviser.Application.Common.Interfaces.Repositories;
using Defender.PersonalFoodAdviser.Application.Common.Interfaces.Services;
using Defender.PersonalFoodAdviser.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Defender.PersonalFoodAdviser.Application.Services;

public class PreferencesService(
    IUserPreferencesRepository repository,
    ILogger<PreferencesService> logger) : IPreferencesService
{
    public async Task<UserPreferences> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Loading preferences for user {UserId}", userId);
        var preferences = await repository.GetByUserIdAsync(userId, cancellationToken);
        if (preferences != null)
        {
            logger.LogInformation("Loaded existing preferences for user {UserId}: likes {LikesCount}, dislikes {DislikesCount}", userId, preferences.Likes.Count, preferences.Dislikes.Count);
            return preferences;
        }

        logger.LogInformation("No preferences found for user {UserId}; creating defaults", userId);
        var created = new UserPreferences { UserId = userId };
        preferences = await repository.UpsertAsync(created, cancellationToken);
        logger.LogInformation("Created default preferences for user {UserId}", userId);
        return preferences;
    }

    public async Task<UserPreferences> UpdateAsync(Guid userId, IReadOnlyList<string> likes, IReadOnlyList<string> dislikes, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Updating preferences for user {UserId}: likes {LikesCount}, dislikes {DislikesCount}", userId, likes?.Count ?? 0, dislikes?.Count ?? 0);
        var preferences = await repository.GetByUserIdAsync(userId, cancellationToken)
            ?? new UserPreferences { UserId = userId };

        var previousLikesCount = preferences.Likes.Count;
        var previousDislikesCount = preferences.Dislikes.Count;
        preferences.Likes = likes?.ToList() ?? [];
        preferences.Dislikes = dislikes?.ToList() ?? [];
        preferences = await repository.UpsertAsync(preferences, cancellationToken);
        logger.LogInformation(
            "Updated preferences for user {UserId}: likes {OldLikes}->{NewLikes}, dislikes {OldDislikes}->{NewDislikes}",
            userId,
            previousLikesCount,
            preferences.Likes.Count,
            previousDislikesCount,
            preferences.Dislikes.Count);
        return preferences;
    }
}
