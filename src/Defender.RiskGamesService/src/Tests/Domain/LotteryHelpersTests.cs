using Defender.RiskGamesService.Domain.Helpers;

namespace Defender.RiskGamesService.Tests.Domain;

public class LotteryHelpersTests
{
    [Fact]
    public void CalculateLotteryPrizeAmount_WhenCalled_ReturnsPercentageOfAmount()
    {
        var result = LotteryHelpers.CalculateLotteryPrizeAmount(1500, 40);

        Assert.Equal(600, result);
    }

    [Fact]
    public void AsCurrency_WhenCalled_ConvertsCentsToUnits()
    {
        var result = LotteryHelpers.AsCurrency(12345);

        Assert.Equal(123.45, result);
    }

    [Fact]
    public void PurchaseLotteryTicketsTransactionComment_WhenCalled_FormatsComment()
    {
        var result = LotteryHelpers.PurchaseLotteryTicketsTransactionComment(1001, [1, 7, 21]);

        Assert.Equal("1001: 1, 7, 21", result);
    }
}
