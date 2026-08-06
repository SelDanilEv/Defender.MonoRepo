using Defender.TravelCalendarService.Domain.Entities;

namespace Defender.TravelCalendarService.Domain.Services;

public record TravelBudgetLine(Guid EventId, string Title, DateOnly? Date, decimal HotelPln, decimal TransportPln, decimal OtherPln)
{
    public decimal TotalPln => HotelPln + TransportPln + OtherPln;
}

public record TravelBudgetSummary(decimal HotelTotalPln, decimal TransportTotalPln, decimal OtherTotalPln, IReadOnlyList<TravelBudgetLine> Details)
{
    public decimal GrandTotalPln => HotelTotalPln + TransportTotalPln + OtherTotalPln;
}

public static class TravelBudgetCalculator
{
    public static TravelBudgetSummary Calculate(IEnumerable<TravelEvent> events)
    {
        var lines = events.Where(item => item.StartDate != null).Select(item => new TravelBudgetLine(
            item.Id,
            item.Title,
            item.StartDate,
            item.Hotel?.CostPln ?? 0,
            item.Trip?.TransportCostPln ?? 0,
            item.OtherCostPln)).OrderBy(item => item.Date).ToArray();

        return new(lines.Sum(item => item.HotelPln), lines.Sum(item => item.TransportPln), lines.Sum(item => item.OtherPln), lines);
    }
}
