namespace Defender.Portal.Infrastructure.Clients.TravelCalendar;

public sealed class TravelCalendarUpstreamException(int status, string? code, string detail) : Exception(detail)
{
    public int Status { get; } = status;
    public string? Code { get; } = code;
}
