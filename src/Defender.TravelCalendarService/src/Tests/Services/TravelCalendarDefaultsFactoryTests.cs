using Defender.TravelCalendarService.Application.Defaults;
using Defender.TravelCalendarService.Domain.Services;
using Defender.TravelCalendarService.Domain.Entities;

namespace Defender.TravelCalendarService.Tests.Services;

public class TravelCalendarDefaultsFactoryTests
{
    [Fact]
    public void Create_WhenFirstUse_ReproducesReferenceSnapshot()
    {
        var factory = new TravelCalendarDefaultsFactory();
        var userId = Guid.NewGuid();
        var calendar = factory.Create(userId, DateTimeOffset.Parse("2026-06-28T12:00:00Z"));
        var events = factory.CreateSeedEvents(userId, DateTimeOffset.Parse("2026-06-28T12:00:00Z"));
        var summary = TravelBudgetCalculator.Calculate(events, calendar.Vehicle);

        Assert.Equal(6, events.Count(item => item.IsMustVisit && item.StartDate == null));
        Assert.Equal(2, events.Count(item => item.Type == TravelEventType.OvernightTrip && item.StartDate != null));
        Assert.Equal(2802.50m, summary.GrandTotalPln);
    }
}
