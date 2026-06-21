using Defender.Common.Configuration.Options;
using Defender.Common.DB.Repositories;
using Defender.HealthCareService.Application.Common.Interfaces.Repositories;
using Defender.HealthCareService.Domain.Entities;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Defender.HealthCareService.Infrastructure.Repositories;

public class HealthEventRepository : BaseMongoRepository<HealthEvent>, IHealthEventRepository
{
    public HealthEventRepository(IOptions<MongoDbOptions> mongoOption) : base(mongoOption.Value)
    {
    }

    public async Task<HealthEvent> GetHealthEventByIdAsync(Guid id)
    {
        return await GetItemAsync(id);
    }

    public async Task<IReadOnlyList<HealthEvent>> GetHealthEventsAsync(Guid userId, DateTimeOffset? from, DateTimeOffset? to)
    {
        var filter = Builders<HealthEvent>.Filter.Eq(e => e.UserId, userId);

        if (from != null)
        {
            filter &= Builders<HealthEvent>.Filter.Gte(e => e.StartedAt, from.Value);
        }

        if (to != null)
        {
            filter &= Builders<HealthEvent>.Filter.Lte(e => e.StartedAt, to.Value);
        }

        return await _mongoCollection
            .Find(filter)
            .SortBy(e => e.StartedAt)
            .ToListAsync();
    }

    public async Task<HealthEvent?> GetHealthEventByIdAsync(Guid userId, Guid id)
    {
        var filter = Builders<HealthEvent>.Filter.Eq(e => e.Id, id)
            & Builders<HealthEvent>.Filter.Eq(e => e.UserId, userId);

        return await _mongoCollection.Find(filter).FirstOrDefaultAsync();
    }

    public async Task<HealthEvent> AddHealthEventAsync(HealthEvent healthEvent)
    {
        return await AddItemAsync(healthEvent);
    }

    public async Task<HealthEvent> UpdateHealthEventAsync(HealthEvent healthEvent)
    {
        var filter = Builders<HealthEvent>.Filter.Eq(e => e.Id, healthEvent.Id)
            & Builders<HealthEvent>.Filter.Eq(e => e.UserId, healthEvent.UserId);

        return await ReplaceItemAsync(healthEvent, filter);
    }

    public async Task<bool> DeleteHealthEventAsync(Guid userId, Guid id)
    {
        var filter = Builders<HealthEvent>.Filter.Eq(e => e.Id, id)
            & Builders<HealthEvent>.Filter.Eq(e => e.UserId, userId);
        var result = await _mongoCollection.DeleteOneAsync(filter);

        return result.DeletedCount > 0;
    }
}

public class HealthChartShareRepository : BaseMongoRepository<HealthChartShare>, IHealthChartShareRepository
{
    public HealthChartShareRepository(IOptions<MongoDbOptions> mongoOption) : base(mongoOption.Value)
    {
    }

    public async Task<HealthChartShare> AddHealthChartShareAsync(HealthChartShare share)
    {
        return await AddItemAsync(share);
    }

    public async Task<HealthChartShare> UpdateHealthChartShareAsync(HealthChartShare share)
    {
        return await ReplaceItemAsync(share);
    }

    public async Task<HealthChartShare?> GetHealthChartShareByTokenAsync(string token)
    {
        return await _mongoCollection
            .Find(share => share.Token == token)
            .FirstOrDefaultAsync();
    }

    public async Task<HealthChartShare?> GetHealthChartShareByUserIdAsync(Guid userId)
    {
        return await _mongoCollection
            .Find(share => share.UserId == userId)
            .SortBy(share => share.CreatedAtUtc)
            .FirstOrDefaultAsync();
    }

    public async Task DisableOtherHealthChartSharesAsync(Guid userId, Guid activeShareId)
    {
        var filter = Builders<HealthChartShare>.Filter.Eq(share => share.UserId, userId)
            & Builders<HealthChartShare>.Filter.Ne(share => share.Id, activeShareId);
        var update = Builders<HealthChartShare>.Update.Set(share => share.IsEnabled, false);

        await _mongoCollection.UpdateManyAsync(filter, update);
    }
}
