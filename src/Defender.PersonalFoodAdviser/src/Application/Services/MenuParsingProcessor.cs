using System.Diagnostics;
using Defender.PersonalFoodAdviser.Application.Common.Helpers;
using Defender.PersonalFoodAdviser.Application.Common.Interfaces.Repositories;
using Defender.PersonalFoodAdviser.Application.Common.Interfaces.Services;
using Defender.PersonalFoodAdviser.Application.Kafka;
using Defender.PersonalFoodAdviser.Domain.Entities;
using Defender.PersonalFoodAdviser.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Defender.PersonalFoodAdviser.Application.Services;

public class MenuParsingProcessor(
    IImageBlobRepository imageBlobRepository,
    IMenuSessionRepository menuSessionRepository,
    IMenuIntelligenceClient menuIntelligenceClient,
    ILogger<MenuParsingProcessor> logger) : IMenuParsingProcessor
{
    public async Task ProcessAsync(MenuParsingRequestedEvent evt, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var completed = false;

        logger.LogInformation(
            "Menu parsing started for session {SessionId}, user {UserId}, imageRefsCount {ImageRefsCount}, imageRefs {ImageRefs}",
            evt.SessionId, evt.UserId, evt.ImageRefs.Count, string.Join(", ", evt.ImageRefs));

        var session = await menuSessionRepository.GetByIdAsync(evt.SessionId, cancellationToken);
        if (session == null)
        {
            logger.LogWarning("Session {SessionId} not found for menu parsing (user {UserId})", evt.SessionId, evt.UserId);
            return;
        }

        if (session.UserId != evt.UserId)
        {
            logger.LogWarning(
                "Skipping menu parsing for session {SessionId}: event user {EventUserId} does not match session owner {SessionUserId}",
                evt.SessionId,
                evt.UserId,
                session.UserId);
            return;
        }

        var currentImageRefs = NormalizeItems(session.ImageRefs);
        if (!AreEquivalent(currentImageRefs, evt.ImageRefs))
        {
            logger.LogInformation(
                "Menu parsing session {SessionId}: event image refs do not match persisted refs; using persisted refs. EventCount {EventCount}, PersistedCount {PersistedCount}",
                evt.SessionId,
                evt.ImageRefs.Count,
                currentImageRefs.Count);
        }

        if ((session.Status is MenuSessionStatus.Review or MenuSessionStatus.Confirmed) && AreEquivalent(currentImageRefs, evt.ImageRefs))
        {
            logger.LogInformation(
                "Skipping menu parsing for session {SessionId}: session is already in status {Status} for the current image set",
                evt.SessionId,
                session.Status);
            return;
        }

        logger.LogDebug("Menu parsing session {SessionId}: current status {CurrentStatus}, storedImageRefs {StoredRefsCount}", session.Id, session.Status, session.ImageRefs.Count);

        try
        {
            var previousStatus = session.Status;
            session.ParsedItems = [];
            session.ConfirmedItems = [];
            session.RankedItems = [];
            session.TrySomethingNew = false;
            session.Status = MenuSessionStatus.Parsing;
            await menuSessionRepository.UpdateAsync(session, cancellationToken);
            logger.LogInformation("Menu parsing session {SessionId}: status changed {PreviousStatus} -> {NewStatus}", evt.SessionId, previousStatus, session.Status);

            var imageBlobs = await LoadImageBlobsAsync(evt, currentImageRefs, cancellationToken);

            logger.LogInformation(
                "Menu parsing session {SessionId}: resolved {ResolvedCount}/{TotalRefs} images, total size {TotalBytes} bytes",
                evt.SessionId, imageBlobs.Count, currentImageRefs.Count, imageBlobs.Sum(blob => blob.Data.Length));

            if (imageBlobs.Count == 0)
            {
                logger.LogWarning("Menu parsing session {SessionId}: no image data available, setting status to Failed", evt.SessionId);
                session.Status = MenuSessionStatus.Failed;
                await menuSessionRepository.UpdateAsync(session, cancellationToken);
                logger.LogInformation("Menu parsing session {SessionId}: status set to {Status}", evt.SessionId, session.Status);
                return;
            }

            var reusedConfirmedItems = await TryGetReusableConfirmedItemsAsync(session, imageBlobs, cancellationToken);
            if (reusedConfirmedItems.Count > 0)
            {
                session.ParsedItems = [.. reusedConfirmedItems];
                session.ConfirmedItems = [.. reusedConfirmedItems];
                session.Status = MenuSessionStatus.Review;
                await menuSessionRepository.UpdateAsync(session, cancellationToken);
                logger.LogInformation(
                    "Menu parsing session {SessionId}: reused {ItemCount} confirmed items from a matching prior session and skipped AI extraction",
                    evt.SessionId,
                    reusedConfirmedItems.Count);

                completed = true;
                return;
            }

            var imageBytes = imageBlobs.Select(blob => blob.Data).ToList();
            logger.LogInformation("Menu parsing session {SessionId}: invoking dish extraction for {ImageCount} images", evt.SessionId, imageBytes.Count);
            var dishNames = await menuIntelligenceClient.ExtractDishNamesFromImagesAsync(imageBytes, cancellationToken);
            session.ParsedItems = NormalizeItems(dishNames);
            logger.LogInformation(
                "Menu parsing session {SessionId}: menu intelligence client returned {DishCount} dish names. ParsedItems: [{Items}]",
                evt.SessionId, session.ParsedItems.Count, string.Join(", ", session.ParsedItems));

            session.Status = MenuSessionStatus.Review;
            await menuSessionRepository.UpdateAsync(session, cancellationToken);
            logger.LogInformation("Menu parsing session {SessionId}: status set to {Status}", evt.SessionId, session.Status);

            completed = true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("Menu parsing canceled for session {SessionId}", evt.SessionId);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Menu parsing failed for session {SessionId}", evt.SessionId);
            session.Status = MenuSessionStatus.Failed;
            await menuSessionRepository.UpdateAsync(session, cancellationToken);
            logger.LogInformation("Menu parsing session {SessionId}: status set to {Status} after failure", evt.SessionId, session.Status);
        }
        finally
        {
            stopwatch.Stop();
            logger.LogInformation(
                "Menu parsing finished for session {SessionId}: completed {Completed}, finalStatus {FinalStatus}, elapsedMs {ElapsedMs}",
                evt.SessionId,
                completed,
                session.Status,
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

    private async Task<List<ImageBlob>> LoadImageBlobsAsync(
        MenuParsingRequestedEvent evt,
        IReadOnlyList<string> imageRefs,
        CancellationToken cancellationToken)
    {
        var imageBlobs = new List<ImageBlob>();

        for (var i = 0; i < imageRefs.Count; i++)
        {
            var refId = imageRefs[i];
            if (!Guid.TryParse(refId, out var blobId))
            {
                logger.LogWarning("Menu parsing session {SessionId}: imageRef at index {Index} ('{RefId}') is not a valid Guid, skipping", evt.SessionId, i, refId);
                continue;
            }

            var blob = await imageBlobRepository.GetByIdAsync(blobId, cancellationToken);
            if (blob?.Data is { Length: > 0 })
            {
                imageBlobs.Add(blob);
                logger.LogDebug("Menu parsing session {SessionId}: loaded blob {BlobId} at index {Index}, size {Size} bytes", evt.SessionId, blobId, i, blob.Data.Length);
                continue;
            }

            logger.LogWarning("Menu parsing session {SessionId}: blob {BlobId} not found or has no data (index={Index}, refId={RefId})", evt.SessionId, blobId, i, refId);
        }

        return imageBlobs;
    }

    private async Task<List<string>> TryGetReusableConfirmedItemsAsync(
        MenuSession currentSession,
        IReadOnlyList<ImageBlob> imageBlobs,
        CancellationToken cancellationToken)
    {
        var imageHashes = GetImageHashes(imageBlobs);
        if (imageHashes.Count != imageBlobs.Count)
        {
            logger.LogInformation(
                "Menu parsing session {SessionId}: skipping session reuse because only {HashCount}/{BlobCount} images could be fingerprinted",
                currentSession.Id,
                imageHashes.Count,
                imageBlobs.Count);
            return [];
        }

        var matchingSessionIds = await imageBlobRepository.FindSessionIdsByExactImageHashesAsync(
                imageHashes,
                currentSession.Id,
                cancellationToken)
            ?? [];

        if (matchingSessionIds.Count == 0)
            return [];

        var reusableSessions = new List<MenuSession>();
        foreach (var matchingSessionId in matchingSessionIds)
        {
            var matchingSession = await menuSessionRepository.GetByIdAsync(matchingSessionId, cancellationToken);
            if (matchingSession == null)
                continue;

            var confirmedItems = NormalizeItems(matchingSession.ConfirmedItems);
            if (confirmedItems.Count == 0)
                continue;

            matchingSession.ConfirmedItems = confirmedItems;
            reusableSessions.Add(matchingSession);
        }

        if (reusableSessions.Count == 0)
            return [];

        var reusableSession = reusableSessions
            .OrderByDescending(session => session.UpdatedAtUtc ?? session.CreatedAtUtc)
            .First();

        return [.. reusableSession.ConfirmedItems];
    }

    private static List<string> GetImageHashes(IReadOnlyList<ImageBlob> imageBlobs)
    {
        var hashes = new List<string>(imageBlobs.Count);

        foreach (var imageBlob in imageBlobs)
        {
            var value = imageBlob.ImageHash;
            if (string.IsNullOrWhiteSpace(value) && imageBlob.Data.Length > 0)
                value = ImageHashHelper.ComputeSha256(imageBlob.Data);

            value = value?.Trim();
            if (string.IsNullOrWhiteSpace(value))
                continue;

            hashes.Add(value.ToUpperInvariant());
        }

        return hashes;
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
