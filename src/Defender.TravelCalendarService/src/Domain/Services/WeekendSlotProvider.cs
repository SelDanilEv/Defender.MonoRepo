using Defender.TravelCalendarService.Domain.ValueObjects;

namespace Defender.TravelCalendarService.Domain.Services;

public static class WeekendSlotProvider
{
    public static IReadOnlyList<WeekendSlot> GetSlots(DateOnly seasonStart, DateOnly seasonEnd)
    {
        var result = new List<WeekendSlot>();
        for (var date = seasonStart; date <= seasonEnd; date = date.AddDays(1))
        {
            if (date.DayOfWeek == DayOfWeek.Saturday && date.AddDays(1) <= seasonEnd)
            {
                result.Add(new WeekendSlot(date, date.AddDays(1)));
            }
        }

        return result;
    }
}
