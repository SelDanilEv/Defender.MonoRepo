namespace Defender.PersonalFoodAdviser.Application.Common.Interfaces.Services;

public interface IImageUploadService
{
    Task<IReadOnlyList<string>> UploadAsync(Guid sessionId, IReadOnlyList<(Stream Stream, string ContentType)> files, CancellationToken cancellationToken = default);
}
