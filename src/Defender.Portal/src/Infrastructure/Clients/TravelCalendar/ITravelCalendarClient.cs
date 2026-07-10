using Defender.Portal.Application.DTOs.TravelCalendar;

namespace Defender.Portal.Infrastructure.Clients.TravelCalendar;

public interface ITravelCalendarClient
{
    Task<TravelCalendarDto> GetAsync(DateOnly? from, DateOnly? to, CancellationToken ct = default);
    Task<TravelCalendarMutationResultDto> SendAsync(HttpMethod method, string path, object request, CancellationToken ct = default);
}
