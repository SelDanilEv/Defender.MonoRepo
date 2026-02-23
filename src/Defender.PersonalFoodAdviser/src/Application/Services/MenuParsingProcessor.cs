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
        logger.LogInformation(
            "Menu parsing started for session {SessionId}, ImageRefs count: {ImageRefsCount}, ImageRefs: {ImageRefs}",
            evt.SessionId, evt.ImageRefs.Count, string.Join(", ", evt.ImageRefs));

        var session = await menuSessionRepository.GetByIdAsync(evt.SessionId, cancellationToken);
        if (session == null)
        {
            logger.LogWarning("Session {SessionId} not found for menu parsing", evt.SessionId);
            return;
        }
        try
        {
            session.Status = MenuSessionStatus.Parsing;
            await menuSessionRepository.UpdateAsync(session, cancellationToken);

            var imageBytes = new List<byte[]>();
            foreach (var refId in evt.ImageRefs)
            {
                if (!Guid.TryParse(refId, out var blobId))
                {
                    logger.LogWarning("Menu parsing session {SessionId}: ImageRef '{RefId}' is not a valid Guid, skipping", evt.SessionId, refId);
                    continue;
                }
                var blob = await imageBlobRepository.GetByIdAsync(blobId, cancellationToken);
                if (blob?.Data != null)
                {
                    imageBytes.Add(blob.Data);
                    logger.LogDebug("Menu parsing session {SessionId}: loaded blob {BlobId}, size {Size} bytes", evt.SessionId, blobId, blob.Data.Length);
                }
                else
                    logger.LogWarning("Menu parsing session {SessionId}: blob {BlobId} not found or has no data (refId={RefId})", evt.SessionId, blobId, refId);
            }

            logger.LogInformation(
                "Menu parsing session {SessionId}: resolved {ResolvedCount}/{TotalRefs} images, total size {TotalBytes} bytes",
                evt.SessionId, imageBytes.Count, evt.ImageRefs.Count, imageBytes.Sum(b => b.Length));

            if (imageBytes.Count == 0)
            {
                logger.LogWarning("Menu parsing session {SessionId}: no image data available, setting status to Failed", evt.SessionId);
                session.Status = MenuSessionStatus.Failed;
                await menuSessionRepository.UpdateAsync(session, cancellationToken);
                return;
            }

            var dishNames = await huggingFaceClient.ExtractDishNamesFromImagesAsync(imageBytes, cancellationToken);
            session.ParsedItems = dishNames.ToList();
            logger.LogInformation(
                "Menu parsing session {SessionId}: HuggingFace returned {DishCount} dish names. ParsedItems: [{Items}]",
                evt.SessionId, session.ParsedItems.Count, string.Join(", ", session.ParsedItems));

            session.Status = MenuSessionStatus.Review;
            await menuSessionRepository.UpdateAsync(session, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Menu parsing failed for session {SessionId}", evt.SessionId);
            session.Status = MenuSessionStatus.Failed;
            await menuSessionRepository.UpdateAsync(session, cancellationToken);
        }
    }
}
