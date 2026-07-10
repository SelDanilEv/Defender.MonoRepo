namespace Defender.Portal.Application.DTOs.TravelCalendar;

public record VehicleSettingsDto(string Name, decimal FuelConsumptionLitersPer100Km, decimal FuelPricePlnPerLiter);
public record CalendarHolidayDto(DateOnly Date, string NameKey, string Flag, string Type);
public record PointOfInterestDto(Guid Id, string Text, bool IsChecked);
public record PackingItemDto(Guid Id, string Text, bool IsChecked, int Order);
public record HotelDetailsDto(bool IsBooked, string? Name, string? Address, string? BookingUrl, decimal CostPln);
public record TravelParticipantDto(Guid UserId, string DisplayName, string? AvatarUrl, string Status);
public record TravelEventDto(Guid Id, long Version, Guid OwnerUserId, string? OwnerDisplayName, string Title, string Type, DateOnly? StartDate, DateOnly? EndDate, string? Notes, bool IsMustVisit, int QueueOrder, IReadOnlyList<TravelParticipantDto> Participants, string? MyParticipationStatus, bool CanEdit, bool CanRespond, HotelDetailsDto? Hotel, decimal DistanceKm, string? MainPoint, IReadOnlyList<PointOfInterestDto> Points, decimal OtherCostPln, decimal TransportCostPln, decimal TotalCostPln);
public record TravelBudgetDetailDto(Guid EventId, string Title, DateOnly? Date, decimal HotelPln, decimal TransportPln, decimal OtherPln, decimal TotalPln);
public record TravelCalendarSummaryDto(int OvernightTripCount, decimal HotelTotalPln, decimal TransportTotalPln, decimal OtherTotalPln, decimal GrandTotalPln, IReadOnlyList<TravelBudgetDetailDto> Details);
public record TravelCalendarDto(Guid Id, long Version, string BaseCity, string Currency, DateOnly SeasonStart, DateOnly SeasonEnd, string Theme, VehicleSettingsDto Vehicle, IReadOnlyList<CalendarHolidayDto> Holidays, IReadOnlyList<TravelEventDto> Events, IReadOnlyList<PackingItemDto> PackingItems, TravelCalendarSummaryDto Summary, DateTimeOffset UpdatedAtUtc);
public record TravelCalendarMutationResultDto(TravelCalendarDto Calendar, Guid? AffectedEventId, Guid? AffectedItemId);
public record TravelCalendarUserOptionDto(Guid UserId, string DisplayName, string Email, string? AvatarUrl);
