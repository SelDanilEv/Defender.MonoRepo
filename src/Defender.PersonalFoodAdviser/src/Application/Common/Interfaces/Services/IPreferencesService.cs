using Defender.PersonalFoodAdviser.Domain.Entities;

namespace Defender.PersonalFoodAdviser.Application.Common.Interfaces.Services;

public interface IPreferencesService
{
    Task<UserPreferences> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<UserPreferences> UpdateAsync(Guid userId, IReadOnlyList<string> likes, IReadOnlyList<string> dislikes, CancellationToken cancellationToken = default);
}
