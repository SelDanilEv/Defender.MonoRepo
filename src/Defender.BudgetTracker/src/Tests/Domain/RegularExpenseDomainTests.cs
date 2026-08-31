using Defender.BudgetTracker.Domain.Entities.RegularExpenses;
using Defender.BudgetTracker.Domain.Enums;
using MongoDB.Bson;

namespace Defender.BudgetTracker.Tests.Domain;

public class RegularExpenseDomainTests
{
    [Fact]
    public void MonthlyContribution_WhenAnnual_UsesExactMonthlyFraction()
    {
        var expense = new ReviewedRegularExpense
        {
            Type = RegularExpenseType.Annual,
            Amount = 100L
        };

        Assert.Equal(100m / 12m, expense.MonthlyContribution);
    }

    [Theory]
    [InlineData(RegularExpenseType.Regular)]
    [InlineData(RegularExpenseType.Subscription)]
    public void MonthlyContribution_WhenMonthlyType_UsesFullAmount(RegularExpenseType type)
    {
        var expense = new ReviewedRegularExpense
        {
            Type = type,
            Amount = 123L
        };

        Assert.Equal(123m, expense.MonthlyContribution);
    }

    [Fact]
    public void ReviewMonth_WhenAssignedMidMonth_NormalizesToFirstDay()
    {
        var review = new global::Defender.BudgetTracker.Domain.Entities.Reviews.RegularExpenseReview
        {
            Month = new DateOnly(2026, 8, 27)
        };

        Assert.Equal(new DateOnly(2026, 8, 1), review.Month);
    }

    [Fact]
    public void ReviewedRegularExpense_WhenSerialized_UsesLegacyGuidRepresentation()
    {
        var regularExpenseId = Guid.NewGuid();
        var snapshot = new ReviewedRegularExpense
        {
            RegularExpenseId = regularExpenseId,
            Name = "Rent",
            Type = RegularExpenseType.Regular,
            Currency = Currency.PLN,
            Amount = 1
        };

        var document = snapshot.ToBsonDocument();
        var storedId = document[nameof(ReviewedRegularExpense.RegularExpenseId)].AsBsonBinaryData;

        Assert.Equal(BsonBinarySubType.UuidLegacy, storedId.SubType);
        Assert.Equal(regularExpenseId, storedId.ToGuid(GuidRepresentation.CSharpLegacy));
    }
}
