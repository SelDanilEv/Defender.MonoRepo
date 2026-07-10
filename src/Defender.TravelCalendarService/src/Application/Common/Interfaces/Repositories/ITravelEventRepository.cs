using Defender.TravelCalendarService.Domain.Entities;

namespace Defender.TravelCalendarService.Application.Common.Interfaces.Repositories;

public interface ITravelEventRepository
{
    Task<IReadOnlyList<TravelEvent>> GetVisibleAsync(Guid userId, CancellationToken cancellationToken);
    Task<IReadOnlyList<TravelEvent>> GetOwnedAsync(Guid userId, CancellationToken cancellationToken);
    Task<TravelEvent?> GetByIdAsync(Guid eventId, CancellationToken cancellationToken);
    Task AddAsync(TravelEvent travelEvent, CancellationToken cancellationToken);
    Task AddRangeAsync(IEnumerable<TravelEvent> travelEvents, CancellationToken cancellationToken);
    Task<bool> ReplaceAsync(TravelEvent travelEvent, long expectedVersion, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid eventId, long expectedVersion, CancellationToken cancellationToken);
}
