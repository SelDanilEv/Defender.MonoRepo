using Defender.PersonalFoodAdviser.Domain.Entities;

namespace Defender.PersonalFoodAdviser.Application.Common.Interfaces.Repositories;

public interface IMenuSessionRepository
{
    Task<MenuSession> CreateAsync(MenuSession session, CancellationToken cancellationToken = default);
    Task<MenuSession?> GetByIdAsync(Guid sessionId, CancellationToken cancellationToken = default);
    Task<MenuSession> UpdateAsync(MenuSession session, CancellationToken cancellationToken = default);
}
