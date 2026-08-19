using Defender.TravelCalendarService.Domain.Services;

namespace Defender.TravelCalendarService.Tests.Domain;

public class WeekendSlotProviderTests
{
    [Fact]
    public void GetSlots_WhenStartingMidweek_ReturnsConsecutiveWeekendSlots()
    {
        var slots = WeekendSlotProvider.GetSlots(new DateOnly(2026, 7, 1)).Take(3).ToArray();

        Assert.Equal(new DateOnly(2026, 7, 4), slots[0].Start);
        Assert.Equal(new DateOnly(2026, 7, 5), slots[0].End);
        Assert.Equal(new DateOnly(2026, 7, 11), slots[1].Start);
        Assert.Equal(new DateOnly(2026, 7, 18), slots[2].Start);
    }
}
