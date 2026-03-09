using Defender.PersonalFoodAdvisor.Application.Common.Interfaces.Repositories;
using Defender.PersonalFoodAdvisor.Application.Common.Interfaces.Services;
using Defender.PersonalFoodAdvisor.Application.Kafka;
using Defender.PersonalFoodAdvisor.Domain.Entities;
using Defender.PersonalFoodAdvisor.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Defender.PersonalFoodAdvisor.Application.Services;

public class MenuSessionService(
    IMenuSessionRepository repository,
    IImageBlobRepository imageBlobRepository,
    IDishRatingRepository dishRatingRepository,
    IMenuParsingOutboxRepository menuParsingOutboxRepository,
    IRecommendationsOutboxRepository recommendationsOutboxRepository,
    IMenuParsingOutboxService menuParsingOutboxService,
    IRecommendationsOutboxService recommendationsOutboxService,
    ILogger<MenuSessionService> logger) : IMenuSessionService
{
    public async Task<MenuSession> CreateAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Creating menu session for user {UserId}", userId);
        var session = new MenuSession
        {
            UserId = userId,
            Status = MenuSessionStatus.Uploaded
        };
        session = await repository.CreateAsync(session, cancellationToken);
        logger.LogInformation("Created menu session {SessionId} for user {UserId} with status {Status}", session.Id, userId, session.Status);
        return session;
    }

    public async Task<MenuSession?> GetByIdAsync(Guid sessionId, Guid userId, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Loading menu session {SessionId} for user {UserId}", sessionId, userId);
        var session = await GetOwnedSessionAsync(sessionId, userId, cancellationToken);
        if (session == null)
        {
            logger.LogWarning("Menu session {SessionId} not found or not accessible for user {UserId}", sessionId, userId);
            return null;
        }

        logger.LogDebug("Loaded menu session {SessionId} for user {UserId} with status {Status}", sessionId, userId, session.Status);
        return session;
    }

    public async Task<IReadOnlyList<MenuSession>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Loading menu sessions for user {UserId}", userId);
        var sessions = await repository.GetByUserIdAsync(userId, cancellationToken);
        logger.LogInformation("Loaded {Count} menu sessions for user {UserId}", sessions.Count, userId);
        return sessions;
    }

    public async Task<bool> DeleteAsync(Guid sessionId, Guid userId, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Deleting menu session {SessionId} for user {UserId}", sessionId, userId);
        var session = await GetOwnedSessionAsync(sessionId, userId, cancellationToken);
        if (session == null)
        {
            logger.LogWarning("Unable to delete menu session: session {SessionId} not found or not owned by user {UserId}", sessionId, userId);
            return false;
        }

        await repository.DeleteAsync(sessionId, cancellationToken);

        try
        {
            await dishRatingRepository.DeleteBySessionIdAsync(sessionId, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Menu session {SessionId} deleted but dish rating cleanup failed", sessionId);
        }

        try
        {
            await imageBlobRepository.DeleteBySessionIdAsync(sessionId, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Menu session {SessionId} deleted but image blob cleanup failed", sessionId);
        }

        try
        {
            await menuParsingOutboxRepository.DeleteBySessionIdAsync(sessionId, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Menu session {SessionId} deleted but menu parsing outbox cleanup failed", sessionId);
        }

        try
        {
            await recommendationsOutboxRepository.DeleteBySessionIdAsync(sessionId, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Menu session {SessionId} deleted but recommendations outbox cleanup failed", sessionId);
        }

        logger.LogInformation("Deleted menu session {SessionId} for user {UserId}", sessionId, userId);
        return true;
    }

    public async Task<MenuSession?> UpdateImageRefsAsync(Guid sessionId, Guid userId, IReadOnlyList<string> imageRefs, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Updating image refs for session {SessionId}, user {UserId}, incomingRefCount {IncomingRefCount}", sessionId, userId, imageRefs?.Count ?? 0);
        var session = await GetOwnedSessionAsync(sessionId, userId, cancellationToken);
        if (session == null)
        {
            logger.LogWarning("Unable to update image refs: session {SessionId} not found or not owned by user {UserId}", sessionId, userId);
            return null;
        }

        var normalizedImageRefs = NormalizeItems(imageRefs);
        var previousCount = session.ImageRefs.Count;
        var imageRefsChanged = !AreEquivalent(session.ImageRefs, normalizedImageRefs);
        session.ImageRefs = normalizedImageRefs;
        if (imageRefsChanged)
        {
            ResetDerivedStateForImages(session);
            logger.LogInformation("Image refs changed for session {SessionId}; cleared parsed, confirmed, and ranked items", sessionId);
        }

        session = await repository.UpdateAsync(session, cancellationToken);
        logger.LogInformation("Updated image refs for session {SessionId}: oldCount {OldCount}, newCount {NewCount}", sessionId, previousCount, session.ImageRefs.Count);
        return session;
    }

    public async Task<MenuSession?> ConfirmAsync(Guid sessionId, Guid userId, IReadOnlyList<string> confirmedItems, bool trySomethingNew, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Confirming menu for session {SessionId}, user {UserId}, confirmedCount {ConfirmedCount}, trySomethingNew {TrySomethingNew}",
            sessionId,
            userId,
            confirmedItems?.Count ?? 0,
            trySomethingNew);

        var session = await GetOwnedSessionAsync(sessionId, userId, cancellationToken);
        if (session == null)
        {
            logger.LogWarning("Unable to confirm menu: session {SessionId} not found or not owned by user {UserId}", sessionId, userId);
            return null;
        }

        var previousStatus = session.Status;
        session.ConfirmedItems = NormalizeItems(confirmedItems);
        session.TrySomethingNew = trySomethingNew;
        session.RankedItems = [];
        session.RecommendationWarningCode = null;
        session.RecommendationWarningMessage = null;
        session.Status = MenuSessionStatus.Confirmed;
        session = await repository.UpdateAsync(session, cancellationToken);
        logger.LogInformation(
            "Menu confirmation updated for session {SessionId}: previousStatus {PreviousStatus}, newStatus {NewStatus}, confirmedCount {ConfirmedCount}",
            sessionId,
            previousStatus,
            session.Status,
            session.ConfirmedItems.Count);
        return session;
    }

    public async Task RequestParsingAsync(Guid sessionId, Guid userId, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Requesting parsing for session {SessionId}, user {UserId}", sessionId, userId);
        var session = await GetOwnedSessionAsync(sessionId, userId, cancellationToken);
        if (session == null)
        {
            logger.LogWarning("Skipping parsing request: session {SessionId} not found or not owned by user {UserId}", sessionId, userId);
            return;
        }

        if (session.ImageRefs.Count == 0)
            logger.LogWarning("Requesting parsing for session {SessionId} with zero image references", sessionId);

        var evt = new MenuParsingRequestedEvent(session.Id, session.UserId, session.ImageRefs);
        await menuParsingOutboxService.EnqueueAsync(evt, cancellationToken);
        logger.LogInformation("Enqueued parsing request for session {SessionId}, imageRefsCount {ImageRefsCount}", session.Id, session.ImageRefs.Count);
    }

    public async Task RequestRecommendationsAsync(Guid sessionId, Guid userId, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Requesting recommendations for session {SessionId}, user {UserId}", sessionId, userId);
        var session = await GetOwnedSessionAsync(sessionId, userId, cancellationToken);
        if (session == null)
        {
            logger.LogWarning("Skipping recommendations request: session {SessionId} not found or not owned by user {UserId}", sessionId, userId);
            return;
        }

        if (session.ConfirmedItems.Count == 0)
            logger.LogWarning("Requesting recommendations for session {SessionId} with zero confirmed items", sessionId);

        session.RecommendationWarningCode = null;
        session.RecommendationWarningMessage = null;
        session = await repository.UpdateAsync(session, cancellationToken);

        var evt = new RecommendationsRequestedEvent(
            session.Id,
            session.UserId,
            session.ConfirmedItems,
            session.TrySomethingNew);
        await recommendationsOutboxService.EnqueueAsync(evt, cancellationToken);
        logger.LogInformation(
            "Enqueued recommendations request for session {SessionId}, confirmedItemsCount {ConfirmedCount}, trySomethingNew {TrySomethingNew}",
            session.Id,
            session.ConfirmedItems.Count,
            session.TrySomethingNew);
    }

    public async Task<IReadOnlyList<string>?> GetRecommendationsAsync(Guid sessionId, Guid userId, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Loading recommendations for session {SessionId}, user {UserId}", sessionId, userId);
        var session = await GetOwnedSessionAsync(sessionId, userId, cancellationToken);
        if (session == null)
        {
            logger.LogWarning("Unable to load recommendations: session {SessionId} not found or not owned by user {UserId}", sessionId, userId);
            return null;
        }

        logger.LogInformation("Loaded recommendations for session {SessionId}: count {Count}, status {Status}", sessionId, session.RankedItems.Count, session.Status);
        return session.RankedItems;
    }

    private async Task<MenuSession?> GetOwnedSessionAsync(Guid sessionId, Guid userId, CancellationToken cancellationToken)
    {
        var session = await repository.GetByIdAsync(sessionId, cancellationToken);
        if (session == null)
        {
            logger.LogDebug("Session {SessionId} not found in repository", sessionId);
            return null;
        }

        if (session.UserId != userId)
        {
            logger.LogWarning("Session ownership mismatch: session {SessionId}, owner {OwnerUserId}, requestedBy {RequestedByUserId}", sessionId, session.UserId, userId);
            return null;
        }

        return session;
    }

    private static void ResetDerivedStateForImages(MenuSession session)
    {
        session.ParsedItems = [];
        session.ConfirmedItems = [];
        session.RankedItems = [];
        session.TrySomethingNew = false;
        session.RecommendationWarningCode = null;
        session.RecommendationWarningMessage = null;
        session.Status = MenuSessionStatus.Uploaded;
    }

    private static List<string> NormalizeItems(IReadOnlyList<string>? items)
    {
        if (items == null || items.Count == 0)
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

    private static bool AreEquivalent(IReadOnlyList<string> left, IReadOnlyList<string> right)
    {
        if (left.Count != right.Count)
            return false;

        for (var i = 0; i < left.Count; i++)
        {
            if (!string.Equals(left[i], right[i], StringComparison.OrdinalIgnoreCase))
                return false;
        }

        return true;
    }
}
