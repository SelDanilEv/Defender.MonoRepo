using Defender.PersonalFoodAdviser.Domain.Entities;

namespace Defender.PersonalFoodAdviser.Application.Common.Interfaces.Services;

public interface IRatingService
{
    Task<IReadOnlyList<DishRating>> GetRatingsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task SubmitRatingAsync(Guid userId, string dishName, int rating, Guid? sessionId, CancellationToken cancellationToken = default);
}
