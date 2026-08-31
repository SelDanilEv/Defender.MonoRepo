using Defender.BudgetTracker.Domain.Entities.Interfaces;
using Defender.BudgetTracker.Domain.Enums;
using Defender.Common.Entities;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Defender.BudgetTracker.Domain.Entities.RegularExpenses;

public class RegularExpense : IUserOwnedModel, IBaseModel
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public Guid Id { get; set; }

    [BsonGuidRepresentation(GuidRepresentation.CSharpLegacy)]
    public Guid UserId { get; set; }

    public string Name { get; set; } = string.Empty;

    [BsonRepresentation(BsonType.String)]
    public RegularExpenseType Type { get; set; }

    [BsonRepresentation(BsonType.String)]
    public Currency Currency { get; set; }

    public long DefaultAmount { get; set; }

    public int OrderPriority { get; set; }
}
