using Defender.TravelCalendarService.Application.Common.Interfaces.Repositories;
using Defender.TravelCalendarService.Application.Models.Requests;
using Defender.TravelCalendarService.Domain.Entities;
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
}
