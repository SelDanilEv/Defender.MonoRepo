using Defender.Common.Entities;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Defender.PersonalFoodAdviser.Domain.Entities;

public class ImageBlob : IBaseModel
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public Guid Id { get; set; }

    [BsonRepresentation(BsonType.String)]
    public Guid SessionId { get; set; }

    public byte[] Data { get; set; } = [];
    public string ContentType { get; set; } = "image/jpeg";
}
