using Defender.Common.DB.SharedStorage.Enums;
using Defender.Common.Errors;
using Defender.Common.Exceptions;
using Defender.Common.Interfaces;
using Defender.WalletService.Application.Common.Interfaces.Services;
using Defender.WalletService.Application.Modules.Transactions.Commands;
using Defender.WalletService.Application.Modules.Transactions.Queries;
using Defender.WalletService.Domain.Entities.Transactions;
using Defender.WalletService.Domain.Entities.Wallets;
using Defender.WalletService.Domain.Enums;
using Moq;

namespace Defender.WalletService.Tests.Modules;

public class TransactionHandlersTests
{
    [Fact]
    public void StartTransferTransactionCommandValidator_InvalidPayload_Fails()
    {
        var validator = new StartTransferTransactionCommandValidator();
        var result = validator.Validate(new StartTransferTransactionCommand
        {
            TargetWalletNumber = 0,
            Amount = 0
        });

        Assert.False(result.IsValid);
    }

    [Fact]
    public void StartTransferTransactionCommandValidator_TargetUserPresent_SkipsWalletValidation()
    {
        var validator = new StartTransferTransactionCommandValidator();
        var result = validator.Validate(new StartTransferTransactionCommand
        {
            TargetUserId = Guid.NewGuid(),
            TargetWalletNumber = 0,
            Amount = 10
        });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void StartPaymentTransactionCommandValidator_InvalidPayload_Fails()
    {
        var validator = new StartPaymentTransactionCommandValidator();
        var result = validator.Validate(new StartPaymentTransactionCommand
        {
            TargetWalletNumber = 0,
            Amount = -5
        });

        Assert.False(result.IsValid);
    }

    [Fact]
    public void StartRechargeTransactionCommandValidator_InvalidPayload_Fails()
    {
        var validator = new StartRechargeTransactionCommandValidator();
        var result = validator.Validate(new StartRechargeTransactionCommand
        {
            TargetWalletNumber = 0,
            Amount = 0
        });

        Assert.False(result.IsValid);
    }

    [Fact]
    public void CancelTransactionCommandValidator_RequiresTransactionId()
    {
        var validator = new StartCancelationTransactionCommandValidator();
        var result = validator.Validate(new CancelTransactionCommand());

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task StartTransferTransactionCommandHandler_UsesCurrentAndTargetWalletNumbers()
    {
        var currentUser = Guid.NewGuid();
        var currentWallet = new Wallet { Id = currentUser, WalletNumber = 11111111 };
        var targetWallet = new Wallet { Id = Guid.NewGuid(), WalletNumber = 22222222 };

        var accountAccessor = new Mock<ICurrentAccountAccessor>();
        accountAccessor.Setup(a => a.GetAccountId()).Returns(currentUser);

        var walletService = new Mock<IWalletManagementService>();
        walletService.Setup(w => w.GetWalletByUserIdAsync(currentUser)).ReturnsAsync(currentWallet);
        walletService.Setup(w => w.GetWalletByNumberAsync(22222222)).ReturnsAsync(targetWallet);

        var transactionService = new Mock<ITransactionManagementService>();
        transactionService.Setup(t => t.CreateTransferTransactionAsync(
                11111111,
                It.Is<Transaction.CreateTransactionRequest>(r => r.TargetWallet == 22222222 && r.Amount == 25 && r.Currency == Currency.USD)))
            .ReturnsAsync(new Transaction { TransactionType = TransactionType.Transfer, FromWallet = 11111111, ToWallet = 22222222 });

        var handler = new StartTransferTransactionCommandHandler(accountAccessor.Object, transactionService.Object, walletService.Object);
        var request = new StartTransferTransactionCommand { TargetWalletNumber = 22222222, Amount = 25, Currency = Currency.USD };

        var result = await handler.Handle(request, CancellationToken.None);

        Assert.Equal(TransactionType.Transfer, result.TransactionType);
        Assert.Equal(11111111, result.FromWallet);
        Assert.Equal(22222222, result.ToWallet);
    }

    [Fact]
    public async Task StartTransferTransactionCommandHandler_WhenTargetMissing_Throws()
    {
        var currentUser = Guid.NewGuid();
        var accountAccessor = new Mock<ICurrentAccountAccessor>();
        accountAccessor.Setup(a => a.GetAccountId()).Returns(currentUser);

        var walletService = new Mock<IWalletManagementService>();
        walletService.Setup(w => w.GetWalletByUserIdAsync(currentUser)).ReturnsAsync(new Wallet { Id = currentUser, WalletNumber = 11111111 });
        walletService.Setup(w => w.GetWalletByNumberAsync(22222222)).ReturnsAsync((Wallet)null!);

        var handler = new StartTransferTransactionCommandHandler(
            accountAccessor.Object,
            Mock.Of<ITransactionManagementService>(),
            walletService.Object);

        await Assert.ThrowsAsync<ServiceException>(() =>
            handler.Handle(new StartTransferTransactionCommand { TargetWalletNumber = 22222222, Amount = 10, Currency = Currency.USD }, CancellationToken.None));
    }

    [Fact]
    public async Task StartRechargeTransactionCommandHandler_ResolvesWalletByUserId()
    {
        var targetUserId = Guid.NewGuid();
        var walletService = new Mock<IWalletManagementService>();
        walletService.Setup(w => w.GetWalletByUserIdAsync(targetUserId))
            .ReturnsAsync(new Wallet { Id = targetUserId, WalletNumber = 33333333 });

        var transactionService = new Mock<ITransactionManagementService>();
        transactionService
            .Setup(t => t.CreateRechargeTransactionAsync(It.Is<Transaction.CreateTransactionRequest>(r => r.TargetWallet == 33333333)))
            .ReturnsAsync(new Transaction { TransactionType = TransactionType.Recharge, ToWallet = 33333333 });

        var handler = new StartRechargeTransactionCommandHandler(transactionService.Object, walletService.Object);
        var command = new StartRechargeTransactionCommand { TargetUserId = targetUserId, Amount = 50, Currency = Currency.EUR };

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.Equal(TransactionType.Recharge, result.TransactionType);
        Assert.Equal(33333333, result.ToWallet);
    }

    [Fact]
    public async Task StartPaymentTransactionCommandHandler_WhenSameWallet_SkipsAuthCheck()
    {
        var currentUserId = Guid.NewGuid();
        var currentWallet = new Wallet { Id = currentUserId, WalletNumber = 44444444 };

        var auth = new Mock<IAuthorizationCheckingService>(MockBehavior.Strict);
        var accessor = new Mock<ICurrentAccountAccessor>();
        accessor.Setup(a => a.GetAccountId()).Returns(currentUserId);

        var walletService = new Mock<IWalletManagementService>();
        walletService.Setup(w => w.GetWalletByUserIdAsync(currentUserId)).ReturnsAsync(currentWallet);

        var transactionService = new Mock<ITransactionManagementService>();
        transactionService.Setup(t => t.CreatePaymentTransactionAsync(It.IsAny<Transaction.CreateTransactionRequest>()))
            .ReturnsAsync(new Transaction { TransactionType = TransactionType.Payment, FromWallet = 44444444 });

        var handler = new StartPaymentTransactionCommandHandler(auth.Object, accessor.Object, transactionService.Object, walletService.Object);
        var request = new StartPaymentTransactionCommand { TargetWalletNumber = 44444444, Amount = 10, Currency = Currency.USD };

        var result = await handler.Handle(request, CancellationToken.None);

        Assert.Equal(TransactionType.Payment, result.TransactionType);
        auth.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task StartPaymentTransactionCommandHandler_WhenDifferentWallet_UsesAuthCheck()
    {
        var currentUserId = Guid.NewGuid();
        var targetWallet = new Wallet { Id = Guid.NewGuid(), WalletNumber = 55555555 };

        var auth = new Mock<IAuthorizationCheckingService>();
        auth.Setup(a => a.ExecuteWithAuthCheckAsync(
                targetWallet.Id,
                It.IsAny<Func<Task<Transaction>>>(),
                false,
                ErrorCode.CM_ForbiddenAccess))
            .Returns<Guid, Func<Task<Transaction>>, bool, ErrorCode>((_, action, _, _) => action());

        var accessor = new Mock<ICurrentAccountAccessor>();
        accessor.Setup(a => a.GetAccountId()).Returns(currentUserId);

        var walletService = new Mock<IWalletManagementService>();
        walletService.Setup(w => w.GetWalletByUserIdAsync(currentUserId))
            .ReturnsAsync(new Wallet { Id = currentUserId, WalletNumber = 11112222 });
        walletService.Setup(w => w.GetWalletByNumberAsync(55555555)).ReturnsAsync(targetWallet);

        var transactionService = new Mock<ITransactionManagementService>();
        transactionService.Setup(t => t.CreatePaymentTransactionAsync(It.Is<Transaction.CreateTransactionRequest>(r => r.TargetWallet == 55555555)))
            .ReturnsAsync(new Transaction { TransactionType = TransactionType.Payment, FromWallet = 55555555 });

        var handler = new StartPaymentTransactionCommandHandler(auth.Object, accessor.Object, transactionService.Object, walletService.Object);

        var result = await handler.Handle(new StartPaymentTransactionCommand { TargetWalletNumber = 55555555, Amount = 15, Currency = Currency.USD }, CancellationToken.None);

        Assert.Equal(TransactionType.Payment, result.TransactionType);
        auth.VerifyAll();
    }

    [Fact]
    public async Task StartPaymentTransactionCommandHandler_WhenTargetUserSpecified_ResolvesWalletByUserId()
    {
        var currentUserId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();
        var targetWallet = new Wallet { Id = targetUserId, WalletNumber = 77776666 };

        var auth = new Mock<IAuthorizationCheckingService>();
        auth.Setup(a => a.ExecuteWithAuthCheckAsync(
                targetUserId,
                It.IsAny<Func<Task<Transaction>>>(),
                false,
                ErrorCode.CM_ForbiddenAccess))
            .Returns<Guid, Func<Task<Transaction>>, bool, ErrorCode>((_, action, _, _) => action());

        var accessor = new Mock<ICurrentAccountAccessor>();
        accessor.Setup(a => a.GetAccountId()).Returns(currentUserId);

        var walletService = new Mock<IWalletManagementService>();
        walletService.Setup(w => w.GetWalletByUserIdAsync(currentUserId))
            .ReturnsAsync(new Wallet { Id = currentUserId, WalletNumber = 11112222 });
        walletService.Setup(w => w.GetWalletByUserIdAsync(targetUserId)).ReturnsAsync(targetWallet);

        var transactionService = new Mock<ITransactionManagementService>();
        transactionService.Setup(t => t.CreatePaymentTransactionAsync(It.Is<Transaction.CreateTransactionRequest>(r => r.TargetWallet == 77776666)))
            .ReturnsAsync(new Transaction { TransactionType = TransactionType.Payment, FromWallet = 77776666 });

        var handler = new StartPaymentTransactionCommandHandler(auth.Object, accessor.Object, transactionService.Object, walletService.Object);

        var result = await handler.Handle(new StartPaymentTransactionCommand { TargetUserId = targetUserId, Amount = 33, Currency = Currency.USD }, CancellationToken.None);

        Assert.Equal(TransactionType.Payment, result.TransactionType);
    }

    [Fact]
    public async Task StartPaymentTransactionCommandHandler_WhenTargetUserWalletMissing_Throws()
    {
        var currentUserId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();

        var accessor = new Mock<ICurrentAccountAccessor>();
        accessor.Setup(a => a.GetAccountId()).Returns(currentUserId);

        var walletService = new Mock<IWalletManagementService>();
        walletService.Setup(w => w.GetWalletByUserIdAsync(currentUserId))
            .ReturnsAsync(new Wallet { Id = currentUserId, WalletNumber = 11112222 });
        walletService.Setup(w => w.GetWalletByUserIdAsync(targetUserId)).ReturnsAsync((Wallet)null!);

        var handler = new StartPaymentTransactionCommandHandler(
            Mock.Of<IAuthorizationCheckingService>(),
            accessor.Object,
            Mock.Of<ITransactionManagementService>(),
            walletService.Object);

        await Assert.ThrowsAsync<ServiceException>(() =>
            handler.Handle(new StartPaymentTransactionCommand { TargetUserId = targetUserId, Amount = 1, Currency = Currency.USD }, CancellationToken.None));
    }

    [Fact]
    public async Task CancelTransactionCommandHandler_DelegatesToService()
    {
        var transaction = new Transaction { TransactionId = "TX-CANCEL" };
        var service = new Mock<ITransactionManagementService>();
        service.Setup(s => s.CancelTransactionAsync("TX-CANCEL")).ReturnsAsync(transaction);
        var handler = new StartCancelationTransactionCommandHandler(service.Object);

        var result = await handler.Handle(new CancelTransactionCommand { TransactionId = "TX-CANCEL" }, CancellationToken.None);

        Assert.Equal("TX-CANCEL", result.TransactionId);
    }

    [Fact]
    public async Task GetUserTransactionByTransactionIdQueryHandler_WhenMissing_ThrowsNotFound()
    {
        var service = new Mock<ITransactionManagementService>();
        service.Setup(s => s.GetTransactionByTransactionIdAsync("MISSING")).ReturnsAsync((Transaction)null!);
        var handler = new GetUserTransactionByTransactionIdQueryHandler(service.Object);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new GetUserTransactionByTransactionIdQuery { TransactionId = "MISSING" }, CancellationToken.None));
    }

    [Fact]
    public async Task GetUserTransactionByTransactionIdQueryHandler_WhenFound_ReturnsTransaction()
    {
        var expected = new Transaction { TransactionId = "FOUND" };
        var service = new Mock<ITransactionManagementService>();
        service.Setup(s => s.GetTransactionByTransactionIdAsync("FOUND")).ReturnsAsync(expected);
        var handler = new GetUserTransactionByTransactionIdQueryHandler(service.Object);

        var result = await handler.Handle(new GetUserTransactionByTransactionIdQuery { TransactionId = "FOUND" }, CancellationToken.None);

        Assert.Same(expected, result);
    }
}
