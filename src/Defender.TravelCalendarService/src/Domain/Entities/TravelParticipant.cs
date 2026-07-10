using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Defender.TravelCalendarService.Domain.Entities;

public class TravelParticipant
{
    [BsonRepresentation(BsonType.String)]
    public Guid UserId { get; set; }

    public string DisplayName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public TravelParticipantStatus Status { get; set; }
    public DateTimeOffset InvitedAtUtc { get; set; }
    public DateTimeOffset? RespondedAtUtc { get; set; }
}
