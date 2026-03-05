using Defender.PersonalFoodAdviser.Domain.Entities;

namespace Defender.PersonalFoodAdviser.Application.Common.Interfaces.Repositories;

public interface IMenuParsingOutboxRepository
{
    Task EnqueueAsync(MenuParsingOutboxMessage message, CancellationToken cancellationToken = default);
    Task<MenuParsingOutboxMessage?> ClaimNextDueAsync(Guid handlerId, DateTime nowUtc, TimeSpan lockDuration, CancellationToken cancellationToken = default);
    Task CompleteAsync(Guid id, Guid handlerId, CancellationToken cancellationToken = default);
    Task ReleaseAsync(Guid id, Guid handlerId, DateTime nextAttemptAtUtc, string? lastError, CancellationToken cancellationToken = default);
    Task DeleteBySessionIdAsync(Guid sessionId, CancellationToken cancellationToken = default);
}
