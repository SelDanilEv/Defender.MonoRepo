using Defender.TravelCalendarService.Domain.Entities;

namespace Defender.TravelCalendarService.Application.Models.Requests;

public record VersionedRequest(long ExpectedVersion);
public record SetThemeRequest(long ExpectedVersion, CalendarTheme Theme);
public record CreateQueuedTripRequest(long ExpectedVersion, string Title);
public record CreateEventFromDateRequest(long ExpectedVersion, DateOnly Date);
public record UpdateTravelEventRequest(long ExpectedVersion, string Title, TravelEventType Type, DateOnly StartDate, DateOnly EndDate, string? Notes, bool HotelBooked, string? HotelName, string? HotelAddress, string? HotelBookingUrl, decimal HotelCostPln, decimal DistanceKm, string? MainPoint, decimal OtherCostPln);
public record AddPointRequest(long ExpectedVersion, string Text);
public record UpdatePointRequest(long ExpectedVersion, string? Text, bool? IsChecked);
public record AddParticipantRequest(long ExpectedVersion, Guid UserId, string DisplayName, string? AvatarUrl);
public record UpdateMyParticipationRequest(long ExpectedVersion, TravelParticipantStatus Status);
public record AddPackingItemRequest(long ExpectedVersion, string Text);
public record UpdatePackingItemRequest(long ExpectedVersion, string? Text, bool? IsChecked);
