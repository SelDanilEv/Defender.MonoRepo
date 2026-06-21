using Defender.Common.Entities;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Defender.HealthCareService.Domain.Entities;

public enum HealthEventType
{
    Temperature,
    Medication,
    Sleep
}

public class HealthEvent : IBaseModel
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public Guid Id { get; set; } = Guid.NewGuid();

    [BsonRepresentation(BsonType.String)]
    public Guid UserId { get; set; }

    public HealthEventType Type { get; set; }

    public DateTimeOffset StartedAt { get; set; }

    public DateTimeOffset? EndedAt { get; set; }

    public decimal? TemperatureCelsius { get; set; }

    public string? MedicationName { get; set; }

    public decimal? MedicationAmount { get; set; }

    public string? MedicationUnit { get; set; }

    public string? Notes { get; set; }
}
