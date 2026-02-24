using Defender.Common.DB.Pagination;
using Defender.Common.DB.SharedStorage.Enums;
using Defender.Common.Errors;
using Defender.Common.Exceptions;
using Defender.Common.Interfaces;
using Defender.WalletService.Application.Common.Interfaces.Services;
using Defender.WalletService.Application.Modules.Transactions.Commands;
using Defender.WalletService.Application.Modules.Transactions.Queries;
using Defender.WalletService.Application.Modules.Wallets.Commands;
using Defender.WalletService.Application.Modules.Wallets.Queries;
using Defender.WalletService.Domain.Consts;
using Defender.WalletService.Domain.Entities.Transactions;
using Defender.WalletService.Domain.Entities.Wallets;
using Defender.WalletService.Domain.Enums;
using Moq;

namespace Defender.WalletService.Tests.Modules;

public class WalletHandlersAndDomainTests
{
    [Fact]
    public void GetWalletInfoByNumberQueryValidator_InvalidWalletNumber_Fails()
    {
        var validator = new GetWalletInfoByNumberQueryValidator();
        var result = validator.Validate(new GetWalletInfoByNumberQuery { WalletNumber = 1 });

        Assert.False(result.IsValid);
    }

    [Fact]
    public void GetUserTransactionByTransactionIdQueryValidator_EmptyTransactionId_Fails()
    {
        var validator = new GetUserTransactionByTransactionIdQueryValidator();
        var result = validator.Validate(new GetUserTransactionByTransactionIdQuery());

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task GetOrCreateWalletCommandHandler_WhenWalletExists_ReturnsExisting()
    {
        var userId = Guid.NewGuid();
        var existingWallet = new Wallet { Id = userId, WalletNumber = 99999999 };

        var auth = new Mock<IAuthorizationCheckingService>();
        auth.Setup(a => a.ExecuteWithAuthCheckAsync(
                userId,
                It.IsAny<Func<Task<Wallet>>>(),
                false,
                ErrorCode.CM_ForbiddenAccess))
            .Returns<Guid, Func<Task<Wallet>>, bool, ErrorCode>((_, action, _, _) => action());

        var account = new Mock<ICurrentAccountAccessor>();
        account.Setup(a => a.GetAccountId()).Returns(userId);

        var walletService = new Mock<IWalletManagementService>();
        walletService.Setup(s => s.GetWalletByUserIdAsync(userId)).ReturnsAsync(existingWallet);

        var handler = new GetOrCreateWalletCommandHandler(auth.Object, account.Object, walletService.Object);

        var result = await handler.Handle(new GetOrCreateWalletCommand(), CancellationToken.None);

        Assert.Same(existingWallet, result);
        walletService.Verify(s => s.CreateNewWalletAsync(It.IsAny<Guid?>()), Times.Never);
    }

    [Fact]
    public async Task AddCurrencyAccountCommandHandler_UsesCurrentUser()
    {
        var userId = Guid.NewGuid();
        var account = new Mock<ICurrentAccountAccessor>();
        account.Setup(a => a.GetAccountId()).Returns(userId);

        var walletService = new Mock<IWalletManagementService>();
        walletService
            .Setup(s => s.AddCurrencyAccountAsync(userId, Currency.EUR, true))
            .ReturnsAsync(new Wallet { Id = userId });

        var handler = new AddCurrencyAccountCommandHandler(account.Object, walletService.Object);

        var result = await handler.Handle(new AddCurrencyAccountCommand { Currency = Currency.EUR, IsDefault = true }, CancellationToken.None);

        Assert.Equal(userId, result.Id);
    }

    [Fact]
    public async Task SetDefaultCurrencyAccountCommandHandler_UsesCurrentUser()
    {
        var userId = Guid.NewGuid();
        var account = new Mock<ICurrentAccountAccessor>();
        account.Setup(a => a.GetAccountId()).Returns(userId);

        var walletService = new Mock<IWalletManagementService>();
        walletService
            .Setup(s => s.SetDefaultCurrencyAccountAsync(userId, Currency.USD))
            .ReturnsAsync(new Wallet { Id = userId });

        var handler = new SetDefaultCurrencyAccountCommandHandler(account.Object, walletService.Object);

        var result = await handler.Handle(new SetDefaultCurrencyAccountCommand { Currency = Currency.USD }, CancellationToken.None);

        Assert.Equal(userId, result.Id);
    }

    [Fact]
    public async Task GetWalletInfoByNumberQueryHandler_WhenMissing_ThrowsServiceException()
    {
        var walletService = new Mock<IWalletManagementService>();
        walletService.Setup(s => s.GetWalletByNumberAsync(12345678)).ReturnsAsync((Wallet)null!);
        var handler = new GetWalletInfoByNumberQueryHandler(walletService.Object);

        await Assert.ThrowsAsync<ServiceException>(() =>
            handler.Handle(new GetWalletInfoByNumberQuery { WalletNumber = 12345678 }, CancellationToken.None));
    }

    [Fact]
    public async Task GetWalletInfoByNumberQueryHandler_WhenExists_ReturnsWallet()
    {
        var wallet = new Wallet { WalletNumber = 12345678 };
        var walletService = new Mock<IWalletManagementService>();
        walletService.Setup(s => s.GetWalletByNumberAsync(12345678)).ReturnsAsync(wallet);
        var handler = new GetWalletInfoByNumberQueryHandler(walletService.Object);

        var result = await handler.Handle(new GetWalletInfoByNumberQuery { WalletNumber = 12345678 }, CancellationToken.None);

        Assert.Same(wallet, result);
    }

    [Fact]
    public async Task GetTransactionHistoryQueryHandler_WithWalletId_UsesAuthCheckAndWalletNumber()
    {
        var walletId = Guid.NewGuid();
        var wallet = new Wallet { Id = walletId, WalletNumber = 77778888 };
        var expected = new PagedResult<Transaction> { TotalItemsCount = 1, CurrentPage = 1, PageSize = 10 };

        var auth = new Mock<IAuthorizationCheckingService>();
        auth.Setup(a => a.ExecuteWithAuthCheckAsync(
                walletId,
                It.IsAny<Func<Task<PagedResult<Transaction>>>>(),
                false,
                ErrorCode.CM_ForbiddenAccess))
            .Returns<Guid, Func<Task<PagedResult<Transaction>>>, bool, ErrorCode>((_, action, _, _) => action());

        var transactionService = new Mock<ITransactionManagementService>();
        transactionService.Setup(s => s.GetTransactionsByWalletNumberAsync(It.IsAny<PaginationRequest>(), 77778888))
            .ReturnsAsync(expected);

        var walletService = new Mock<IWalletManagementService>();
        walletService.Setup(w => w.GetWalletByUserIdAsync(walletId)).ReturnsAsync(wallet);

        var handler = new GetTransactionsQueryHandler(auth.Object, Mock.Of<ICurrentAccountAccessor>(), transactionService.Object, walletService.Object);

        var result = await handler.Handle(new GetTransactionHistoryQuery { WalletId = walletId }, CancellationToken.None);

        Assert.Same(expected, result);
    }

    [Fact]
    public void TransactionFactoryMethods_CreateExpectedWalletDirections()
    {
        var request = new Transaction.CreateTransactionRequest
        {
            TargetWallet = 88889999,
            Amount = 42,
            Currency = Currency.USD,
            TransactionPurpose = TransactionPurpose.NoPurpose
        };

        var payment = Transaction.CreatePayment(request);
        var recharge = Transaction.CreateRecharge(request);
        var transfer = Transaction.CreateTransfer(request, 11112222);

        Assert.Equal(TransactionType.Payment, payment.TransactionType);
        Assert.Equal(88889999, payment.FromWallet);
        Assert.Equal(ConstantValues.NoWallet, payment.ToWallet);

        Assert.Equal(TransactionType.Recharge, recharge.TransactionType);
        Assert.Equal(88889999, recharge.ToWallet);
        Assert.Equal(ConstantValues.NoWallet, recharge.FromWallet);

        Assert.Equal(TransactionType.Transfer, transfer.TransactionType);
        Assert.Equal(11112222, transfer.FromWallet);
        Assert.Equal(88889999, transfer.ToWallet);
    }

    [Fact]
    public void TransactionCreateCancelation_SwapsWalletsAndReferencesParent()
    {
        var parent = new Transaction
        {
            TransactionId = "TRN-123",
            FromWallet = 11112222,
            ToWallet = 33334444,
            Amount = 12,
            Currency = Currency.EUR,
            TransactionPurpose = TransactionPurpose.NoPurpose,
            Comment = "note"
        };

        var cancellation = Transaction.CreateCancelation(parent);

        Assert.Equal("TRN-123", cancellation.ParentTransactionId);
        Assert.Equal(33334444, cancellation.FromWallet);
        Assert.Equal(11112222, cancellation.ToWallet);
        Assert.Equal(TransactionType.Revert, cancellation.TransactionType);
        Assert.Equal(TransactionStatus.Queued, cancellation.TransactionStatus);
    }

    [Fact]
    public void BaseWalletCurrencyHelpers_WorkAsExpected()
    {
        var wallet = new Wallet
        {
            CurrencyAccounts =
            [
                new CurrencyAccount(Currency.USD, true),
                new CurrencyAccount(Currency.EUR, false)
            ]
        };

        Assert.True(wallet.IsCurrencyAccountExist(Currency.USD));
        Assert.False(wallet.IsCurrencyAccountExist(Currency.PLN));
        Assert.Equal(Currency.USD, wallet.GetDefaultCurrencyAccount().Currency);
        Assert.Equal(Currency.EUR, wallet.GetCurrencyAccount(Currency.EUR).Currency);
    }

    [Fact]
    public void BaseTransactionCommand_CreateTransactionRequest_MapsAllFields()
    {
        var command = new StartRechargeTransactionCommand
        {
            TargetWalletNumber = 12345678,
            Amount = 77,
            Currency = Currency.EUR,
            Comment = "abc",
            TransactionPurpose = TransactionPurpose.NoPurpose
        };

        var request = command.CreateTransactionRequest;

        Assert.Equal(12345678, request.TargetWallet);
        Assert.Equal(77, request.Amount);
        Assert.Equal(Currency.EUR, request.Currency);
        Assert.Equal("abc", request.Comment);
    }
}
