using Defender.Common.Configuration.Options;
using Defender.Common.DB.Repositories;
using Defender.PersonalFoodAdviser.Application.Common.Interfaces.Repositories;
using Defender.PersonalFoodAdviser.Domain.Entities;
using Microsoft.Extensions.Options;

namespace Defender.PersonalFoodAdviser.Infrastructure.Repositories;

public class MenuSessionRepository : BaseMongoRepository<MenuSession>, IMenuSessionRepository
{
    public MenuSessionRepository(IOptions<MongoDbOptions> mongoOption)
        : base(mongoOption.Value, "MenuSession")
    {
    }

    public async Task<MenuSession> CreateAsync(MenuSession session, CancellationToken cancellationToken = default)
    {
        if (session.Id == Guid.Empty)
            session.Id = Guid.NewGuid();
        session.CreatedAtUtc = DateTime.UtcNow;
        return await AddItemAsync(session);
    }

    public async Task<MenuSession?> GetByIdAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        return await GetItemAsync(sessionId);
    }

    public async Task<MenuSession> UpdateAsync(MenuSession session, CancellationToken cancellationToken = default)
    {
        session.UpdatedAtUtc = DateTime.UtcNow;
        return await ReplaceItemAsync(session);
    }
}
