using Defender.PersonalFoodAdviser.Application.Common.Interfaces.Repositories;
using Defender.PersonalFoodAdviser.Application.Common.Interfaces.Services;
using Defender.PersonalFoodAdviser.Domain.Entities;

namespace Defender.PersonalFoodAdviser.Application.Services;

public class ImageUploadService(
    IImageBlobRepository imageBlobRepository,
    IMenuSessionRepository menuSessionRepository) : IImageUploadService
{
    public async Task<IReadOnlyList<string>> UploadAsync(Guid sessionId, IReadOnlyList<(Stream Stream, string ContentType)> files, CancellationToken cancellationToken = default)
    {
        var refs = new List<string>();
        foreach (var (stream, contentType) in files)
        {
            await using var ms = new MemoryStream();
            await stream.CopyToAsync(ms, cancellationToken);
            var blob = new ImageBlob
            {
                SessionId = sessionId,
                Data = ms.ToArray(),
                ContentType = contentType
            };
            blob = await imageBlobRepository.SaveAsync(blob, cancellationToken);
            refs.Add(blob.Id.ToString());
        }
        var session = await menuSessionRepository.GetByIdAsync(sessionId, cancellationToken);
        if (session != null)
        {
            session.ImageRefs.AddRange(refs);
            await menuSessionRepository.UpdateAsync(session, cancellationToken);
        }
        return refs;
    }
}
