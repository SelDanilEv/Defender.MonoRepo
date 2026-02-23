using Defender.Common.Entities;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Defender.PersonalFoodAdviser.Domain.Entities;

public class UserPreferences : IBaseModel
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public Guid Id { get; set; }

    [BsonRepresentation(BsonType.String)]
    public Guid UserId { get; set; }

    public List<string> Likes { get; set; } = [];
    public List<string> Dislikes { get; set; } = [];
}
