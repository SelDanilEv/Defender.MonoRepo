using Defender.Common.Entities;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Defender.PersonalFoodAdvisor.Domain.Entities;

public class MenuParsingOutboxMessage : IBaseModel
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public Guid Id { get; set; }

    [BsonRepresentation(BsonType.String)]
    public Guid SessionId { get; set; }

    [BsonRepresentation(BsonType.String)]
    public Guid UserId { get; set; }

    public List<string> ImageRefs { get; set; } = [];
    public DateTime NextAttemptAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public DateTime? LockedUntilUtc { get; set; }

    [BsonRepresentation(BsonType.String)]
    public Guid? HandlerId { get; set; }

    public string? LastError { get; set; }
}
