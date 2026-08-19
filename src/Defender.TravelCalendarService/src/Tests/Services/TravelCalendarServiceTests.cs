using Defender.TravelCalendarService.Application.Common.Interfaces.Repositories;
using Defender.TravelCalendarService.Application.Models.Requests;
using Defender.TravelCalendarService.Domain.Entities;
using Defender.TravelCalendarService.Domain.Exceptions;
using Defender.TravelCalendarService.Domain.ValueObjects;
using Moq;
using CalendarService = Defender.TravelCalendarService.Application.Services.TravelCalendarService;

namespace Defender.TravelCalendarService.Tests.Services;

public class TravelCalendarServiceTests
{
    [Fact]
    public async Task CreateEventFromDateAsync_WhenDateIsOutsideFormerRange_PersistsEvent()
    {
        var userId = Guid.NewGuid();
        var calendar = TravelCalendar.Create(userId, DateTimeOffset.Parse("2026-07-01T00:00:00Z"));
        var calendarRepository = new Mock<ITravelCalendarRepository>();
        var eventRepository = new Mock<ITravelEventRepository>();
        calendarRepository
            .Setup(repository => repository.GetOrCreateAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(calendar);
        eventRepository
            .Setup(repository => repository.GetVisibleAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var service = new CalendarService(calendarRepository.Object, eventRepository.Object, TimeProvider.System);
        var request = new CreateEventFromDateRequest(calendar.Version, new DateOnly(2026, 12, 25));

        var result = await service.CreateEventFromDateAsync(userId, request, CancellationToken.None);

        eventRepository.Verify(repository => repository.AddAsync(
            It.Is<TravelEvent>(item =>
                item.Title == "New event"
                && item.Type == TravelEventType.DayTrip
                && item.StartDate == new DateOnly(2026, 12, 25)
                && item.EndDate == new DateOnly(2026, 12, 25)),
            It.IsAny<CancellationToken>()), Times.Once);
        Assert.NotNull(result.AffectedEventId);
    }

    [Fact]
    public async Task CreateEventAsync_WhenRequestIsValid_PersistsSuppliedEvent()
    {
        var userId = Guid.NewGuid();
        var calendar = TravelCalendar.Create(userId, DateTimeOffset.Parse("2026-07-01T00:00:00Z"));
        var calendarRepository = new Mock<ITravelCalendarRepository>();
        var eventRepository = new Mock<ITravelEventRepository>();
        calendarRepository
            .Setup(repository => repository.GetOrCreateAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(calendar);
        eventRepository
            .Setup(repository => repository.GetVisibleAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var service = new CalendarService(calendarRepository.Object, eventRepository.Object, TimeProvider.System);
        var request = new CreateTravelEventRequest(
            calendar.Version,
            "Museum",
            TravelEventType.Event,
            new DateOnly(2026, 7, 18),
            new DateOnly(2026, 7, 18),
            "Modern art",
            false,
            null,
            null,
            null,
            0,
            0,
            null,
            25);

        var result = await service.CreateEventAsync(userId, request, CancellationToken.None);

        eventRepository.Verify(repository => repository.AddAsync(
            It.Is<TravelEvent>(item =>
                item.OwnerUserId == userId
                && item.Title == "Museum"
                && item.Type == TravelEventType.Event
                && item.StartDate == new DateOnly(2026, 7, 18)
                && item.EndDate == new DateOnly(2026, 7, 18)
                && item.Notes == "Modern art"
                && item.OtherCostPln == 25),
            It.IsAny<CancellationToken>()), Times.Once);
        Assert.NotNull(result.AffectedEventId);
    }

    [Fact]
    public async Task CreateEventAsync_WhenTripTransportCostProvided_PersistsTransportCostOnTrip()
    {
        var userId = Guid.NewGuid();
        var calendar = TravelCalendar.Create(userId, DateTimeOffset.Parse("2026-07-01T00:00:00Z"));
        var calendarRepository = new Mock<ITravelCalendarRepository>();
        var eventRepository = new Mock<ITravelEventRepository>();
        calendarRepository
            .Setup(repository => repository.GetOrCreateAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(calendar);
        eventRepository
            .Setup(repository => repository.GetVisibleAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var service = new CalendarService(calendarRepository.Object, eventRepository.Object, TimeProvider.System);
        var request = new CreateTravelEventRequest(
            calendar.Version,
            "Mountains",
            TravelEventType.DayTrip,
            new DateOnly(2026, 7, 18),
            new DateOnly(2026, 7, 18),
            null,
            false,
            null,
            null,
            null,
            0,
            120,
            "Zakopane",
            0);

        await service.CreateEventAsync(userId, request, CancellationToken.None);

        eventRepository.Verify(repository => repository.AddAsync(
            It.Is<TravelEvent>(item => item.Trip != null && item.Trip.TransportCostPln == 120m),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateEventAsync_WhenDateRangeOverlapsExistingEvent_ThrowsConflictWithOverlapCode()
    {
        var userId = Guid.NewGuid();
        var calendar = TravelCalendar.Create(userId, DateTimeOffset.Parse("2026-07-01T00:00:00Z"));
        var calendarRepository = new Mock<ITravelCalendarRepository>();
        var eventRepository = new Mock<ITravelEventRepository>();
        calendarRepository
            .Setup(repository => repository.GetOrCreateAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(calendar);
        var existingEvent = TravelEvent.Scheduled(userId, "Existing trip", TravelEventType.OvernightTrip, new DateOnly(2026, 7, 17), new DateOnly(2026, 7, 19));
        eventRepository
            .Setup(repository => repository.GetVisibleAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([existingEvent]);

        var service = new CalendarService(calendarRepository.Object, eventRepository.Object, TimeProvider.System);
        var request = new CreateTravelEventRequest(
            calendar.Version,
            "Museum",
            TravelEventType.Event,
            new DateOnly(2026, 7, 18),
            new DateOnly(2026, 7, 18),
            "Modern art",
            false,
            null,
            null,
            null,
            0,
            0,
            null,
            25);

        var exception = await Assert.ThrowsAsync<TravelCalendarConflictException>(
            () => service.CreateEventAsync(userId, request, CancellationToken.None));

        Assert.Equal("TRAVEL_CALENDAR_DATE_OVERLAP", exception.Code);
    }

    [Fact]
    public async Task CreateEventAsync_WhenExpectedVersionIsStale_ThrowsConflictWithVersionConflictCode()
    {
        var userId = Guid.NewGuid();
        var calendar = TravelCalendar.Create(userId, DateTimeOffset.Parse("2026-07-01T00:00:00Z"));
        var calendarRepository = new Mock<ITravelCalendarRepository>();
        var eventRepository = new Mock<ITravelEventRepository>();
        calendarRepository
            .Setup(repository => repository.GetOrCreateAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(calendar);

        var service = new CalendarService(calendarRepository.Object, eventRepository.Object, TimeProvider.System);
        var request = new CreateTravelEventRequest(
            calendar.Version + 1,
            "Museum",
            TravelEventType.Event,
            new DateOnly(2026, 7, 18),
            new DateOnly(2026, 7, 18),
            "Modern art",
            false,
            null,
            null,
            null,
            0,
            0,
            null,
            25);

        var exception = await Assert.ThrowsAsync<TravelCalendarConflictException>(
            () => service.CreateEventAsync(userId, request, CancellationToken.None));

        Assert.Equal("TRAVEL_CALENDAR_VERSION_CONFLICT", exception.Code);
    }

    // Contract guard for the Portal-side "widen the invitations fetch to unbounded" fix:
    // useTravelCalendar.ts's load() now calls GET with no from/to at all. This asserts
    // that already-existing backend behavior (no production code changed here) really
    // does return everything in that case, so the frontend fix has something to rely on.
    [Fact]
    public async Task GetAsync_WhenNoRangeProvided_ReturnsEveryVisibleEventRegardlessOfDate()
    {
        var userId = Guid.NewGuid();
        var calendar = TravelCalendar.Create(userId, DateTimeOffset.Parse("2026-07-01T00:00:00Z"));
        var calendarRepository = new Mock<ITravelCalendarRepository>();
        var eventRepository = new Mock<ITravelEventRepository>();
        calendarRepository
            .Setup(repository => repository.GetOrCreateAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(calendar);

        var withinDateRange = TravelEvent.Scheduled(userId, "Within date range", TravelEventType.DayTrip, new DateOnly(2026, 7, 18), new DateOnly(2026, 7, 18));
        var farInThePast = TravelEvent.Scheduled(userId, "Long past", TravelEventType.DayTrip, new DateOnly(2020, 1, 1), new DateOnly(2020, 1, 1));
        var farInTheFuture = TravelEvent.Scheduled(userId, "Far future", TravelEventType.DayTrip, new DateOnly(2030, 1, 1), new DateOnly(2030, 1, 1));
        var undated = TravelEvent.Queued(userId, "Wishlist item", 0, DateTimeOffset.Parse("2026-07-01T00:00:00Z"));
        eventRepository
            .Setup(repository => repository.GetVisibleAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([withinDateRange, farInThePast, farInTheFuture, undated]);

        var service = new CalendarService(calendarRepository.Object, eventRepository.Object, TimeProvider.System);

        var result = await service.GetAsync(userId, null, null, CancellationToken.None);

        Assert.Equal(4, result.Events.Count);
        Assert.Contains(result.Events, item => item.Id == withinDateRange.Id);
        Assert.Contains(result.Events, item => item.Id == farInThePast.Id);
        Assert.Contains(result.Events, item => item.Id == farInTheFuture.Id);
        Assert.Contains(result.Events, item => item.Id == undated.Id);
    }

    [Fact]
    public async Task GetAsync_WhenRangeProvided_ExcludesDatedEventsOutsideItButKeepsUndatedOnes()
    {
        var userId = Guid.NewGuid();
        var calendar = TravelCalendar.Create(userId, DateTimeOffset.Parse("2026-07-01T00:00:00Z"));
        var calendarRepository = new Mock<ITravelCalendarRepository>();
        var eventRepository = new Mock<ITravelEventRepository>();
        calendarRepository
            .Setup(repository => repository.GetOrCreateAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(calendar);

        var insideRange = TravelEvent.Scheduled(userId, "Inside range", TravelEventType.DayTrip, new DateOnly(2026, 7, 18), new DateOnly(2026, 7, 18));
        var beforeRange = TravelEvent.Scheduled(userId, "Before range", TravelEventType.DayTrip, new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 1));
        var afterRange = TravelEvent.Scheduled(userId, "After range", TravelEventType.DayTrip, new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 1));
        var undated = TravelEvent.Queued(userId, "Wishlist item", 0, DateTimeOffset.Parse("2026-07-01T00:00:00Z"));
        eventRepository
            .Setup(repository => repository.GetVisibleAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([insideRange, beforeRange, afterRange, undated]);

        var service = new CalendarService(calendarRepository.Object, eventRepository.Object, TimeProvider.System);

        var result = await service.GetAsync(userId, new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 31), CancellationToken.None);

        Assert.Equal(2, result.Events.Count);
        Assert.Contains(result.Events, item => item.Id == insideRange.Id);
        Assert.Contains(result.Events, item => item.Id == undated.Id);
        Assert.DoesNotContain(result.Events, item => item.Id == beforeRange.Id);
        Assert.DoesNotContain(result.Events, item => item.Id == afterRange.Id);
    }
}
