namespace Defender.TravelCalendarService.Domain.Entities;

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

public class PointOfInterest
{
    [BsonRepresentation(BsonType.String)]
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Text { get; set; } = string.Empty;
    public bool IsChecked { get; set; }
}
