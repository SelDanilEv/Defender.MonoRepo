using Defender.PersonalFoodAdviser.Domain.Entities;

namespace Defender.PersonalFoodAdviser.Application.Common.Interfaces.Repositories;

public interface IImageBlobRepository
{
    Task<ImageBlob> SaveAsync(ImageBlob blob, CancellationToken cancellationToken = default);
    Task<ImageBlob?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
