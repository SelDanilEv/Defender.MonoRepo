using System.Diagnostics;
using Defender.PersonalFoodAdviser.Application.Common.Interfaces.Repositories;
using Defender.PersonalFoodAdviser.Application.Common.Interfaces.Services;
using Defender.PersonalFoodAdviser.Application.Kafka;
using Defender.PersonalFoodAdviser.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Defender.PersonalFoodAdviser.Application.Services;

public class MenuParsingProcessor(
    IImageBlobRepository imageBlobRepository,
    IMenuSessionRepository menuSessionRepository,
    IHuggingFaceClient huggingFaceClient,
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

        logger.LogDebug("Menu parsing session {SessionId}: current status {CurrentStatus}, storedImageRefs {StoredRefsCount}", session.Id, session.Status, session.ImageRefs.Count);

        try
        {
            var previousStatus = session.Status;
            session.Status = MenuSessionStatus.Parsing;
            await menuSessionRepository.UpdateAsync(session, cancellationToken);
            logger.LogInformation("Menu parsing session {SessionId}: status changed {PreviousStatus} -> {NewStatus}", evt.SessionId, previousStatus, session.Status);

            var imageBytes = new List<byte[]>();
            for (var i = 0; i < evt.ImageRefs.Count; i++)
            {
                var refId = evt.ImageRefs[i];
                if (!Guid.TryParse(refId, out var blobId))
                {
                    logger.LogWarning("Menu parsing session {SessionId}: imageRef at index {Index} ('{RefId}') is not a valid Guid, skipping", evt.SessionId, i, refId);
                    continue;
                }

                var blob = await imageBlobRepository.GetByIdAsync(blobId, cancellationToken);
                if (blob?.Data != null)
                {
                    imageBytes.Add(blob.Data);
                    logger.LogDebug("Menu parsing session {SessionId}: loaded blob {BlobId} at index {Index}, size {Size} bytes", evt.SessionId, blobId, i, blob.Data.Length);
                }
                else
                    logger.LogWarning("Menu parsing session {SessionId}: blob {BlobId} not found or has no data (index={Index}, refId={RefId})", evt.SessionId, blobId, i, refId);
            }

            logger.LogInformation(
                "Menu parsing session {SessionId}: resolved {ResolvedCount}/{TotalRefs} images, total size {TotalBytes} bytes",
                evt.SessionId, imageBytes.Count, evt.ImageRefs.Count, imageBytes.Sum(b => b.Length));

            if (imageBytes.Count == 0)
            {
                logger.LogWarning("Menu parsing session {SessionId}: no image data available, setting status to Failed", evt.SessionId);
                session.Status = MenuSessionStatus.Failed;
                await menuSessionRepository.UpdateAsync(session, cancellationToken);
                logger.LogInformation("Menu parsing session {SessionId}: status set to {Status}", evt.SessionId, session.Status);
                return;
            }

            logger.LogInformation("Menu parsing session {SessionId}: invoking dish extraction for {ImageCount} images", evt.SessionId, imageBytes.Count);
            var dishNames = await huggingFaceClient.ExtractDishNamesFromImagesAsync(imageBytes, cancellationToken);
            session.ParsedItems = dishNames.ToList();
            logger.LogInformation(
                "Menu parsing session {SessionId}: HuggingFace returned {DishCount} dish names. ParsedItems: [{Items}]",
                evt.SessionId, session.ParsedItems.Count, string.Join(", ", session.ParsedItems));

            session.Status = MenuSessionStatus.Review;
            await menuSessionRepository.UpdateAsync(session, cancellationToken);
            logger.LogInformation("Menu parsing session {SessionId}: status set to {Status}", evt.SessionId, session.Status);

            completed = true;
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
}
