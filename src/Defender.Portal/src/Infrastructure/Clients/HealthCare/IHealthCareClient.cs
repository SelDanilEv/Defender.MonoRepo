using Defender.Portal.Application.DTOs.HealthCare;
using Defender.Portal.Application.Models.ApiRequests.HealthCare;

namespace Defender.Portal.Infrastructure.Clients.HealthCare;

public interface IHealthCareClient
{
    Task<IReadOnlyList<PortalHealthEventDto>> GetEventsAsync(DateTimeOffset? from, DateTimeOffset? to, CancellationToken cancellationToken = default);
    Task<PortalHealthEventDto> CreateEventAsync(PortalHealthEventDto healthEvent, CancellationToken cancellationToken = default);
    Task<PortalHealthEventDto?> UpdateEventAsync(Guid id, PortalHealthEventDto healthEvent, CancellationToken cancellationToken = default);
    Task<bool> DeleteEventAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PortalHealthChartShareDto> CreateShareAsync(CreateHealthChartShareRequest request, CancellationToken cancellationToken = default);
    Task<PortalHealthChartShareDto?> GetPublicShareAsync(string token, CancellationToken cancellationToken = default);
}
