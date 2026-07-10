using Defender.TravelCalendarService.Domain.Entities;

namespace Defender.TravelCalendarService.Application.Defaults;

public class TravelCalendarDefaultsFactory
{
    public TravelCalendar Create(Guid userId, DateTimeOffset now)
    {
        var calendar = TravelCalendar.Create(userId, now);
        calendar.Holidays =
        [
            new(new DateOnly(2026, 8, 15), "armedForcesDay", "🇵🇱", "national"),
        ];
        return calendar;
    }
}
