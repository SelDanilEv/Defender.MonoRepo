namespace Defender.Portal.Application.DTOs.HealthCare;

public record PortalHealthChartShareDto(
    string Token,
    string PublicUrl,
    IReadOnlyList<PortalHealthEventDto> Events,
    DateTimeOffset CreatedAtUtc);
