namespace Defender.TravelCalendarService.Domain.Exceptions;

public abstract class TravelCalendarDomainException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}

public sealed class TravelCalendarConflictException(string code, string message) : TravelCalendarDomainException(code, message);

public sealed class TravelCalendarNotFoundException(string message) : TravelCalendarDomainException("TRAVEL_CALENDAR_NOT_FOUND", message);

public sealed class TravelCalendarValidationException(string code, string message) : TravelCalendarDomainException(code, message);
