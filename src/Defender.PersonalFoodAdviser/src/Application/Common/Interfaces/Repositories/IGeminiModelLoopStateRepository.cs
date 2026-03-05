using Defender.PersonalFoodAdviser.Domain.Entities;

namespace Defender.PersonalFoodAdviser.Application.Common.Interfaces.Repositories;

public interface IGeminiModelLoopStateRepository
{
    Task<IReadOnlyList<GeminiModelLoopState>> GetAllAsync(CancellationToken cancellationToken = default);
    Task UpsertAsync(GeminiModelLoopState state, CancellationToken cancellationToken = default);
}
