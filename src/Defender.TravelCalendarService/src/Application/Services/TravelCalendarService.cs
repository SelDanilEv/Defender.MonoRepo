using Defender.TravelCalendarService.Application.Common.Interfaces.Repositories;
using Defender.TravelCalendarService.Application.Common.Interfaces.Services;
using Defender.TravelCalendarService.Application.DTOs;
using Defender.TravelCalendarService.Application.Models.Requests;
using Defender.TravelCalendarService.Domain.Entities;
using Defender.TravelCalendarService.Domain.Exceptions;
using Defender.TravelCalendarService.Domain.Services;
using Defender.TravelCalendarService.Domain.ValueObjects;

namespace Defender.TravelCalendarService.Application.Services;

public class TravelCalendarService(
    ITravelCalendarRepository calendarRepository,
    ITravelEventRepository eventRepository,
    TimeProvider timeProvider) : ITravelCalendarService
{
    public async Task<TravelCalendarDto> GetAsync(Guid userId, DateOnly? from, DateOnly? to, CancellationToken cancellationToken)
    {
        if (from.HasValue && to.HasValue && from > to)
        {
            throw new TravelCalendarValidationException("TRAVEL_CALENDAR_INVALID_RANGE", "The start of the requested range must not be after its end.");
        }

        var calendar = await calendarRepository.GetOrCreateAsync(userId, cancellationToken);
        var events = await eventRepository.GetVisibleAsync(userId, cancellationToken);
        var page = events.Where(item => IsVisibleInRange(item, from, to)).ToArray();
        return Map(calendar, page, userId, events);
    }

    public Task<TravelCalendarMutationResultDto> SetThemeAsync(Guid userId, SetThemeRequest request, CancellationToken cancellationToken)
        => MutateCalendarAsync(userId, request.ExpectedVersion, calendar => calendar.SetTheme(request.Theme, Now), cancellationToken);

    public async Task<TravelCalendarMutationResultDto> AddQueuedTripAsync(Guid userId, CreateQueuedTripRequest request, CancellationToken cancellationToken)
    {
        var calendar = await calendarRepository.GetOrCreateAsync(userId, cancellationToken);
        EnsureCalendarVersion(calendar, request.ExpectedVersion);

        var ownedEvents = await eventRepository.GetOwnedAsync(userId, cancellationToken);
        var queueOrder = ownedEvents.Where(item => item.StartDate == null && item.IsMustVisit).Select(item => item.QueueOrder).DefaultIfEmpty().Max() + 1;
        var travelEvent = TravelEvent.Queued(userId, request.Title, queueOrder, Now);

        await eventRepository.AddAsync(travelEvent, cancellationToken);
        var page = await GetAsync(userId, null, null, cancellationToken);
        return new(page, travelEvent.Id, null);
    }

    public async Task<TravelCalendarMutationResultDto> CreateEventFromDateAsync(Guid userId, CreateEventFromDateRequest request, CancellationToken cancellationToken)
    {
        var calendar = await calendarRepository.GetOrCreateAsync(userId, cancellationToken);

        var start = request.Date.DayOfWeek == DayOfWeek.Sunday ? request.Date.AddDays(-1) : request.Date;
        var isWeekend = request.Date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
        var end = isWeekend ? start.AddDays(1) : start;

        EnsureSeason(calendar, start, end);
        var visibleEvents = await eventRepository.GetVisibleAsync(userId, cancellationToken);
        EnsureAvailable(visibleEvents, start, end, null);

        var travelEvent = TravelEvent.Scheduled(
            userId,
            isWeekend ? "Weekend trip" : "New event",
            isWeekend ? TravelEventType.OvernightTrip : TravelEventType.DayTrip,
            start,
            end);
        StampNewEvent(travelEvent);

        await eventRepository.AddAsync(travelEvent, cancellationToken);
        var page = await GetAsync(userId, null, null, cancellationToken);
        return new(page, travelEvent.Id, null);
    }

    public async Task<TravelCalendarMutationResultDto> CreateEventAsync(Guid userId, CreateTravelEventRequest request, CancellationToken cancellationToken)
    {
        var calendar = await calendarRepository.GetOrCreateAsync(userId, cancellationToken);
        EnsureCalendarVersion(calendar, request.ExpectedVersion);

        var (start, end) = request.EndDate < request.StartDate
            ? (request.EndDate, request.StartDate)
            : (request.StartDate, request.EndDate);
        EnsureSeason(calendar, start, end);
        var visibleEvents = await eventRepository.GetVisibleAsync(userId, cancellationToken);
        EnsureAvailable(visibleEvents, start, end, null);

        var travelEvent = TravelEvent.Scheduled(userId, request.Title, request.Type, start, end);
        travelEvent.UpdateSharedDetails(
            userId,
            request.Title,
            request.Type,
            start,
            end,
            request.Notes,
            new HotelDetails
            {
                IsBooked = request.HotelBooked,
                Name = request.HotelName,
                Address = request.HotelAddress,
                BookingUrl = request.HotelBookingUrl,
                CostPln = request.HotelCostPln,
            },
            request.DistanceKm,
            request.MainPoint,
            request.OtherCostPln,
            Now);

        await eventRepository.AddAsync(travelEvent, cancellationToken);
        var page = await GetAsync(userId, null, null, cancellationToken);
        return new(page, travelEvent.Id, null);
    }

    public Task<TravelCalendarMutationResultDto> UpdateEventAsync(Guid userId, Guid eventId, UpdateTravelEventRequest request, CancellationToken cancellationToken)
        => MutateEventAsync(userId, eventId, request.ExpectedVersion, async (travelEvent, calendar, visibleEvents) =>
        {
            var hotel = new HotelDetails
            {
                IsBooked = request.HotelBooked,
                Name = request.HotelName,
                Address = request.HotelAddress,
                BookingUrl = request.HotelBookingUrl,
                CostPln = request.HotelCostPln,
            };

            var (start, end) = request.EndDate < request.StartDate
                ? (request.EndDate, request.StartDate)
                : (request.StartDate, request.EndDate);
            EnsureSeason(calendar, start, end);
            EnsureAvailable(visibleEvents, start, end, eventId);

            travelEvent.UpdateSharedDetails(userId, request.Title, request.Type, request.StartDate, request.EndDate, request.Notes, hotel, request.DistanceKm, request.MainPoint, request.OtherCostPln, Now);
            await SaveEventAsync(travelEvent, request.ExpectedVersion, cancellationToken);
            return (travelEvent.Id, (Guid?)null);
        }, cancellationToken);

    public Task<TravelCalendarMutationResultDto> RemoveEventAsync(Guid userId, Guid eventId, VersionedRequest request, CancellationToken cancellationToken)
        => MutateEventAsync(userId, eventId, request.ExpectedVersion, async (travelEvent, calendar, visibleEvents) =>
        {
            if (travelEvent.IsMustVisit)
            {
                travelEvent.Unschedule(userId, Now);
                await SaveEventAsync(travelEvent, request.ExpectedVersion, cancellationToken);
            }
            else
            {
                if (!travelEvent.CanEdit(userId))
                {
                    throw new TravelCalendarConflictException("TRAVEL_CALENDAR_OWNER_ONLY", "Only the event owner can delete the event.");
                }

                if (!await eventRepository.DeleteAsync(eventId, request.ExpectedVersion, cancellationToken))
                {
                    throw VersionConflict();
                }
            }

            return (eventId, (Guid?)null);
        }, cancellationToken);

    public Task<TravelCalendarMutationResultDto> AutoScheduleAsync(Guid userId, Guid eventId, VersionedRequest request, CancellationToken cancellationToken)
        => MutateEventAsync(userId, eventId, request.ExpectedVersion, async (travelEvent, calendar, visibleEvents) =>
        {
            travelEvent.AutoSchedule(userId, calendar.SeasonStart, calendar.SeasonEnd, visibleEvents, Now);
            await SaveEventAsync(travelEvent, request.ExpectedVersion, cancellationToken);
            return (eventId, (Guid?)null);
        }, cancellationToken);

    public Task<TravelCalendarMutationResultDto> AddPointAsync(Guid userId, Guid eventId, AddPointRequest request, CancellationToken cancellationToken)
        => MutateEventAsync(userId, eventId, request.ExpectedVersion, async (travelEvent, calendar, visibleEvents) =>
        {
            var pointId = travelEvent.AddPoint(userId, request.Text, Now);
            await SaveEventAsync(travelEvent, request.ExpectedVersion, cancellationToken);
            return (eventId, pointId);
        }, cancellationToken);

    public Task<TravelCalendarMutationResultDto> UpdatePointAsync(Guid userId, Guid eventId, Guid pointId, UpdatePointRequest request, CancellationToken cancellationToken)
        => MutateEventAsync(userId, eventId, request.ExpectedVersion, async (travelEvent, calendar, visibleEvents) =>
        {
            RequirePatch(request.Text, request.IsChecked);
            travelEvent.UpdatePoint(userId, pointId, request.Text, request.IsChecked, Now);
            await SaveEventAsync(travelEvent, request.ExpectedVersion, cancellationToken);
            return (eventId, pointId);
        }, cancellationToken);

    public Task<TravelCalendarMutationResultDto> RemovePointAsync(Guid userId, Guid eventId, Guid pointId, VersionedRequest request, CancellationToken cancellationToken)
        => MutateEventAsync(userId, eventId, request.ExpectedVersion, async (travelEvent, calendar, visibleEvents) =>
        {
            travelEvent.RemovePoint(userId, pointId, Now);
            await SaveEventAsync(travelEvent, request.ExpectedVersion, cancellationToken);
            return (eventId, pointId);
        }, cancellationToken);

    public Task<TravelCalendarMutationResultDto> AddParticipantAsync(Guid userId, Guid eventId, AddParticipantRequest request, CancellationToken cancellationToken)
        => MutateEventAsync(userId, eventId, request.ExpectedVersion, async (travelEvent, calendar, visibleEvents) =>
        {
            travelEvent.AddParticipant(userId, request.UserId, request.DisplayName, request.AvatarUrl, Now);
            await SaveEventAsync(travelEvent, request.ExpectedVersion, cancellationToken);
            return (eventId, (Guid?)null);
        }, cancellationToken);

    public Task<TravelCalendarMutationResultDto> RemoveParticipantAsync(Guid userId, Guid eventId, Guid participantUserId, VersionedRequest request, CancellationToken cancellationToken)
        => MutateEventAsync(userId, eventId, request.ExpectedVersion, async (travelEvent, calendar, visibleEvents) =>
        {
            travelEvent.RemoveParticipant(userId, participantUserId, Now);
            await SaveEventAsync(travelEvent, request.ExpectedVersion, cancellationToken);
            return (eventId, (Guid?)null);
        }, cancellationToken);

    public Task<TravelCalendarMutationResultDto> UpdateMyParticipationAsync(Guid userId, Guid eventId, UpdateMyParticipationRequest request, CancellationToken cancellationToken)
        => MutateEventAsync(userId, eventId, request.ExpectedVersion, async (travelEvent, calendar, visibleEvents) =>
        {
            if (request.Status == TravelParticipantStatus.Accepted || request.Status == TravelParticipantStatus.Declined)
            {
                travelEvent.RespondToInvitation(userId, request.Status, Now);
            }
            else
            {
                throw new TravelCalendarValidationException("TRAVEL_CALENDAR_INVALID_PARTICIPATION_STATUS", "Pending is not a valid response.");
            }

            await SaveEventAsync(travelEvent, request.ExpectedVersion, cancellationToken);
            return (eventId, (Guid?)null);
        }, cancellationToken);

    public Task<TravelCalendarMutationResultDto> AddPackingItemAsync(Guid userId, AddPackingItemRequest request, CancellationToken cancellationToken)
        => MutateCalendarAsync(userId, request.ExpectedVersion, calendar => calendar.AddPackingItem(request.Text, Now), cancellationToken);

    public Task<TravelCalendarMutationResultDto> UpdatePackingItemAsync(Guid userId, Guid itemId, UpdatePackingItemRequest request, CancellationToken cancellationToken)
        => MutateCalendarAsync(userId, request.ExpectedVersion, calendar =>
        {
            RequirePatch(request.Text, request.IsChecked);
            calendar.UpdatePackingItem(itemId, request.Text, request.IsChecked, Now);
        }, cancellationToken, itemId);

    public Task<TravelCalendarMutationResultDto> RemovePackingItemAsync(Guid userId, Guid itemId, VersionedRequest request, CancellationToken cancellationToken)
        => MutateCalendarAsync(userId, request.ExpectedVersion, calendar => calendar.RemovePackingItem(itemId, Now), cancellationToken, itemId);

    private DateTimeOffset Now => timeProvider.GetUtcNow();

    private async Task<TravelCalendarMutationResultDto> MutateCalendarAsync(
        Guid userId,
        long expectedVersion,
        Action<TravelCalendar> action,
        CancellationToken cancellationToken,
        Guid? affectedItemId = null)
    {
        var calendar = await calendarRepository.GetOrCreateAsync(userId, cancellationToken);
        EnsureCalendarVersion(calendar, expectedVersion);

        action(calendar);
        if (!await calendarRepository.ReplaceAsync(calendar, expectedVersion, cancellationToken))
        {
            throw VersionConflict();
        }

        var events = await eventRepository.GetVisibleAsync(userId, cancellationToken);
        return new(Map(calendar, events, userId), null, affectedItemId);
    }

    private async Task<TravelCalendarMutationResultDto> MutateEventAsync(
        Guid userId,
        Guid eventId,
        long expectedVersion,
        Func<TravelEvent, TravelCalendar, IReadOnlyList<TravelEvent>, Task<(Guid? EventId, Guid? ItemId)>> action,
        CancellationToken cancellationToken)
    {
        var calendar = await calendarRepository.GetOrCreateAsync(userId, cancellationToken);
        var visibleEvents = await eventRepository.GetVisibleAsync(userId, cancellationToken);
        var travelEvent = visibleEvents.FirstOrDefault(item => item.Id == eventId) ?? throw new TravelCalendarNotFoundException("Event was not found.");
        if (travelEvent.Version != expectedVersion)
        {
            throw VersionConflict();
        }

        var affected = await action(travelEvent, calendar, visibleEvents);
        var page = await GetAsync(userId, null, null, cancellationToken);
        return new(page, affected.EventId, affected.ItemId);
    }

    private async Task SaveEventAsync(TravelEvent travelEvent, long expectedVersion, CancellationToken cancellationToken)
    {
        if (!await eventRepository.ReplaceAsync(travelEvent, expectedVersion, cancellationToken))
        {
            throw VersionConflict();
        }
    }

    private static void EnsureCalendarVersion(TravelCalendar calendar, long expectedVersion)
    {
        if (calendar.Version != expectedVersion)
        {
            throw VersionConflict();
        }
    }

    private static void EnsureSeason(TravelCalendar calendar, DateOnly start, DateOnly end)
    {
        if (start < calendar.SeasonStart || end > calendar.SeasonEnd)
        {
            throw new TravelCalendarValidationException("TRAVEL_CALENDAR_DATE_OUTSIDE_SEASON", "Date is outside the season.");
        }
    }

    private static void EnsureAvailable(IEnumerable<TravelEvent> visibleEvents, DateOnly start, DateOnly end, Guid? excludedEventId)
    {
        if (visibleEvents.Where(item => item.Id != excludedEventId).Any(item => item.Overlaps(start, end)))
        {
            throw new TravelCalendarConflictException("TRAVEL_CALENDAR_DATE_OVERLAP", "Date range overlaps an event.");
        }
    }

    private static void StampNewEvent(TravelEvent travelEvent)
    {
        travelEvent.CreatedAtUtc = DateTimeOffset.UtcNow;
        travelEvent.UpdatedAtUtc = travelEvent.CreatedAtUtc;
    }

    private static TravelCalendarConflictException VersionConflict()
        => new("TRAVEL_CALENDAR_VERSION_CONFLICT", "Calendar changed. Reload and retry.");

    private static void RequirePatch(string? text, bool? check)
    {
        if (text == null && check == null)
        {
            throw new TravelCalendarValidationException("TRAVEL_CALENDAR_EMPTY_PATCH", "At least one field is required.");
        }
    }

    private static bool IsVisibleInRange(TravelEvent item, DateOnly? from, DateOnly? to)
    {
        if (item.StartDate == null || item.EndDate == null || (!from.HasValue && !to.HasValue))
        {
            return true;
        }

        return (!from.HasValue || item.EndDate >= from) && (!to.HasValue || item.StartDate <= to);
    }

    public static TravelCalendarDto Map(TravelCalendar calendar, IEnumerable<TravelEvent> events, Guid viewerUserId, IEnumerable<TravelEvent>? summaryEvents = null)
    {
        var orderedEvents = events
            .OrderBy(item => item.StartDate == null)
            .ThenBy(item => item.StartDate)
            .ThenBy(item => item.QueueOrder)
            .ToArray();

        var orderedSummaryEvents = (summaryEvents ?? events)
            .OrderBy(item => item.StartDate == null)
            .ThenBy(item => item.StartDate)
            .ThenBy(item => item.QueueOrder)
            .ToArray();
        var budget = TravelBudgetCalculator.Calculate(orderedSummaryEvents, calendar.Vehicle);

        return new(
            calendar.Id,
            calendar.Version,
            calendar.BaseCity,
            calendar.Currency,
            calendar.SeasonStart,
            calendar.SeasonEnd,
            calendar.Theme,
            new(calendar.Vehicle.Name, calendar.Vehicle.FuelConsumptionLitersPer100Km, calendar.Vehicle.FuelPricePlnPerLiter),
            calendar.Holidays.Select(item => new CalendarHolidayDto(item.Date, item.NameKey, item.Flag, item.Type)).ToArray(),
            orderedEvents.Select(item =>
            {
                var transport = TravelBudgetCalculator.CalculateTransport(item.Trip?.DistanceKm ?? 0, calendar.Vehicle);
                return new TravelEventDto(
                    item.Id,
                    item.Version,
                    item.OwnerUserId,
                    null,
                    item.Title,
                    item.Type,
                    item.StartDate,
                    item.EndDate,
                    item.Notes,
                    item.IsMustVisit,
                    item.QueueOrder,
                    item.Participants.Select(participant => new TravelParticipantDto(participant.UserId, participant.DisplayName, participant.AvatarUrl, participant.Status)).ToArray(),
                    item.GetParticipationStatus(viewerUserId),
                    item.CanEdit(viewerUserId),
                    item.CanRespond(viewerUserId),
                    item.Hotel == null ? null : new HotelDetailsDto(item.Hotel.IsBooked, item.Hotel.Name, item.Hotel.Address, item.Hotel.BookingUrl, item.Hotel.CostPln),
                    item.Trip?.DistanceKm ?? 0,
                    item.Trip?.MainPoint,
                    item.Trip?.Points.Select(point => new PointOfInterestDto(point.Id, point.Text, point.IsChecked)).ToArray() ?? [],
                    item.OtherCostPln,
                    transport,
                    (item.Hotel?.CostPln ?? 0) + transport + item.OtherCostPln);
            }).ToArray(),
            calendar.PackingItems.OrderBy(item => item.Order).Select(item => new PackingItemDto(item.Id, item.Text, item.IsChecked, item.Order)).ToArray(),
            new(
                orderedSummaryEvents.Count(item => item.Type == TravelEventType.OvernightTrip && item.StartDate != null),
                budget.HotelTotalPln,
                budget.TransportTotalPln,
                budget.OtherTotalPln,
                budget.GrandTotalPln,
                budget.Details.Select(item => new TravelBudgetDetailDto(item.EventId, item.Title, item.Date, item.HotelPln, item.TransportPln, item.OtherPln, item.TotalPln)).ToArray()),
            calendar.UpdatedAtUtc);
    }
}
