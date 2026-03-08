using Defender.Common.DB.SharedStorage.Entities;
using Defender.Common.DB.SharedStorage.Enums;
using Defender.Common.Interfaces;
using Defender.RiskGamesService.Application.Handlers.Transaction;
using Defender.RiskGamesService.Application.Models.Transaction;
using Defender.RiskGamesService.Domain.Entities.Lottery.UserTickets;
using Defender.RiskGamesService.Domain.Enums;
using SharedTransactionType = Defender.Common.DB.SharedStorage.Enums.TransactionType;

namespace Defender.RiskGamesService.Tests.Handlers;

public class TransactionHandlersTests
{
    [Fact]
    public async Task StartPaymentTransactionHandler_WhenAsUser_SetsCurrentUserAndCallsWallet()
    {
        var accountAccessor = new Mock<ICurrentAccountAccessor>();
        var walletWrapper = new Mock<Application.Common.Interfaces.Wrapper.IWalletWrapper>();
        var userId = Guid.NewGuid();
        var model = new TransactionModel { TransactionId = "tx-payment" };
        accountAccessor.Setup(x => x.GetAccountId()).Returns(userId);
        walletWrapper
            .Setup(x => x.StartPaymentTransactionAsync(userId, 100, Currency.USD, GameType.Lottery, "c"))
            .ReturnsAsync(model);
        var sut = new StartPaymentTransactionHandler(accountAccessor.Object, walletWrapper.Object);
        var request = new TransactionRequest("draw-1", 100, Currency.USD, SharedTransactionType.Payment, GameType.Lottery, "c");

        var result = await sut.HandleStartTransactionAsync(request);

        Assert.Equal("tx-payment", result.TransactionId);
        walletWrapper.VerifyAll();
    }

    [Fact]
    public async Task StartRechargeTransactionHandler_WhenAsUser_SetsCurrentUserAndCallsWallet()
    {
        var accountAccessor = new Mock<ICurrentAccountAccessor>();
        var walletWrapper = new Mock<Application.Common.Interfaces.Wrapper.IWalletWrapper>();
        var userId = Guid.NewGuid();
        var model = new TransactionModel { TransactionId = "tx-recharge" };
        accountAccessor.Setup(x => x.GetAccountId()).Returns(userId);
        walletWrapper
            .Setup(x => x.StartRechargeTransactionAsync(userId, 200, Currency.EUR, GameType.Lottery, "x"))
            .ReturnsAsync(model);
        var sut = new StartRechargeTransactionHandler(accountAccessor.Object, walletWrapper.Object);
        var request = new TransactionRequest("draw-2", 200, Currency.EUR, SharedTransactionType.Recharge, GameType.Lottery, "x");

        var result = await sut.HandleStartTransactionAsync(request);

        Assert.Equal("tx-recharge", result.TransactionId);
        walletWrapper.VerifyAll();
    }

    [Fact]
    public async Task LotteryTransactionHandler_WhenPaymentProceed_UpdatesTicketsAndKeepsTracking()
    {
        var repo = new Mock<Application.Common.Interfaces.Repositories.Lottery.ILotteryUserTicketRepository>();
        repo.Setup(x => x.UpdateManyUserTicketsAsync(
                It.IsAny<Defender.Common.DB.Model.FindModelRequest<UserTicket>>(),
                It.IsAny<Defender.Common.DB.Model.UpdateModelRequest<UserTicket>>()))
            .ReturnsAsync(1);
        var sut = new LotteryTransactionHandler(repo.Object);

        var result = await sut.HandleGameTransactionAsync(new TransactionStatusUpdatedEvent
        {
            TransactionId = "t1",
            TransactionType = SharedTransactionType.Payment,
            TransactionStatus = TransactionStatus.Proceed
        });

        Assert.False(result.StopTracking);
        repo.Verify(x => x.UpdateManyUserTicketsAsync(
            It.IsAny<Defender.Common.DB.Model.FindModelRequest<UserTicket>>(),
            It.IsAny<Defender.Common.DB.Model.UpdateModelRequest<UserTicket>>()), Times.Once);
    }

    [Fact]
    public async Task LotteryTransactionHandler_WhenPaymentFailed_DeletesTicketsAndStopsTracking()
    {
        var repo = new Mock<Application.Common.Interfaces.Repositories.Lottery.ILotteryUserTicketRepository>();
        repo.Setup(x => x.DeleteTicketByPaymentTransactionIdAsync("t2")).Returns(Task.CompletedTask);
        var sut = new LotteryTransactionHandler(repo.Object);

        var result = await sut.HandleGameTransactionAsync(new TransactionStatusUpdatedEvent
        {
            TransactionId = "t2",
            TransactionType = SharedTransactionType.Payment,
            TransactionStatus = TransactionStatus.Failed
        });

        Assert.True(result.StopTracking);
        repo.VerifyAll();
    }
}
