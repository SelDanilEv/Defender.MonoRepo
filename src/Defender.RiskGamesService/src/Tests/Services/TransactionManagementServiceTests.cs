using Defender.Common.Exceptions;
using Defender.RiskGamesService.Application.Common.Interfaces.Repositories.Transactions;
using Defender.RiskGamesService.Application.Common.Interfaces.Wrapper;
using Defender.RiskGamesService.Application.Factories.Transaction;
using Defender.RiskGamesService.Application.Models.Transaction;
using Defender.RiskGamesService.Application.Services.Transaction;
using Defender.RiskGamesService.Domain.Enums;

using SharedTransactionType = Defender.Common.DB.SharedStorage.Enums.TransactionType;

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

    private TransactionManagementService CreateSut()
    {
        var serviceProvider = new Mock<IServiceProvider>();
        var transactionHandlerFactory = new TransactionHandlerFactory(serviceProvider.Object);

        return new TransactionManagementService(
            _walletWrapper.Object,
            _transactionToTrackRepository.Object,
            transactionHandlerFactory);
    }
}
