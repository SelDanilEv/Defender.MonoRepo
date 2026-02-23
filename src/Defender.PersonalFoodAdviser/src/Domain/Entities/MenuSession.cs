using Defender.Common.Entities;
using Defender.PersonalFoodAdviser.Domain.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Defender.PersonalFoodAdviser.Domain.Entities;

public class MenuSession : IBaseModel
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public Guid Id { get; set; }

    [BsonRepresentation(BsonType.String)]
    public Guid UserId { get; set; }

    [BsonRepresentation(BsonType.String)]
    public MenuSessionStatus Status { get; set; }

    public List<string> ImageRefs { get; set; } = [];
    public List<string> ParsedItems { get; set; } = [];
    public List<string> ConfirmedItems { get; set; } = [];
    public List<string> RankedItems { get; set; } = [];
    public bool TrySomethingNew { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}
