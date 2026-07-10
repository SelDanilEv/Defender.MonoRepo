using Defender.Common.Wrapper;
using Defender.Portal.Application.Common.Interfaces.Wrappers;
using Defender.Portal.Application.DTOs.TravelCalendar;

namespace Defender.Portal.Infrastructure.Clients.TravelCalendar;

public class TravelCalendarWrapper(ITravelCalendarClient client) : BaseSwaggerWrapper, ITravelCalendarWrapper
{
    public Task<TravelCalendarDto> GetAsync(string? from, string? to, CancellationToken ct = default) => ExecuteSafelyAsync(() => client.GetAsync(from, to, ct));
    public Task<TravelCalendarMutationResultDto> SendAsync(HttpMethod method, string path, object request, CancellationToken ct = default) => ExecuteSafelyAsync(() => client.SendAsync(method, path, request, ct));
}
