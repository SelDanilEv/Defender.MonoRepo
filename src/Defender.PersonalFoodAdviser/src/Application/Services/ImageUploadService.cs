using Defender.PersonalFoodAdviser.Application.Common.Interfaces.Repositories;
using Defender.PersonalFoodAdviser.Application.Common.Interfaces.Services;
using Defender.PersonalFoodAdviser.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Defender.PersonalFoodAdviser.Application.Services;

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
                ContentType = contentType
            };
            blob = await imageBlobRepository.SaveAsync(blob, cancellationToken);
            refs.Add(blob.Id.ToString());
            logger.LogDebug("Saved image blob for session {SessionId}: index {Index}, blobId {BlobId}, contentType {ContentType}, bytes {Bytes}", sessionId, i, blob.Id, contentType, bytes.Length);
        }

        var session = await menuSessionRepository.GetByIdAsync(sessionId, cancellationToken);
        if (session != null)
        {
            var previousCount = session.ImageRefs.Count;
            session.ImageRefs.AddRange(refs);
            await menuSessionRepository.UpdateAsync(session, cancellationToken);
            logger.LogInformation("Updated session image refs after upload: session {SessionId}, oldCount {OldCount}, newCount {NewCount}", sessionId, previousCount, session.ImageRefs.Count);
        }
        else
        {
            logger.LogWarning("Uploaded image blobs for session {SessionId}, but session was not found for image refs update", sessionId);
        }

        logger.LogInformation("Image upload completed for session {SessionId}: uploadedRefsCount {RefsCount}", sessionId, refs.Count);
        return refs;
    }
}
