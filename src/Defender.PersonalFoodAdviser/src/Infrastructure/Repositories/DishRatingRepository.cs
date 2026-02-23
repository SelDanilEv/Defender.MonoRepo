using Defender.Common.Configuration.Options;
using Defender.Common.DB.Model;
using Defender.Common.DB.Repositories;
using Defender.PersonalFoodAdviser.Application.Common.Interfaces.Repositories;
using Defender.PersonalFoodAdviser.Domain.Entities;
using Microsoft.Extensions.Options;

namespace Defender.PersonalFoodAdviser.Infrastructure.Repositories;

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

    public async Task<IReadOnlyList<DishRating>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var request = FindModelRequest<DishRating>.Init(x => x.UserId, userId);
        var items = await GetItemsAsync(request);
        return (IReadOnlyList<DishRating>)items;
    }
}
