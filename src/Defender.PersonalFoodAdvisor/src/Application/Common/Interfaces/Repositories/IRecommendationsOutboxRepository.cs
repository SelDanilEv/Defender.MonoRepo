using Defender.PersonalFoodAdvisor.Domain.Entities;

namespace Defender.PersonalFoodAdvisor.Application.Common.Interfaces.Repositories;

public interface IRecommendationsOutboxRepository
{
    Task EnqueueAsync(RecommendationsOutboxMessage message, CancellationToken cancellationToken = default);
    Task<RecommendationsOutboxMessage?> ClaimNextDueAsync(Guid handlerId, DateTime nowUtc, TimeSpan lockDuration, CancellationToken cancellationToken = default);
    Task CompleteAsync(Guid id, Guid handlerId, CancellationToken cancellationToken = default);
    Task ReleaseAsync(Guid id, Guid handlerId, DateTime nextAttemptAtUtc, string? lastError, CancellationToken cancellationToken = default);
    Task DeleteBySessionIdAsync(Guid sessionId, CancellationToken cancellationToken = default);
}
