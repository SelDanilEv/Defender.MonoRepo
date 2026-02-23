using Defender.PersonalFoodAdviser.Application.Common.Interfaces.Repositories;
using Defender.PersonalFoodAdviser.Application.Common.Interfaces.Services;
using Defender.PersonalFoodAdviser.Application.Kafka;
using Microsoft.Extensions.Logging;

namespace Defender.PersonalFoodAdviser.Application.Services;

public class RecommendationProcessor(
    IMenuSessionRepository menuSessionRepository,
    IUserPreferencesRepository userPreferencesRepository,
    IDishRatingRepository dishRatingRepository,
    IHuggingFaceClient huggingFaceClient,
    ILogger<RecommendationProcessor> logger) : IRecommendationProcessor
{
    private const int TopN = 10;

    public async Task ProcessAsync(RecommendationsRequestedEvent evt, CancellationToken cancellationToken = default)
    {
        var session = await menuSessionRepository.GetByIdAsync(evt.SessionId, cancellationToken);
        if (session == null)
        {
            logger.LogWarning("Session {SessionId} not found for recommendations", evt.SessionId);
            return;
        }
        try
        {
            var preferences = await userPreferencesRepository.GetByUserIdAsync(evt.UserId, cancellationToken);
            var likes = preferences?.Likes ?? [];
            var dislikes = preferences?.Dislikes ?? [];
            var ratings = await dishRatingRepository.GetByUserIdAsync(evt.UserId, cancellationToken);
            var ratingHistory = ratings.Select(r => (r.DishName, r.Rating)).ToList();

            var ranked = await huggingFaceClient.GetRankedRecommendationsAsync(
                evt.ConfirmedItems.ToList(),
                likes,
                dislikes,
                ratingHistory,
                evt.TrySomethingNew,
                TopN,
                cancellationToken);

            session.RankedItems = ranked.ToList();
            await menuSessionRepository.UpdateAsync(session, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Recommendations failed for session {SessionId}", evt.SessionId);
        }
    }
}
