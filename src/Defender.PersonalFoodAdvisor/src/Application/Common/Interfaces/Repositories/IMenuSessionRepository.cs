using Defender.PersonalFoodAdvisor.Domain.Entities;

namespace Defender.PersonalFoodAdvisor.Application.Common.Interfaces.Repositories;

public interface IMenuSessionRepository
{
    Task<MenuSession> CreateAsync(MenuSession session, CancellationToken cancellationToken = default);
    Task<MenuSession?> GetByIdAsync(Guid sessionId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MenuSession>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<MenuSession> UpdateAsync(MenuSession session, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid sessionId, CancellationToken cancellationToken = default);
}
