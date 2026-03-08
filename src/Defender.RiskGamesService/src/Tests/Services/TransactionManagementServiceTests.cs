using Defender.Common.Exceptions;
using Defender.RiskGamesService.Application.Common.Interfaces.Repositories.Transactions;
using Defender.RiskGamesService.Application.Common.Interfaces.Wrapper;
using Defender.RiskGamesService.Application.Factories.Transaction;
using Defender.RiskGamesService.Application.Handlers.Transaction;
using Defender.RiskGamesService.Application.Models.Transaction;
using Defender.RiskGamesService.Application.Services.Transaction;
using Defender.RiskGamesService.Domain.Entities.Transactions;
using Defender.RiskGamesService.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;

using SharedTransactionType = Defender.Common.DB.SharedStorage.Enums.TransactionType;
using SharedTransactionStatus = Defender.Common.DB.SharedStorage.Enums.TransactionStatus;
using SharedTransactionPurpose = Defender.Common.DB.SharedStorage.Enums.TransactionPurpose;

namespace Defender.RiskGamesService.Tests.Services;

public class TransactionManagementServiceTests
{
    private readonly Mock<IWalletWrapper> _walletWrapper = new();
    private readonly Mock<ITransactionToTrackRepository> _transactionToTrackRepository = new();

    [Fact]
    public async Task TryGetTransactionInfoAsync_WhenTransactionIdIsEmpty_ReturnsNull()
    {
        var sut = CreateSut();

        var result = await sut.TryGetTransactionInfoAsync(string.Empty);

        Assert.Null(result);
        _walletWrapper.Verify(x => x.GetTransactionAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task TryGetTransactionInfoAsync_WhenWrapperThrows_ReturnsNull()
    {
        _walletWrapper
            .Setup(x => x.GetTransactionAsync("tx-1"))
            .ThrowsAsync(new InvalidOperationException("failed"));
        var sut = CreateSut();

        var result = await sut.TryGetTransactionInfoAsync("tx-1");

        Assert.Null(result);
    }

    [Fact]
    public async Task StartTransactionAsync_WhenRequestIsInvalid_ThrowsServiceException()
    {
        var request = new TransactionRequest(
            drawId: string.Empty,
            amount: 100,
            currency: Currency.USD,
            transactionType: SharedTransactionType.Unknown,
            gameType: GameType.Undefined);
        var sut = CreateSut();

        await Assert.ThrowsAsync<ServiceException>(() => sut.StartTransactionAsync(request));
    }

    [Fact]
    public async Task StopTrackTransactionAsync_WhenTransactionIdIsEmpty_DoesNothing()
    {
        var sut = CreateSut();

        await sut.StopTrackTransactionAsync(null);

        _transactionToTrackRepository.Verify(x => x.DeleteTransactonAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task StartTransactionAsync_WhenValidPaymentRequest_StartsAndTracksTransaction()
    {
        var currentAccountAccessor = new Mock<Defender.Common.Interfaces.ICurrentAccountAccessor>();
        var lotteryTicketRepository = new Mock<Defender.RiskGamesService.Application.Common.Interfaces.Repositories.Lottery.ILotteryUserTicketRepository>();
        var userId = Guid.NewGuid();
        currentAccountAccessor.Setup(x => x.GetAccountId()).Returns(userId);
        _walletWrapper
            .Setup(x => x.StartPaymentTransactionAsync(userId, 150, Currency.USD, GameType.Lottery, "comment"))
            .ReturnsAsync(new TransactionModel
            {
                TransactionId = "tx-100",
                TransactionPurpose = SharedTransactionPurpose.Lottery
            });
        _transactionToTrackRepository
            .Setup(x => x.CreateTransactionAsync(It.IsAny<TransactionToTrack>()))
            .ReturnsAsync(new TransactionToTrack());
        var sut = CreateSutWithHandlers(currentAccountAccessor.Object, lotteryTicketRepository.Object);
        var request = new TransactionRequest("draw-1", 150, Currency.USD, SharedTransactionType.Payment, GameType.Lottery, "comment");

        var result = await sut.StartTransactionAsync(request);
        await result.CreateTransactionToTrackTask;

        Assert.Equal("tx-100", result.Transaction.TransactionId);
        _transactionToTrackRepository.Verify(x => x.CreateTransactionAsync(It.IsAny<TransactionToTrack>()), Times.Once);
    }

    [Fact]
    public async Task CheckUnhandledTicketsForDrawAsync_WhenTransactionExists_StopsTracking()
    {
        var currentAccountAccessor = new Mock<Defender.Common.Interfaces.ICurrentAccountAccessor>();
        var lotteryTicketRepository = new Mock<Defender.RiskGamesService.Application.Common.Interfaces.Repositories.Lottery.ILotteryUserTicketRepository>();
        var trackedTransaction = new TransactionToTrack
        {
            TransactionId = "tx-200",
            DrawId = "draw-2",
            GameType = GameType.Lottery
        };
        _transactionToTrackRepository
            .Setup(x => x.GetTransactionsAsync("draw-2", GameType.Lottery))
            .ReturnsAsync([trackedTransaction]);
        _walletWrapper
            .Setup(x => x.GetTransactionAsync("tx-200"))
            .ReturnsAsync(new AnonymousTransactionModel
            {
                TransactionId = "tx-200",
                TransactionType = SharedTransactionType.Payment,
                TransactionPurpose = SharedTransactionPurpose.Lottery,
                TransactionStatus = SharedTransactionStatus.Failed
            });
        _transactionToTrackRepository
            .Setup(x => x.GetTransactionAsync("tx-200"))
            .ReturnsAsync(trackedTransaction);
        _transactionToTrackRepository
            .Setup(x => x.DeleteTransactonAsync("tx-200"))
            .Returns(Task.CompletedTask);
        lotteryTicketRepository
            .Setup(x => x.DeleteTicketByPaymentTransactionIdAsync("tx-200"))
            .Returns(Task.CompletedTask);
        var sut = CreateSutWithHandlers(currentAccountAccessor.Object, lotteryTicketRepository.Object);

        await sut.CheckUnhandledTicketsForDrawAsync("draw-2", GameType.Lottery);

        _transactionToTrackRepository.Verify(x => x.DeleteTransactonAsync("tx-200"), Times.AtLeastOnce);
    }

    private TransactionManagementService CreateSut()
    {
        var serviceProvider = new Mock<IServiceProvider>();
        var transactionHandlerFactory = new TransactionHandlerFactory(serviceProvider.Object);

        return new TransactionManagementService(
            _walletWrapper.Object,
            _transactionToTrackRepository.Object,
            transactionHandlerFactory);
    }

    private TransactionManagementService CreateSutWithHandlers(
        Defender.Common.Interfaces.ICurrentAccountAccessor currentAccountAccessor,
        Defender.RiskGamesService.Application.Common.Interfaces.Repositories.Lottery.ILotteryUserTicketRepository lotteryUserTicketRepository)
    {
        var services = new ServiceCollection();
        services.AddSingleton(currentAccountAccessor);
        services.AddSingleton(_walletWrapper.Object);
        services.AddSingleton(lotteryUserTicketRepository);
        services.AddTransient<StartPaymentTransactionHandler>();
        services.AddTransient<StartRechargeTransactionHandler>();
        services.AddTransient<LotteryTransactionHandler>();
        var serviceProvider = services.BuildServiceProvider();
        var factory = new TransactionHandlerFactory(serviceProvider);

        return new TransactionManagementService(
            _walletWrapper.Object,
            _transactionToTrackRepository.Object,
            factory);
    }
}
