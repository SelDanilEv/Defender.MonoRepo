using Defender.TravelCalendarService.Application.Defaults;

namespace Defender.TravelCalendarService.Tests.Services;

public class TravelCalendarDefaultsFactoryTests
{
    [Fact]
    public void Create_WhenFirstUse_CreatesEmptyCalendar()
    {
        var factory = new TravelCalendarDefaultsFactory();
        var userId = Guid.NewGuid();
        var calendar = factory.Create(userId, DateTimeOffset.Parse("2026-06-28T12:00:00Z"));

        Assert.Empty(calendar.PackingItems);
        Assert.Null(typeof(TravelCalendarDefaultsFactory).GetMethod("CreateSeedEvents"));
    }
}
