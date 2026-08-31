using Defender.BudgetTracker.Domain.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Defender.BudgetTracker.Domain.Entities.RegularExpenses;

public class ReviewedRegularExpense
{
    [BsonGuidRepresentation(GuidRepresentation.CSharpLegacy)]
    public Guid RegularExpenseId { get; set; }

    public string Name { get; set; } = string.Empty;

    [BsonRepresentation(BsonType.String)]
    public RegularExpenseType Type { get; set; }

    [BsonRepresentation(BsonType.String)]
    public Currency Currency { get; set; }

    public long Amount { get; set; }

    public int OrderPriority { get; set; }

    [BsonIgnore]
    public decimal MonthlyContribution => Type == RegularExpenseType.Annual
        ? Amount / 12m
        : Amount;
}
