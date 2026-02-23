using Defender.Common.Entities;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Defender.PersonalFoodAdviser.Domain.Entities;

public class DishRating : IBaseModel
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public Guid Id { get; set; }

    [BsonRepresentation(BsonType.String)]
    public Guid UserId { get; set; }

    public string DishName { get; set; } = string.Empty;
    public int Rating { get; set; }

    [BsonRepresentation(BsonType.String)]
    public Guid? SessionId { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}
