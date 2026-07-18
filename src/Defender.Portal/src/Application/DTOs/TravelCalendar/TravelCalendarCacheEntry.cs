namespace Defender.Portal.Application.DTOs.TravelCalendar;

public record TravelCalendarCacheEntry(Guid UserId, string? From, string? To, TravelCalendarDto Calendar);
