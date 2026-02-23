using Defender.PersonalFoodAdviser.Application.Common.Interfaces.Repositories;
using Defender.PersonalFoodAdviser.Application.Common.Interfaces.Services;
using Defender.PersonalFoodAdviser.Domain.Entities;

namespace Defender.PersonalFoodAdviser.Application.Services;

public class PreferencesService(IUserPreferencesRepository repository) : IPreferencesService
{
    public async Task<UserPreferences> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var preferences = await repository.GetByUserIdAsync(userId, cancellationToken);
        if (preferences != null)
            return preferences;
        var created = new UserPreferences { UserId = userId };
        return await repository.UpsertAsync(created, cancellationToken);
    }

    public async Task<UserPreferences> UpdateAsync(Guid userId, IReadOnlyList<string> likes, IReadOnlyList<string> dislikes, CancellationToken cancellationToken = default)
    {
        var preferences = await repository.GetByUserIdAsync(userId, cancellationToken)
            ?? new UserPreferences { UserId = userId };
        preferences.Likes = likes?.ToList() ?? [];
        preferences.Dislikes = dislikes?.ToList() ?? [];
        return await repository.UpsertAsync(preferences, cancellationToken);
    }
}
