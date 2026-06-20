namespace Defender.Portal.Application.DTOs.HealthCare;

public enum PortalHealthEventType
{
    Temperature,
    Medication,
    Sleep
}

public class PortalHealthEventDto
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public PortalHealthEventType Type { get; set; }

    public DateTimeOffset StartedAt { get; set; }

    public DateTimeOffset? EndedAt { get; set; }

    public decimal? TemperatureCelsius { get; set; }

    public string? MedicationName { get; set; }

    public decimal? MedicationAmount { get; set; }

    public string? MedicationUnit { get; set; }

    public string? Notes { get; set; }
}
