using Defender.TravelCalendarService.Domain.ValueObjects;

namespace Defender.TravelCalendarService.Domain.Services;

public static class WeekendSlotProvider
{
    public static IEnumerable<WeekendSlot> GetSlots(DateOnly startDate)
    {
        var daysUntilSaturday = ((int)DayOfWeek.Saturday - (int)startDate.DayOfWeek + 7) % 7;
        var date = startDate.AddDays(daysUntilSaturday);
        var lastStartDate = DateOnly.MaxValue.AddDays(-1);

        while (date <= lastStartDate)
        {
            yield return new WeekendSlot(date, date.AddDays(1));

            if (date > lastStartDate.AddDays(-7))
            {
                yield break;
            }

            date = date.AddDays(7);
        }
    }
}
