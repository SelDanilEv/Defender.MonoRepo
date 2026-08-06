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
}
