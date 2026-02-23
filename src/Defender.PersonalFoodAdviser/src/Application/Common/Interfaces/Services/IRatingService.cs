namespace Defender.PersonalFoodAdviser.Application.Common.Interfaces.Services;

public interface IRatingService
{
    Task SubmitRatingAsync(Guid userId, string dishName, int rating, Guid? sessionId, CancellationToken cancellationToken = default);
}
