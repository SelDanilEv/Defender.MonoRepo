using Defender.Common.Wrapper;
using Defender.Portal.Application.Common.Interfaces.Wrappers;
using Defender.Portal.Application.DTOs.HealthCare;
using Defender.Portal.Application.Models.ApiRequests.HealthCare;

namespace Defender.Portal.Infrastructure.Clients.HealthCare;

public class HealthCareWrapper(IHealthCareClient client) : BaseSwaggerWrapper, IHealthCareWrapper
{
    public Task<IReadOnlyList<PortalHealthEventDto>> GetEventsAsync(DateTimeOffset? from, DateTimeOffset? to, CancellationToken cancellationToken = default)
        => ExecuteSafelyAsync(() => client.GetEventsAsync(from, to, cancellationToken));

    public Task<PortalHealthEventDto> CreateEventAsync(PortalHealthEventDto healthEvent, CancellationToken cancellationToken = default)
        => ExecuteSafelyAsync(() => client.CreateEventAsync(healthEvent, cancellationToken));

    public Task<PortalHealthEventDto?> UpdateEventAsync(Guid id, PortalHealthEventDto healthEvent, CancellationToken cancellationToken = default)
        => ExecuteSafelyAsync(() => client.UpdateEventAsync(id, healthEvent, cancellationToken));

    public Task<bool> DeleteEventAsync(Guid id, CancellationToken cancellationToken = default)
        => ExecuteSafelyAsync(() => client.DeleteEventAsync(id, cancellationToken));

    public Task<PortalHealthChartShareDto> CreateShareAsync(CreateHealthChartShareRequest request, CancellationToken cancellationToken = default)
        => ExecuteSafelyAsync(() => client.CreateShareAsync(request, cancellationToken));

    public Task<PortalHealthChartShareDto?> GetPublicShareAsync(string token, CancellationToken cancellationToken = default)
        => ExecuteSafelyAsync(() => client.GetPublicShareAsync(token, cancellationToken));
}
