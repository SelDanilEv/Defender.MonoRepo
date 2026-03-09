using Defender.Common.Configuration.Options;
using Defender.Common.DB.Model;
using Defender.Common.DB.Repositories;
using Defender.PersonalFoodAdvisor.Application.Common.Interfaces.Repositories;
using Defender.PersonalFoodAdvisor.Domain.Entities;
using Microsoft.Extensions.Options;

namespace Defender.PersonalFoodAdvisor.Infrastructure.Repositories;

public class UserPreferencesRepository : BaseMongoRepository<UserPreferences>, IUserPreferencesRepository
{
    public UserPreferencesRepository(IOptions<MongoDbOptions> mongoOption)
        : base(mongoOption.Value, "UserPreferences")
    {
    }

    public async Task<UserPreferences?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var request = FindModelRequest<UserPreferences>.Init(x => x.UserId, userId);
        return await GetItemAsync(request);
    }

    public async Task<UserPreferences> UpsertAsync(UserPreferences preferences, CancellationToken cancellationToken = default)
    {
        var existing = await GetByUserIdAsync(preferences.UserId, cancellationToken);
        if (existing == null)
        {
            if (preferences.Id == Guid.Empty)
                preferences.Id = Guid.NewGuid();
            return await AddItemAsync(preferences);
        }
        preferences.Id = existing.Id;
        return await ReplaceItemAsync(preferences);
    }
}
