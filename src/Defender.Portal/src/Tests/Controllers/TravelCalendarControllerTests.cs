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

        Assert.Same(calendar, Assert.IsType<OkObjectResult>(result).Value);
        wrapper.Verify(item => item.GetAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static TravelCalendarDto CreateCalendar() => new(
        Guid.NewGuid(), 1, "Warsaw", "PLN", new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31), "Light",
        new VehicleSettingsDto("Car", 7, 6), [], [], [], new TravelCalendarSummaryDto(0, 0, 0, 0, 0, []), DateTimeOffset.UtcNow);
}
