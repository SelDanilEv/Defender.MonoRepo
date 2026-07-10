using Defender.TravelCalendarService.Domain.Entities;

namespace Defender.TravelCalendarService.Tests.Domain;

public class TravelCalendarTests
{
    [Fact]
    public void SetTheme_WhenValueChanges_IncrementsVersion()
    {
        var calendar = TravelCalendar.Create(Guid.NewGuid(), DateTimeOffset.UtcNow);

        calendar.SetTheme(CalendarTheme.Dark, DateTimeOffset.UtcNow);

        Assert.Equal(CalendarTheme.Dark, calendar.Theme);
        Assert.Equal(1, calendar.Version);
    }

    [Fact]
    public void AddPackingItem_WhenTextValid_AddsItemAndIncrementsVersion()
    {
        var calendar = TravelCalendar.Create(Guid.NewGuid(), DateTimeOffset.UtcNow);

        var itemId = calendar.AddPackingItem("Passport", DateTimeOffset.UtcNow);

        var item = Assert.Single(calendar.PackingItems);
        Assert.Equal(itemId, item.Id);
        Assert.Equal(1, calendar.Version);
    }
}
