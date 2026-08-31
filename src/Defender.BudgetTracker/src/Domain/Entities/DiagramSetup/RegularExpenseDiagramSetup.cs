using Defender.BudgetTracker.Domain.Entities.Interfaces;
using Defender.BudgetTracker.Domain.Enums;
using Defender.Common.Entities;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Defender.BudgetTracker.Domain.Entities.DiagramSetup;

public class RegularExpenseDiagramSetup : IUserOwnedModel, IBaseModel
{
    private DateOnly? _endMonth;

    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public Guid Id { get; set; }

    [BsonGuidRepresentation(GuidRepresentation.CSharpLegacy)]
    public Guid UserId { get; set; }

    [BsonRepresentation(BsonType.String)]
    public Currency MainCurrency { get; set; }

    public int? LastMonths { get; set; }

    public DateOnly? EndMonth
    {
        get => _endMonth;
        set => _endMonth = value.HasValue
            ? new DateOnly(value.Value.Year, value.Value.Month, 1)
            : null;
    }
}
