namespace Defender.TravelCalendarService.Domain.Entities;

public enum TravelEventType
{
    OvernightTrip,
    DayTrip,
    Event,
    Rest,
    Family,
}

public enum CalendarTheme
{
    Light,
    Dark,
}

public enum TravelParticipantStatus
{
    Pending,
    Accepted,
    Declined,
}
