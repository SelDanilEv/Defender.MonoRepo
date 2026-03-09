using System.Diagnostics;
using System.Net;
using Defender.PersonalFoodAdvisor.Application.Common.Interfaces.Repositories;
using Defender.PersonalFoodAdvisor.Application.Common.Interfaces.Services;
using Defender.PersonalFoodAdvisor.Application.Kafka;
using Microsoft.Extensions.Logging;

namespace Defender.PersonalFoodAdvisor.Application.Services;

public class RecommendationProcessor(
    IMenuSessionRepository menuSessionRepository,
    IUserPreferencesRepository userPreferencesRepository,
    IDishRatingRepository dishRatingRepository,
    IMenuIntelligenceClient menuIntelligenceClient,
    IRecommendationsOutboxService recommendationsOutboxService,
    ILogger<RecommendationProcessor> logger) : IRecommendationProcessor
{
    private const int TopN = 10;

    public async Task ProcessAsync(RecommendationsRequestedEvent evt, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var completed = false;

        logger.LogInformation(
            "Recommendations processing started for session {SessionId}, user {UserId}, confirmedItemsCount {ConfirmedItemsCount}, trySomethingNew {TrySomethingNew}, attempt {Attempt}",
            evt.SessionId,
            evt.UserId,
            evt.ConfirmedItems.Count,
            evt.TrySomethingNew,
            evt.Attempt);

        var session = await menuSessionRepository.GetByIdAsync(evt.SessionId, cancellationToken);
        if (session == null)
        {
            logger.LogWarning("Session {SessionId} not found for recommendations (user {UserId})", evt.SessionId, evt.UserId);
            return;
        }

        if (session.UserId != evt.UserId)
        {
            logger.LogWarning(
                "Skipping recommendations for session {SessionId}: event user {EventUserId} does not match session owner {SessionUserId}",
                evt.SessionId,
                evt.UserId,
                session.UserId);
            return;
        }

        try
        {
            var preferences = await userPreferencesRepository.GetByUserIdAsync(evt.UserId, cancellationToken);
            var likes = preferences?.Likes ?? [];
            var dislikes = preferences?.Dislikes ?? [];
            var ratings = await dishRatingRepository.GetByUserIdAsync(evt.UserId, cancellationToken);
            var ratingHistory = ratings.Select(r => (r.DishName, r.Rating)).ToList();
            var confirmedItems = NormalizeItems(session.ConfirmedItems);
            var trySomethingNew = session.TrySomethingNew;

            logger.LogInformation(
                "Recommendations context for session {SessionId}: likes {LikesCount}, dislikes {DislikesCount}, ratingHistory {RatingHistoryCount}",
                evt.SessionId,
                likes.Count,
                dislikes.Count,
                ratingHistory.Count);

            if (confirmedItems.Count == 0)
            {
                logger.LogWarning("Recommendations requested with zero confirmed items for session {SessionId}", evt.SessionId);
                session.RankedItems = [];
                ClearRecommendationWarning(session);
                await menuSessionRepository.UpdateAsync(session, cancellationToken);
                return;
            }

            logger.LogInformation(
                "Calling recommendation model for session {SessionId}: candidates {CandidateCount}, topN {TopN}",
                evt.SessionId,
                confirmedItems.Count,
                TopN);

            var ranked = await menuIntelligenceClient.GetRankedRecommendationsAsync(
                confirmedItems,
                likes,
                dislikes,
                ratingHistory,
                trySomethingNew,
                TopN,
                cancellationToken);

            session.RankedItems = NormalizeItems(ranked);
            ClearRecommendationWarning(session);
            await menuSessionRepository.UpdateAsync(session, cancellationToken);

            logger.LogInformation(
                "Recommendations processing completed for session {SessionId}: rankedItemsCount {RankedCount}",
                evt.SessionId,
                session.RankedItems.Count);

            completed = true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("Recommendations canceled for session {SessionId}", evt.SessionId);
            throw;
        }
        catch (HttpRequestException ex) when (IsManualRefreshWarningCandidate(ex.StatusCode))
        {
            var retryScheduled = await recommendationsOutboxService.ScheduleRetryAsync(evt, ex.StatusCode, cancellationToken);
            if (retryScheduled)
            {
                logger.LogWarning(
                    ex,
                    "Recommendations temporarily unavailable for session {SessionId}. Provider returned {StatusCode}; automatic retry scheduled",
                    evt.SessionId,
                    ex.StatusCode);
                return;
            }

            logger.LogWarning(
                ex,
                "Recommendations temporarily unavailable for session {SessionId}. Provider returned {StatusCode}; manual refresh will be required",
                evt.SessionId,
                ex.StatusCode);

            session.RankedItems = [];
            session.RecommendationWarningCode = RecommendationWarningCode.ProviderBusy;
            session.RecommendationWarningMessage = BuildRecommendationWarningMessage(ex.StatusCode);
            await menuSessionRepository.UpdateAsync(session, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Recommendations failed for session {SessionId}", evt.SessionId);
        }
        finally
        {
            stopwatch.Stop();
            logger.LogInformation(
                "Recommendations processing finished for session {SessionId}: completed {Completed}, attempt {Attempt}, elapsedMs {ElapsedMs}",
                evt.SessionId,
                completed,
                evt.Attempt,
                stopwatch.ElapsedMilliseconds);
        }
    }

    private static List<string> NormalizeItems(IReadOnlyList<string> items)
    {
        if (items.Count == 0)
            return [];

        var normalized = new List<string>(items.Count);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in items)
        {
            var value = item?.Trim();
            if (string.IsNullOrWhiteSpace(value) || !seen.Add(value))
                continue;

            normalized.Add(value);
        }

        return normalized;
    }

    private static bool IsManualRefreshWarningCandidate(HttpStatusCode? statusCode)
        => statusCode is HttpStatusCode.TooManyRequests or HttpStatusCode.ServiceUnavailable;

    private static string BuildRecommendationWarningMessage(HttpStatusCode? statusCode)
        => statusCode switch
        {
            HttpStatusCode.TooManyRequests => "Recommendation provider rate limit reached. Click Refresh to retry manually.",
            HttpStatusCode.ServiceUnavailable => "Recommendation provider is temporarily unavailable. Click Refresh to retry manually.",
            _ => "Recommendations are temporarily unavailable. Click Refresh to retry manually."
        };

    private static void ClearRecommendationWarning(Domain.Entities.MenuSession session)
    {
        session.RecommendationWarningCode = null;
        session.RecommendationWarningMessage = null;
    }

    private static class RecommendationWarningCode
    {
        public const string ProviderBusy = "ProviderBusy";
    }
}
