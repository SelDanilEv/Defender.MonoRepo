using Defender.Common.DB.SharedStorage.Enums;
using Defender.Common.Errors;
using Defender.WalletService.Application.Common.Interfaces.Services;
using Defender.WalletService.Application.Services;
using Defender.WalletService.Domain.Entities.Transactions;
using Defender.WalletService.Domain.Entities.Wallets;
using Defender.WalletService.Domain.Enums;
using Moq;
using MongoDB.Driver;

namespace Defender.WalletService.Tests.Services;

public class TransactionProcessingServiceTests
{
    private static IClientSessionHandle CreateSession()
    {
        var session = new Mock<IClientSessionHandle>();
        session.Setup(s => s.StartTransaction(It.IsAny<TransactionOptions>()));
        session.Setup(s => s.CommitTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        session.Setup(s => s.AbortTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        return session.Object;
    }

    [Fact]
    public async Task ProcessTransaction_WhenTransactionIdEmpty_ReturnsTrue()
    {
        var sut = new TransactionProcessingService(
            Mock.Of<ITransactionManagementService>(),
            Mock.Of<IWalletManagementService>());

        var result = await sut.ProcessTransaction(string.Empty);

        Assert.True(result);
    }

    [Fact]
    public async Task ProcessTransaction_WhenStatusIsNotQueued_ReturnsTrueWithoutWalletSession()
    {
        var transactionService = new Mock<ITransactionManagementService>();
        transactionService
            .Setup(s => s.GetTransactionByTransactionIdAsync("TX-1"))
            .ReturnsAsync(new Transaction { TransactionId = "TX-1", TransactionStatus = TransactionStatus.Proceed });

        var walletService = new Mock<IWalletManagementService>(MockBehavior.Strict);
        var sut = new TransactionProcessingService(transactionService.Object, walletService.Object);

        var result = await sut.ProcessTransaction("TX-1");

        Assert.True(result);
        walletService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ProcessTransaction_WhenTypeUnknown_UpdatesFailedAndReturnsTrue()
    {
        var tx = new Transaction
        {
            TransactionId = "TX-2",
            TransactionStatus = TransactionStatus.Queued,
            TransactionType = (TransactionType)999
        };

        var transactionService = new Mock<ITransactionManagementService>();
        transactionService
            .Setup(s => s.GetTransactionByTransactionIdAsync("TX-2"))
            .ReturnsAsync(tx);
        transactionService
            .Setup(s => s.UpdateTransactionStatusAsync(tx, TransactionStatus.Failed, ErrorCode.UnhandledError))
            .ReturnsAsync(new Transaction { TransactionId = "TX-2", TransactionStatus = TransactionStatus.Failed });

        var walletService = new Mock<IWalletManagementService>();
        walletService.Setup(w => w.OpenWalletUpdateSessionAsync()).ReturnsAsync(Mock.Of<IClientSessionHandle>());

        var sut = new TransactionProcessingService(transactionService.Object, walletService.Object);

        var result = await sut.ProcessTransaction("TX-2");

        Assert.True(result);
        transactionService.Verify(
            s => s.UpdateTransactionStatusAsync(tx, TransactionStatus.Failed, ErrorCode.UnhandledError),
            Times.Once);
    }

    [Fact]
    public async Task ProcessTransaction_WhenTransferTargetsSameWallet_FailsTransaction()
    {
        var tx = new Transaction
        {
            TransactionId = "TX-3",
            TransactionStatus = TransactionStatus.Queued,
            TransactionType = TransactionType.Transfer,
            FromWallet = 12345678,
            ToWallet = 12345678,
            Currency = Currency.USD,
            Amount = 10
        };

        var transactionService = new Mock<ITransactionManagementService>();
        transactionService.Setup(s => s.GetTransactionByTransactionIdAsync("TX-3")).ReturnsAsync(tx);
        transactionService
            .Setup(s => s.UpdateTransactionStatusAsync(tx, TransactionStatus.Failed, ErrorCode.BR_WLT_SenderAndRecipientAreTheSame))
            .ReturnsAsync(new Transaction { TransactionId = "TX-3", TransactionStatus = TransactionStatus.Failed });

        var walletService = new Mock<IWalletManagementService>();
        walletService.Setup(w => w.OpenWalletUpdateSessionAsync()).ReturnsAsync(Mock.Of<IClientSessionHandle>());

        var sut = new TransactionProcessingService(transactionService.Object, walletService.Object);

        var result = await sut.ProcessTransaction("TX-3");

        Assert.True(result);
        transactionService.Verify(
            s => s.UpdateTransactionStatusAsync(tx, TransactionStatus.Failed, ErrorCode.BR_WLT_SenderAndRecipientAreTheSame),
            Times.Once);
    }

    [Fact]
    public async Task ProcessTransaction_WhenRechargeWalletMissing_FailsTransaction()
    {
        var tx = new Transaction
        {
            TransactionId = "TX-4",
            TransactionStatus = TransactionStatus.Queued,
            TransactionType = TransactionType.Recharge,
            ToWallet = 99999999,
            Currency = Currency.USD,
            Amount = 10
        };

        var transactionService = new Mock<ITransactionManagementService>();
        transactionService.Setup(s => s.GetTransactionByTransactionIdAsync("TX-4")).ReturnsAsync(tx);
        transactionService
            .Setup(s => s.UpdateTransactionStatusAsync(tx, TransactionStatus.Failed, ErrorCode.BR_WLT_WalletIsNotExist))
            .ReturnsAsync(new Transaction { TransactionId = "TX-4", TransactionStatus = TransactionStatus.Failed });

        var walletService = new Mock<IWalletManagementService>();
        walletService.Setup(w => w.OpenWalletUpdateSessionAsync()).ReturnsAsync(Mock.Of<IClientSessionHandle>());
        walletService.Setup(w => w.GetWalletByNumberAsync(99999999)).ReturnsAsync((Wallet)null!);

        var sut = new TransactionProcessingService(transactionService.Object, walletService.Object);

        var result = await sut.ProcessTransaction("TX-4");

        Assert.True(result);
        transactionService.Verify(
            s => s.UpdateTransactionStatusAsync(tx, TransactionStatus.Failed, ErrorCode.BR_WLT_WalletIsNotExist),
            Times.Once);
    }

    [Fact]
    public async Task ProcessTransaction_WhenRechargeValid_Proceeds()
    {
        var tx = new Transaction
        {
            TransactionId = "TX-5",
            TransactionStatus = TransactionStatus.Queued,
            TransactionType = TransactionType.Recharge,
            ToWallet = 45678901,
            Currency = Currency.USD,
            Amount = 10
        };

        var wallet = new Wallet
        {
            Id = Guid.NewGuid(),
            WalletNumber = 45678901,
            CurrencyAccounts = [new CurrencyAccount(Currency.USD, true) { Balance = 5 }]
        };

        var transactionService = new Mock<ITransactionManagementService>();
        transactionService.Setup(s => s.GetTransactionByTransactionIdAsync("TX-5")).ReturnsAsync(tx);
        transactionService.Setup(s => s.UpdateTransactionStatusAsync(tx, TransactionStatus.Proceed, It.IsAny<string>()))
            .ReturnsAsync(new Transaction { TransactionId = "TX-5", TransactionStatus = TransactionStatus.Proceed });

        var session = CreateSession();

        var walletService = new Mock<IWalletManagementService>();
        walletService.Setup(w => w.OpenWalletUpdateSessionAsync()).ReturnsAsync(session);
        walletService.Setup(w => w.GetWalletByNumberAsync(45678901)).ReturnsAsync(wallet);
        walletService
            .Setup(w => w.UpdateCurrencyAccountsAsync(wallet.Id, wallet.CurrencyAccounts, session))
            .ReturnsAsync(wallet);

        var sut = new TransactionProcessingService(transactionService.Object, walletService.Object);

        var result = await sut.ProcessTransaction("TX-5");

        Assert.True(result);
        transactionService.Verify(s => s.UpdateTransactionStatusAsync(tx, TransactionStatus.Proceed, It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task ProcessTransaction_WhenPaymentCurrencyAccountMissing_FailsTransaction()
    {
        var tx = new Transaction
        {
            TransactionId = "TX-6",
            TransactionStatus = TransactionStatus.Queued,
            TransactionType = TransactionType.Payment,
            FromWallet = 11112222,
            Currency = Currency.EUR,
            Amount = 10
        };

        var wallet = new Wallet
        {
            Id = Guid.NewGuid(),
            WalletNumber = 11112222,
            CurrencyAccounts = [new CurrencyAccount(Currency.USD, true)]
        };

        var transactionService = new Mock<ITransactionManagementService>();
        transactionService.Setup(s => s.GetTransactionByTransactionIdAsync("TX-6")).ReturnsAsync(tx);
        transactionService
            .Setup(s => s.UpdateTransactionStatusAsync(tx, TransactionStatus.Failed, ErrorCode.BR_WLT_SenderCurrencyAccountIsNotExist))
            .ReturnsAsync(new Transaction { TransactionId = "TX-6", TransactionStatus = TransactionStatus.Failed });

        var session = CreateSession();

        var walletService = new Mock<IWalletManagementService>();
        walletService.Setup(w => w.OpenWalletUpdateSessionAsync()).ReturnsAsync(session);
        walletService.Setup(w => w.GetWalletByNumberAsync(11112222)).ReturnsAsync(wallet);

        var sut = new TransactionProcessingService(transactionService.Object, walletService.Object);

        var result = await sut.ProcessTransaction("TX-6");

        Assert.True(result);
        transactionService.Verify(
            s => s.UpdateTransactionStatusAsync(tx, TransactionStatus.Failed, ErrorCode.BR_WLT_SenderCurrencyAccountIsNotExist),
            Times.Once);
    }

    [Fact]
    public async Task ProcessTransaction_WhenTransferValid_UpdatesBothWalletsAndProceeds()
    {
        var tx = new Transaction
        {
            TransactionId = "TX-7",
            TransactionStatus = TransactionStatus.Queued,
            TransactionType = TransactionType.Transfer,
            FromWallet = 11110000,
            ToWallet = 22220000,
            Currency = Currency.USD,
            Amount = 20
        };

        var fromWallet = new Wallet
        {
            Id = Guid.NewGuid(),
            WalletNumber = 11110000,
            CurrencyAccounts = [new CurrencyAccount(Currency.USD, true) { Balance = 100 }]
        };
        var toWallet = new Wallet
        {
            Id = Guid.NewGuid(),
            WalletNumber = 22220000,
            CurrencyAccounts = [new CurrencyAccount(Currency.USD, true) { Balance = 5 }]
        };

        var transactionService = new Mock<ITransactionManagementService>();
        transactionService.Setup(s => s.GetTransactionByTransactionIdAsync("TX-7")).ReturnsAsync(tx);
        transactionService.Setup(s => s.UpdateTransactionStatusAsync(tx, TransactionStatus.Proceed, It.IsAny<string>()))
            .ReturnsAsync(new Transaction { TransactionId = "TX-7", TransactionStatus = TransactionStatus.Proceed });

        var session = CreateSession();
        var walletService = new Mock<IWalletManagementService>();
        walletService.Setup(w => w.OpenWalletUpdateSessionAsync()).ReturnsAsync(session);
        walletService.Setup(w => w.GetWalletByNumberAsync(11110000)).ReturnsAsync(fromWallet);
        walletService.Setup(w => w.GetWalletByNumberAsync(22220000)).ReturnsAsync(toWallet);
        walletService.Setup(w => w.UpdateCurrencyAccountsAsync(fromWallet.Id, fromWallet.CurrencyAccounts, session))
            .ReturnsAsync(fromWallet);
        walletService.Setup(w => w.UpdateCurrencyAccountsAsync(toWallet.Id, toWallet.CurrencyAccounts, session))
            .ReturnsAsync(toWallet);

        var sut = new TransactionProcessingService(transactionService.Object, walletService.Object);

        var result = await sut.ProcessTransaction("TX-7");

        Assert.True(result);
        Assert.Equal(80, fromWallet.GetCurrencyAccount(Currency.USD).Balance);
        Assert.Equal(25, toWallet.GetCurrencyAccount(Currency.USD).Balance);
    }

    [Fact]
    public async Task ProcessTransaction_WhenPaymentValid_Proceeds()
    {
        var tx = new Transaction
        {
            TransactionId = "TX-8",
            TransactionStatus = TransactionStatus.Queued,
            TransactionType = TransactionType.Payment,
            FromWallet = 77770000,
            Currency = Currency.EUR,
            Amount = 10
        };

        var fromWallet = new Wallet
        {
            Id = Guid.NewGuid(),
            WalletNumber = 77770000,
            CurrencyAccounts = [new CurrencyAccount(Currency.EUR, true) { Balance = 20 }]
        };

        var transactionService = new Mock<ITransactionManagementService>();
        transactionService.Setup(s => s.GetTransactionByTransactionIdAsync("TX-8")).ReturnsAsync(tx);
        transactionService.Setup(s => s.UpdateTransactionStatusAsync(tx, TransactionStatus.Proceed, It.IsAny<string>()))
            .ReturnsAsync(new Transaction { TransactionId = "TX-8", TransactionStatus = TransactionStatus.Proceed });

        var session = CreateSession();
        var walletService = new Mock<IWalletManagementService>();
        walletService.Setup(w => w.OpenWalletUpdateSessionAsync()).ReturnsAsync(session);
        walletService.Setup(w => w.GetWalletByNumberAsync(77770000)).ReturnsAsync(fromWallet);
        walletService.Setup(w => w.UpdateCurrencyAccountsAsync(fromWallet.Id, fromWallet.CurrencyAccounts, session))
            .ReturnsAsync(fromWallet);

        var sut = new TransactionProcessingService(transactionService.Object, walletService.Object);

        var result = await sut.ProcessTransaction("TX-8");

        Assert.True(result);
        Assert.Equal(10, fromWallet.GetCurrencyAccount(Currency.EUR).Balance);
    }

    [Fact]
    public async Task ProcessTransaction_WhenRevertValid_RevertsOriginalTransaction()
    {
        var tx = new Transaction
        {
            TransactionId = "TX-9",
            ParentTransactionId = "PARENT-1",
            TransactionStatus = TransactionStatus.Queued,
            TransactionType = TransactionType.Revert,
            FromWallet = 55550000,
            ToWallet = 66660000,
            Currency = Currency.USD,
            Amount = 5
        };

        var original = new Transaction
        {
            TransactionId = "PARENT-1",
            TransactionStatus = TransactionStatus.Proceed
        };

        var fromWallet = new Wallet
        {
            Id = Guid.NewGuid(),
            WalletNumber = 55550000,
            CurrencyAccounts = [new CurrencyAccount(Currency.USD, true) { Balance = 15 }]
        };
        var toWallet = new Wallet
        {
            Id = Guid.NewGuid(),
            WalletNumber = 66660000,
            CurrencyAccounts = [new CurrencyAccount(Currency.USD, true) { Balance = 10 }]
        };

        var transactionService = new Mock<ITransactionManagementService>();
        transactionService.Setup(s => s.GetTransactionByTransactionIdAsync("TX-9")).ReturnsAsync(tx);
        transactionService.Setup(s => s.GetTransactionByTransactionIdAsync("PARENT-1")).ReturnsAsync(original);
        transactionService.Setup(s => s.UpdateTransactionStatusAsync(tx, TransactionStatus.Proceed, It.IsAny<string>()))
            .ReturnsAsync(new Transaction { TransactionId = "TX-9", TransactionStatus = TransactionStatus.Proceed });
        transactionService.Setup(s => s.UpdateTransactionStatusAsync(original, TransactionStatus.Reverted, It.IsAny<string>()))
            .ReturnsAsync(new Transaction { TransactionId = "PARENT-1", TransactionStatus = TransactionStatus.Reverted });

        var session = CreateSession();
        var walletService = new Mock<IWalletManagementService>();
        walletService.Setup(w => w.OpenWalletUpdateSessionAsync()).ReturnsAsync(session);
        walletService.Setup(w => w.GetWalletByNumberAsync(55550000)).ReturnsAsync(fromWallet);
        walletService.Setup(w => w.GetWalletByNumberAsync(66660000)).ReturnsAsync(toWallet);
        walletService.Setup(w => w.UpdateCurrencyAccountsAsync(fromWallet.Id, fromWallet.CurrencyAccounts, session))
            .ReturnsAsync(fromWallet);
        walletService.Setup(w => w.UpdateCurrencyAccountsAsync(toWallet.Id, toWallet.CurrencyAccounts, session))
            .ReturnsAsync(toWallet);

        var sut = new TransactionProcessingService(transactionService.Object, walletService.Object);

        var result = await sut.ProcessTransaction("TX-9");

        Assert.True(result);
        transactionService.Verify(s => s.UpdateTransactionStatusAsync(original, TransactionStatus.Reverted, It.IsAny<string>()), Times.Once);
    }
}
