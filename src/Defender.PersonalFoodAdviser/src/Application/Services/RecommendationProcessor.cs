using System.Diagnostics;
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
        var stopwatch = Stopwatch.StartNew();
        var completed = false;

        logger.LogInformation(
            "Recommendations processing started for session {SessionId}, user {UserId}, confirmedItemsCount {ConfirmedItemsCount}, trySomethingNew {TrySomethingNew}",
            evt.SessionId,
            evt.UserId,
            evt.ConfirmedItems.Count,
            evt.TrySomethingNew);

        var session = await menuSessionRepository.GetByIdAsync(evt.SessionId, cancellationToken);
        if (session == null)
        {
            logger.LogWarning("Session {SessionId} not found for recommendations (user {UserId})", evt.SessionId, evt.UserId);
            return;
        }

        try
        {
            var preferences = await userPreferencesRepository.GetByUserIdAsync(evt.UserId, cancellationToken);
            var likes = preferences?.Likes ?? [];
            var dislikes = preferences?.Dislikes ?? [];
            var ratings = await dishRatingRepository.GetByUserIdAsync(evt.UserId, cancellationToken);
            var ratingHistory = ratings.Select(r => (r.DishName, r.Rating)).ToList();

            logger.LogInformation(
                "Recommendations context for session {SessionId}: likes {LikesCount}, dislikes {DislikesCount}, ratingHistory {RatingHistoryCount}",
                evt.SessionId,
                likes.Count,
                dislikes.Count,
                ratingHistory.Count);

            if (evt.ConfirmedItems.Count == 0)
                logger.LogWarning("Recommendations requested with zero confirmed items for session {SessionId}", evt.SessionId);

            logger.LogInformation(
                "Calling recommendation model for session {SessionId}: candidates {CandidateCount}, topN {TopN}",
                evt.SessionId,
                evt.ConfirmedItems.Count,
                TopN);

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

            logger.LogInformation(
                "Recommendations processing completed for session {SessionId}: rankedItemsCount {RankedCount}",
                evt.SessionId,
                session.RankedItems.Count);

            completed = true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Recommendations failed for session {SessionId}", evt.SessionId);
        }
        finally
        {
            stopwatch.Stop();
            logger.LogInformation(
                "Recommendations processing finished for session {SessionId}: completed {Completed}, elapsedMs {ElapsedMs}",
                evt.SessionId,
                completed,
                stopwatch.ElapsedMilliseconds);
        }
    }
}
