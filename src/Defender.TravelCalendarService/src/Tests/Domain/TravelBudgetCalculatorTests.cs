using Defender.TravelCalendarService.Domain.Entities;
using Defender.TravelCalendarService.Domain.Services;

namespace Defender.TravelCalendarService.Tests.Domain;

public class TravelBudgetCalculatorTests
{
    [Fact]
    public void Calculate_WhenEventsProvided_ReturnsCategoryAndGrandTotals()
    {
        var events = new[]
        {
            TravelEvent.Scheduled(Guid.NewGuid(), "Gdansk", TravelEventType.OvernightTrip, new DateOnly(2026, 7, 4), new DateOnly(2026, 7, 5), 568, 251, 700),
            TravelEvent.Scheduled(Guid.NewGuid(), "Concert", TravelEventType.Event, new DateOnly(2026, 8, 8), new DateOnly(2026, 8, 8), otherCostPln: 700),
        };

        var result = TravelBudgetCalculator.Calculate(events);

        Assert.Equal(251m, result.HotelTotalPln);
        Assert.Equal(568m, result.TransportTotalPln);
        Assert.Equal(1400m, result.OtherTotalPln);
        Assert.Equal(2219m, result.GrandTotalPln);
    }
}
