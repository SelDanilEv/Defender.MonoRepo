using Defender.BudgetTracker.Domain.Entities.Interfaces;
using Defender.BudgetTracker.Domain.Entities.Rates;
using Defender.BudgetTracker.Domain.Entities.RegularExpenses;
using Defender.Common.Entities;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Defender.BudgetTracker.Domain.Entities.Reviews;

public class RegularExpenseReview : IUserOwnedModel, IBaseModel
{
    private DateOnly _month;

    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public Guid Id { get; set; }

    [BsonGuidRepresentation(GuidRepresentation.CSharpLegacy)]
    public Guid UserId { get; set; }

    public DateOnly Month
    {
        get => _month;
        set => _month = NormalizeMonth(value);
    }

    public List<ReviewedRegularExpense> Expenses { get; set; } = [];

    public RatesModel RatesModel { get; set; } = new();

    public static DateOnly NormalizeMonth(DateOnly date)
        => new(date.Year, date.Month, 1);
}
