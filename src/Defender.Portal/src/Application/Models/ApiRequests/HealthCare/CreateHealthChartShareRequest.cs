using Defender.Portal.Application.DTOs.HealthCare;

namespace Defender.Portal.Application.Models.ApiRequests.HealthCare;

public record CreateHealthChartShareRequest(
    DateTimeOffset? From,
    DateTimeOffset? To,
    HealthChartShareRangeMode? RangeMode = null);

public record UpdateHealthChartShareStatusRequest(bool IsEnabled);
