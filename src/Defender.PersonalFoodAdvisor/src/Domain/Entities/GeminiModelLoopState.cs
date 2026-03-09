using Defender.PersonalFoodAdvisor.Domain.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Defender.PersonalFoodAdvisor.Domain.Entities;

public class GeminiModelLoopState
{
    [BsonId]
    public string Id { get; set; } = string.Empty;

    [BsonRepresentation(BsonType.String)]
    public GeminiModelRoute Route { get; set; }

    public List<string> Models { get; set; } = [];
    public int ActiveModelIndex { get; set; }
    public DateTime? LastSwitchAtUtc { get; set; }
    public DateTime LastResetDateUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
