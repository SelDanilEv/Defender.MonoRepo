namespace Defender.TravelCalendarService.Domain.ValueObjects;

public record VehicleSettings(string Name, decimal FuelConsumptionLitersPer100Km, decimal FuelPricePlnPerLiter);

public record WeekendSlot(DateOnly Start, DateOnly End);

public class HotelDetails
{
    public bool IsBooked { get; set; }
    public string? Name { get; set; }
    public string? Address { get; set; }
    public string? BookingUrl { get; set; }
    public decimal CostPln { get; set; }
}

public class TripDetails
{
    public decimal DistanceKm { get; set; }
    public string? MainPoint { get; set; }
    public List<Entities.PointOfInterest> Points { get; set; } = [];
}
