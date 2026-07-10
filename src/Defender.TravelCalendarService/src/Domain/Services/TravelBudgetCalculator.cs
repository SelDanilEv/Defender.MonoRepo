using Defender.TravelCalendarService.Domain.Entities;
using Defender.TravelCalendarService.Domain.ValueObjects;

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
    public static decimal CalculateTransport(decimal distanceKm, VehicleSettings vehicle) =>
        Math.Round(distanceKm / 100m * vehicle.FuelConsumptionLitersPer100Km * vehicle.FuelPricePlnPerLiter, 2, MidpointRounding.AwayFromZero);

    public static TravelBudgetSummary Calculate(IEnumerable<TravelEvent> events, VehicleSettings vehicle)
    {
        var lines = events.Where(item => item.StartDate != null).Select(item => new TravelBudgetLine(
            item.Id,
            item.Title,
            item.StartDate,
            item.Hotel?.CostPln ?? 0,
            CalculateTransport(item.Trip?.DistanceKm ?? 0, vehicle),
            item.OtherCostPln)).OrderBy(item => item.Date).ToArray();

        return new(lines.Sum(item => item.HotelPln), lines.Sum(item => item.TransportPln), lines.Sum(item => item.OtherPln), lines);
    }
}
