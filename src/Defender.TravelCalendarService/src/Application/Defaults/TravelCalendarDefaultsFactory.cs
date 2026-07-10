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

    public IReadOnlyList<TravelEvent> CreateSeedEvents(Guid userId, DateTimeOffset now)
        =>
        [
            Trip(userId, "Toruń and Bydgoszcz", new DateOnly(2026, 7, 4), new DateOnly(2026, 7, 5), 540, 252, 200, true, "Toruń Old Town"),
            Simple(userId, "Wedding", TravelEventType.Event, new DateOnly(2026, 7, 17), 500),
            Simple(userId, "Family celebration", TravelEventType.Event, new DateOnly(2026, 7, 31), 0),
            Simple(userId, "Birthday", TravelEventType.Event, new DateOnly(2026, 8, 8), 400),
            Trip(userId, "Ojców National Park", new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 2), 597, 250, 300, false, "Ojców National Park"),
            Simple(userId, "Family day", TravelEventType.Family, new DateOnly(2026, 7, 11), 0),
            Queue(userId, "Kraków", 1, now),
            Queue(userId, "Kielce", 2, now),
            Queue(userId, "Mikołajki", 3, now),
            Queue(userId, "Katowice", 4, now),
            Queue(userId, "Sopot", 5, now),
            Queue(userId, "Malbork", 6, now),
        ];

    private static TravelEvent Trip(Guid userId, string title, DateOnly start, DateOnly end, decimal distance, decimal hotel, decimal other, bool mustVisit, string mainPoint)
    {
        var item = TravelEvent.Scheduled(userId, title, TravelEventType.OvernightTrip, start, end, distance, hotel, other);
        item.IsMustVisit = mustVisit;
        item.Hotel!.IsBooked = mustVisit;
        item.Trip!.MainPoint = mainPoint;
        return item;
    }

    private static TravelEvent Simple(Guid userId, string title, TravelEventType type, DateOnly date, decimal cost)
        => TravelEvent.Scheduled(userId, title, type, date, date, otherCostPln: cost);

    private static TravelEvent Queue(Guid userId, string title, int order, DateTimeOffset now)
        => TravelEvent.Queued(userId, title, order, now);
}
