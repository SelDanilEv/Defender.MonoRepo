using Defender.Common.Wrapper;
using Defender.Portal.Application.Common.Interfaces.Wrappers;
using Defender.Portal.Application.DTOs.TravelCalendar;

namespace Defender.Portal.Infrastructure.Clients.TravelCalendar;

public class TravelCalendarWrapper(ITravelCalendarClient client) : BaseSwaggerWrapper, ITravelCalendarWrapper
{
    public Task<TravelCalendarDto> GetAsync(CancellationToken ct = default) => ExecuteSafelyAsync(() => client.GetAsync(ct));
    public Task<TravelCalendarMutationResultDto> SendAsync(HttpMethod method, string path, object request, CancellationToken ct = default) => ExecuteSafelyAsync(() => client.SendAsync(method, path, request, ct));
}
