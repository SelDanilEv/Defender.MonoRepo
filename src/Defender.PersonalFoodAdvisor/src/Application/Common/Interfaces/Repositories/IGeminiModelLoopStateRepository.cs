using Defender.PersonalFoodAdvisor.Domain.Entities;

namespace Defender.PersonalFoodAdvisor.Application.Common.Interfaces.Repositories;

public interface IGeminiModelLoopStateRepository
{
    Task<IReadOnlyList<GeminiModelLoopState>> GetAllAsync(CancellationToken cancellationToken = default);
    Task UpsertAsync(GeminiModelLoopState state, CancellationToken cancellationToken = default);
}
