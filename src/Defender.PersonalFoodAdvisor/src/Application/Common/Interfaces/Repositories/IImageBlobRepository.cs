using Defender.PersonalFoodAdvisor.Domain.Entities;

namespace Defender.PersonalFoodAdvisor.Application.Common.Interfaces.Repositories;

public interface IImageBlobRepository
{
    Task<ImageBlob> SaveAsync(ImageBlob blob, CancellationToken cancellationToken = default);
    Task<ImageBlob?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task DeleteBySessionIdAsync(Guid sessionId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Guid>> FindSessionIdsByExactImageHashesAsync(
        IReadOnlyList<string> imageHashes,
        Guid excludedSessionId,
        CancellationToken cancellationToken = default);
}
