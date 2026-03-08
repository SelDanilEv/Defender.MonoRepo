using Defender.Common.DB.SharedStorage.Entities;
using Defender.Common.DB.SharedStorage.Enums;
using Defender.RiskGamesService.Domain.Entities.Lottery;
using Defender.RiskGamesService.Domain.Entities.Lottery.Draw;
using Defender.RiskGamesService.Domain.Entities.Lottery.Enums;
using Defender.RiskGamesService.Domain.Entities.Lottery.TicketsSettings;
using Defender.RiskGamesService.Domain.Entities.Lottery.UserTickets;
using Defender.RiskGamesService.Domain.Entities.Transactions;
using Defender.RiskGamesService.Domain.Enums;
using Defender.RiskGamesService.Domain.Helpers;

namespace Defender.RiskGamesService.Tests.Domain;

public class RiskGamesDomainEntitiesTests
{
    [Fact]
    public void LotterySchedule_CreateAndUpdateNextStartDate_Works()
    {
        var schedule = LotterySchedule.Create(
            LotteryScheduleType.Custom,
            customHours: 1,
            startDate: DateTime.UtcNow.AddHours(-3));

        var updated = schedule.UpdateNextStartDate();

        Assert.NotNull(updated);
        Assert.True(schedule.NextStartDate >= DateTime.UtcNow.AddHours(-1));
        Assert.True(schedule.LastStartedDate <= schedule.NextStartDate);
    }

    [Fact]
    public void LotteryModel_IncomePercentage_ComputesFromTicketAndPrizeData()
    {
        var model = new LotteryModel
        {
            TicketsSetup = TicketsSetup.Create(
                ticketsAmount: 100,
                startTicketNumber: 1,
                allowedValues: [100, 200],
                isCustomValueAllowed: true,
                minValue: 100,
                maxValue: 200,
                allowedCurrencies: [Currency.USD],
                prizes:
                [
                    new TicketPrize { TicketsAmount = 2, Coefficient = 200 },
                    new TicketPrize { TicketsAmount = 1, Coefficient = 500 }
                ])
        };

        Assert.Equal(9100, model.IncomePercentage);
    }

    [Fact]
    public void LotteryDraw_Create_CopiesConfigurationFromModel()
    {
        var model = new LotteryModel
        {
            Id = Guid.NewGuid(),
            PublicNames = new Dictionary<string, string> { ["en"] = "lottery" },
            Schedule = LotterySchedule.Create(
                LotteryScheduleType.Daily,
                customHours: 1,
                startDate: DateTime.UtcNow.AddHours(-2)),
            TicketsSetup = TicketsSetup.Create(
                ticketsAmount: 20,
                startTicketNumber: 10,
                allowedValues: [100],
                isCustomValueAllowed: false,
                minValue: 100,
                maxValue: 100,
                allowedCurrencies: [Currency.USD, Currency.EUR],
                prizes:
                [
                    new TicketPrize { Coefficient = 150, TicketsAmount = 3 },
                    new TicketPrize { Coefficient = 200, TicketsAmount = 2 }
                ])
        };

        var draw = LotteryDraw.Create(model);

        Assert.Equal(model.Id, draw.LotteryId);
        Assert.Equal(10, draw.MinTicketNumber);
        Assert.Equal(29, draw.MaxTicketNumber);
        Assert.Equal(20, draw.TicketsAmount);
        Assert.Equal(2, draw.Winnings.Count);
        Assert.Contains(Currency.EUR, draw.AllowedCurrencies);
    }

    [Fact]
    public void LotteryDraw_GetDrawStartAndEndDate_HandlesCustomDuration()
    {
        var schedule = LotterySchedule.Create(
            LotteryScheduleType.Custom,
            customHours: 2,
            startDate: DateTime.UtcNow.AddHours(-10),
            durationType: LotteryScheduleType.Custom,
            durationCustomDays: 2);

        var (startDate, endDate) = LotteryDraw.GetDrawStartAndEndDate(schedule);

        Assert.True(endDate >= DateTime.UtcNow.AddHours(-2));
        Assert.True(startDate <= endDate);
    }

    [Fact]
    public void LotteryHelpers_Methods_ReturnExpectedValues()
    {
        var purchaseComment = LotteryHelpers.PurchaseLotteryTicketsTransactionComment(123, [1, 2, 3]);
        var prizeAmount = LotteryHelpers.CalculateLotteryPrizeAmount(1000, 250);
        var prizeComment = LotteryHelpers.LotteryPrizeTransactionComment(123, 7, 1000, 250);

        Assert.Contains("123: 1, 2, 3", purchaseComment);
        Assert.Equal(2500, prizeAmount);
        Assert.Contains("25", prizeComment);
        Assert.Equal(10.0, LotteryHelpers.AsCurrency(1000));
    }

    [Fact]
    public void OutboxTransactionStatus_MapsFromAndToEvent()
    {
        var sourceEvent = new TransactionStatusUpdatedEvent
        {
            TransactionId = "tx-1",
            TransactionStatus = TransactionStatus.Proceed,
            TransactionType = TransactionType.Payment,
            TransactionPurpose = TransactionPurpose.Lottery
        };

        var outbox = OutboxTransactionStatus.CreateFromStatusUpdatedEvent(sourceEvent);
        var mappedBack = outbox.ConvertToStatusUpdatedEvent();

        Assert.Equal("tx-1", outbox.TransactionId);
        Assert.Equal(TransactionStatus.Proceed, mappedBack.TransactionStatus);
        Assert.Equal(TransactionPurpose.Lottery, mappedBack.TransactionPurpose);
    }

    [Fact]
    public void DomainEntities_PropertyRoundtrip_Works()
    {
        var transaction = new TransactionToTrack
        {
            TransactionId = "tx-2",
            DrawId = "draw-1",
            GameType = GameType.Lottery
        };
        var userTicket = new UserTicket
        {
            DrawNumber = 77,
            TicketNumber = 11,
            Amount = 200,
            Currency = Currency.USD,
            UserId = Guid.NewGuid(),
            Status = UserTicketStatus.Paid
        };
        var winning = new Winning
        {
            Coefficient = 300,
            Tickets = [11, 12]
        };

        Assert.Equal(GameType.Lottery, transaction.GameType);
        Assert.Equal(77, userTicket.DrawNumber);
        Assert.Equal(UserTicketStatus.Paid, userTicket.Status);
        Assert.Equal(300, winning.Coefficient);
        Assert.Equal(2, winning.Tickets.Count);
    }
}
