using Defender.PersonalFoodAdviser.Application.Common.Interfaces.Repositories;
using Defender.PersonalFoodAdviser.Application.Common.Interfaces.Services;
using Defender.PersonalFoodAdviser.Domain.Entities;

namespace Defender.PersonalFoodAdviser.Application.Services;

public class RatingService(IDishRatingRepository repository) : IRatingService
{
    public async Task SubmitRatingAsync(Guid userId, string dishName, int rating, Guid? sessionId, CancellationToken cancellationToken = default)
    {
        var entity = new DishRating
        {
            UserId = userId,
            DishName = dishName ?? string.Empty,
            Rating = Math.Clamp(rating, 1, 5),
            SessionId = sessionId
        };
        await repository.CreateAsync(entity, cancellationToken);
    }
}
