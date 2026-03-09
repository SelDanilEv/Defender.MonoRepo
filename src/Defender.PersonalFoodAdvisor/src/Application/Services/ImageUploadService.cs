using Defender.PersonalFoodAdvisor.Application.Common.Interfaces.Repositories;
using Defender.PersonalFoodAdvisor.Application.Common.Interfaces.Services;
using Defender.PersonalFoodAdvisor.Application.Common.Helpers;
using Defender.PersonalFoodAdvisor.Domain.Entities;
using Defender.PersonalFoodAdvisor.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Defender.PersonalFoodAdvisor.Application.Services;

public class ImageUploadService(
    IImageBlobRepository imageBlobRepository,
    IMenuSessionRepository menuSessionRepository,
    ILogger<ImageUploadService> logger) : IImageUploadService
{
    public async Task<IReadOnlyList<string>> UploadAsync(Guid sessionId, IReadOnlyList<(Stream Stream, string ContentType)> files, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Uploading images for session {SessionId}: fileCount {FileCount}", sessionId, files.Count);
        var refs = new List<string>();
        for (var i = 0; i < files.Count; i++)
        {
            var (stream, contentType) = files[i];
            await using var ms = new MemoryStream();
            await stream.CopyToAsync(ms, cancellationToken);
            var bytes = ms.ToArray();
            var blob = new ImageBlob
            {
                SessionId = sessionId,
                Data = bytes,
                ContentType = contentType,
                ImageHash = ImageHashHelper.ComputeSha256(bytes)
            };
            blob = await imageBlobRepository.SaveAsync(blob, cancellationToken);
            refs.Add(blob.Id.ToString());
            logger.LogDebug("Saved image blob for session {SessionId}: index {Index}, blobId {BlobId}, contentType {ContentType}, bytes {Bytes}", sessionId, i, blob.Id, contentType, bytes.Length);
        }

        var session = await menuSessionRepository.GetByIdAsync(sessionId, cancellationToken);
        if (session != null)
        {
            var previousCount = session.ImageRefs.Count;
            session.ImageRefs = MergeImageRefs(session.ImageRefs, refs);
            ResetDerivedStateForImages(session);
            await menuSessionRepository.UpdateAsync(session, cancellationToken);
            logger.LogInformation(
                "Updated session image refs after upload: session {SessionId}, oldCount {OldCount}, newCount {NewCount}; cleared parsed, confirmed, and ranked items",
                sessionId,
                previousCount,
                session.ImageRefs.Count);
        }
        else
        {
            logger.LogWarning("Uploaded image blobs for session {SessionId}, but session was not found for image refs update", sessionId);
        }

        logger.LogInformation("Image upload completed for session {SessionId}: uploadedRefsCount {RefsCount}", sessionId, refs.Count);
        return refs;
    }

    private static List<string> MergeImageRefs(IReadOnlyList<string> existingRefs, IReadOnlyList<string> newRefs)
    {
        var merged = new List<string>(existingRefs.Count + newRefs.Count);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var imageRef in existingRefs.Concat(newRefs))
        {
            var value = imageRef?.Trim();
            if (string.IsNullOrWhiteSpace(value) || !seen.Add(value))
                continue;

            merged.Add(value);
        }

        return merged;
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
}
