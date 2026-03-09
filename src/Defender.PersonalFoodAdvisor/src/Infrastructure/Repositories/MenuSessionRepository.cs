using Defender.Common.Configuration.Options;
using Defender.Common.DB.Model;
using Defender.Common.DB.Repositories;
using Defender.PersonalFoodAdvisor.Application.Common.Interfaces.Repositories;
using Defender.PersonalFoodAdvisor.Domain.Entities;
using Microsoft.Extensions.Options;

namespace Defender.PersonalFoodAdvisor.Infrastructure.Repositories;

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

    public async Task<IReadOnlyList<MenuSession>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var request = FindModelRequest<MenuSession>.Init(x => x.UserId, userId);
        var items = await GetItemsAsync(request);
        return items
            .OrderByDescending(session => session.UpdatedAtUtc ?? session.CreatedAtUtc)
            .ToList();
    }

    public async Task<MenuSession> UpdateAsync(MenuSession session, CancellationToken cancellationToken = default)
    {
        session.UpdatedAtUtc = DateTime.UtcNow;
        return await ReplaceItemAsync(session);
    }

    public Task DeleteAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        return RemoveItemAsync(sessionId);
    }
}
