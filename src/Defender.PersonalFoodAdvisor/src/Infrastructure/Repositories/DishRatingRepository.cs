using Defender.Common.Configuration.Options;
using Defender.Common.DB.Model;
using Defender.Common.DB.Repositories;
using Defender.PersonalFoodAdvisor.Application.Common.Interfaces.Repositories;
using Defender.PersonalFoodAdvisor.Domain.Entities;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Defender.PersonalFoodAdvisor.Infrastructure.Repositories;

public class DishRatingRepository : BaseMongoRepository<DishRating>, IDishRatingRepository
{
    public DishRatingRepository(IOptions<MongoDbOptions> mongoOption)
        : base(mongoOption.Value, "DishRating")
    {
    }

    public async Task<DishRating> CreateAsync(DishRating rating, CancellationToken cancellationToken = default)
    {
        if (rating.Id == Guid.Empty)
            rating.Id = Guid.NewGuid();
        rating.CreatedAtUtc = DateTime.UtcNow;
        return await AddItemAsync(rating);
    }

    public async Task<DishRating> UpdateAsync(DishRating rating, CancellationToken cancellationToken = default)
    {
        rating.UpdatedAtUtc = DateTime.UtcNow;
        return await ReplaceItemAsync(rating);
    }

    public async Task<IReadOnlyList<DishRating>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var request = FindModelRequest<DishRating>.Init(x => x.UserId, userId);
        var items = await GetItemsAsync(request);
        return items
            .GroupBy(item => NormalizeDishName(item.DishName), StringComparer.OrdinalIgnoreCase)
            .Select(group => group
                .OrderByDescending(item => item.UpdatedAtUtc ?? item.CreatedAtUtc)
                .First())
            .OrderByDescending(item => item.UpdatedAtUtc ?? item.CreatedAtUtc)
            .ToList();
    }

    public async Task DeleteBySessionIdAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        await _mongoCollection.DeleteManyAsync(
            Builders<DishRating>.Filter.Eq(rating => rating.SessionId, sessionId),
            cancellationToken);
    }

    private static string NormalizeDishName(string? dishName)
        => dishName?.Trim() ?? string.Empty;
}
