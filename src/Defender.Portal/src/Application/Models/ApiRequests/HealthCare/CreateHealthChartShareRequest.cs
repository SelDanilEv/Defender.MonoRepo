namespace Defender.Portal.Application.Models.ApiRequests.HealthCare;

public record CreateHealthChartShareRequest(DateTimeOffset? From, DateTimeOffset? To);

public record UpdateHealthChartShareStatusRequest(bool IsEnabled);
