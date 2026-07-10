using Defender.Portal.Application.DTOs.TravelCalendar;

namespace Defender.Portal.Application.Common.Interfaces.Wrappers;

public interface ITravelCalendarWrapper
{
    Task<TravelCalendarDto> GetAsync(string? from, string? to, CancellationToken ct = default);
    Task<TravelCalendarMutationResultDto> SendAsync(HttpMethod method, string path, object request, CancellationToken ct = default);
}
