using Defender.PersonalFoodAdviser.Domain.Entities;

namespace Defender.PersonalFoodAdviser.Application.Common.Interfaces.Repositories;

public interface IDishRatingRepository
{
    Task<DishRating> CreateAsync(DishRating rating, CancellationToken cancellationToken = default);
    Task<DishRating> UpdateAsync(DishRating rating, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DishRating>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task DeleteBySessionIdAsync(Guid sessionId, CancellationToken cancellationToken = default);
}
