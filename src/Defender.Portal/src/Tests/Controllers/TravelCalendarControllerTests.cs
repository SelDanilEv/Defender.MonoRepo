using AutoMapper;
using Defender.Common.Interfaces;
using Defender.DistributedCache;
using Defender.Portal.Application.Common.Interfaces.Wrappers;
using Defender.Portal.Application.DTOs.TravelCalendar;
using Defender.Portal.WebUI.Controllers.V1;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Defender.Portal.Tests.Controllers;

public class TravelCalendarControllerTests
{
    [Fact]
    public async Task Get_WhenRangeIsCached_ReturnsCachedCalendarWithoutCallingService()
    {
        var userId = Guid.NewGuid();
        var calendar = CreateCalendar();
        var account = new Mock<ICurrentAccountAccessor>();
        account.Setup(item => item.GetAccountId()).Returns(userId);
        var wrapper = new Mock<ITravelCalendarWrapper>();
        var cache = new Mock<IDistributedCache>();
        cache
            .Setup(item => item.Get<TravelCalendarCacheEntry>(It.IsAny<string>(), It.IsAny<Func<Task<TravelCalendarCacheEntry>>>(), TimeSpan.FromDays(7)))
            .ReturnsAsync(new TravelCalendarCacheEntry(userId, "2026-07-01", "2026-07-31", calendar));
        var sut = new TravelCalendarController(
            Mock.Of<IMediator>(),
            Mock.Of<IMapper>(),
            wrapper.Object,
            Mock.Of<IUserManagementWrapper>(),
            account.Object,
            cache.Object);

        var result = await sut.Get("2026-07-01", "2026-07-31", CancellationToken.None);

        Assert.Equal(calendar, Assert.IsType<OkObjectResult>(result).Value);
        wrapper.Verify(item => item.GetAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Get_WhenCalendarContainsSharedEvent_AddsOrganizerDisplayNameBeforeCaching()
    {
        var userId = Guid.NewGuid();
        var organizerId = Guid.NewGuid();
        var calendar = CreateCalendar() with
        {
            Events = [new TravelEventDto(organizerId, 1, organizerId, null, "Weekend trip", "DayTrip", new DateOnly(2026, 7, 18), new DateOnly(2026, 7, 18), null, false, 0, [], "Pending", false, true, null, 0, null, [], 0, 0, 0)],
        };
        var account = new Mock<ICurrentAccountAccessor>();
        account.Setup(item => item.GetAccountId()).Returns(userId);
        var wrapper = new Mock<ITravelCalendarWrapper>();
        wrapper.Setup(item => item.GetAsync("2026-07-01", "2026-07-31", It.IsAny<CancellationToken>())).ReturnsAsync(calendar);
        var users = new Mock<IUserManagementWrapper>();
        users.Setup(item => item.GetPublicUserInfoAsync(organizerId)).ReturnsAsync(new Application.DTOs.Accounts.PublicUserInfoDto { Id = organizerId, Nickname = "Alice" });
        var cache = new Mock<IDistributedCache>();
        cache
            .Setup(item => item.Get<TravelCalendarCacheEntry>(It.IsAny<string>(), It.IsAny<Func<Task<TravelCalendarCacheEntry>>>(), TimeSpan.FromDays(7)))
            .Returns((string _, Func<Task<TravelCalendarCacheEntry>> factory, TimeSpan _) => factory());
        var sut = new TravelCalendarController(Mock.Of<IMediator>(), Mock.Of<IMapper>(), wrapper.Object, users.Object, account.Object, cache.Object);

        var result = await sut.Get("2026-07-01", "2026-07-31", CancellationToken.None);

        var response = Assert.IsType<OkObjectResult>(result);
        var responseCalendar = Assert.IsType<TravelCalendarDto>(response.Value);
        Assert.Equal("Alice", responseCalendar.Events.Single().OwnerDisplayName);
        users.Verify(item => item.GetPublicUserInfoAsync(organizerId), Times.Once);
    }

    private static TravelCalendarDto CreateCalendar() => new(
        Guid.NewGuid(), 1, "Warsaw", "PLN", new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31), "Light",
        new VehicleSettingsDto("Car", 7, 6), [], [], [], new TravelCalendarSummaryDto(0, 0, 0, 0, 0, []), DateTimeOffset.UtcNow);
}
