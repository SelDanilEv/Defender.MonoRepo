using Defender.TravelCalendarService.Domain.Services;

namespace Defender.TravelCalendarService.Tests.Domain;

public class WeekendSlotProviderTests
{
    [Fact]
    public void GetSlots_WhenQ3Season_ReturnsOrderedSaturdaySundaySlots()
    {
        var slots = WeekendSlotProvider.GetSlots(new DateOnly(2026, 7, 1), new DateOnly(2026, 9, 30));

        Assert.NotEmpty(slots);
        Assert.Equal(DayOfWeek.Saturday, slots[0].Start.DayOfWeek);
        Assert.Equal(slots[0].Start.AddDays(1), slots[0].End);
        Assert.True(slots.SequenceEqual(slots.OrderBy(slot => slot.Start)));
    }
}
