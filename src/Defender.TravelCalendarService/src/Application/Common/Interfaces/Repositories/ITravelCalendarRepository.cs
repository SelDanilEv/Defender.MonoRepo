using Defender.TravelCalendarService.Domain.Entities;

namespace Defender.TravelCalendarService.Application.Common.Interfaces.Repositories;

public interface ITravelCalendarRepository
{
    Task<TravelCalendar> GetOrCreateAsync(Guid userId, CancellationToken cancellationToken);
    Task<bool> ReplaceAsync(TravelCalendar calendar, long expectedVersion, CancellationToken cancellationToken);
}
