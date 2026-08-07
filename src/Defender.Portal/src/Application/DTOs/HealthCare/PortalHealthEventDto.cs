using System.Text.Json.Serialization;

namespace Defender.Portal.Application.DTOs.HealthCare;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PortalHealthEventType
{
    Temperature,
    Medication,
    Sleep,
    Wellbeing,
    Analysis,
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PortalAnalysisStatus
{
    Bad,
    HasDeviations,
    Excellent,
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

    public int? WellbeingScore { get; set; }

    public string? AnalysisName { get; set; }

    public PortalAnalysisStatus? AnalysisStatus { get; set; }

    public string? Notes { get; set; }
}
