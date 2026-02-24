using Defender.BudgetTracker.Domain.Entities.Position;
using Defender.BudgetTracker.Domain.Enums;

namespace Defender.BudgetTracker.Tests.Domain;

public class ReviewedPositionTests
{
    [Fact]
    public void FromPosition_WhenAmountProvided_MapsBaseFieldsAndAmount()
    {
        var source = new BasePosition
        {
            Name = "Salary",
            Currency = Currency.PLN,
            Tags = ["income", "monthly"],
            OrderPriority = 5
        };

        var result = ReviewedPosition.FromPosition(source, 12345);

        Assert.Equal("Salary", result.Name);
        Assert.Equal(Currency.PLN, result.Currency);
        Assert.Equal(source.Tags, result.Tags);
        Assert.Equal(5, result.OrderPriority);
        Assert.Equal(12345, result.Amount);
    }

    [Fact]
    public void FromPosition_WhenAmountNotProvided_UsesZeroAmount()
    {
        var source = new BasePosition { Name = "Rent", Currency = Currency.USD };

        var result = ReviewedPosition.FromPosition(source);

        Assert.Equal(0, result.Amount);
    }
}
