using Defender.PersonalFoodAdvisor.Domain.Entities;

namespace Defender.PersonalFoodAdvisor.Application.Common.Interfaces.Services;

public interface IRatingService
{
    Task<IReadOnlyList<DishRating>> GetRatingsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task SubmitRatingAsync(Guid userId, string dishName, int rating, Guid? sessionId, CancellationToken cancellationToken = default);
}
