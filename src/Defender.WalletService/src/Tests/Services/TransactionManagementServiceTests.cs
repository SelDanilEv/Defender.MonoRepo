using Defender.Common.DB.Pagination;
using Defender.Common.DB.SharedStorage.Entities;
using Defender.Common.DB.SharedStorage.Enums;
using Defender.Common.Errors;
using Defender.Common.Exceptions;
using Defender.Kafka;
using Defender.Kafka.Default;
using Defender.WalletService.Application.Common.Interfaces.Repositories;
using Defender.WalletService.Application.Services;
using Defender.WalletService.Common.Kafka;
using Defender.WalletService.Domain.Entities.Transactions;
using Defender.WalletService.Domain.Enums;
using Moq;

namespace Defender.WalletService.Tests.Services;

public class TransactionManagementServiceTests
{
    private readonly Mock<ITransactionRepository> _repository = new();
    private readonly Mock<IDefaultKafkaProducer<string>> _newProducer = new();
    private readonly Mock<IDefaultKafkaProducer<TransactionStatusUpdatedEvent>> _updatedProducer = new();

    private TransactionManagementService CreateSut()
        => new(_repository.Object, _newProducer.Object, _updatedProducer.Object);

    [Fact]
    public async Task GetTransactionByTransactionIdAsync_ReturnsRepositoryResult()
    {
        var expected = new Transaction { TransactionId = "TX-1" };
        _repository.Setup(r => r.GetTransactionByIdAsync("TX-1")).ReturnsAsync(expected);

        var result = await CreateSut().GetTransactionByTransactionIdAsync("TX-1");

        Assert.Same(expected, result);
    }

    [Fact]
    public async Task GetTransactionsByWalletNumberAsync_ReturnsRepositoryResult()
    {
        var expected = new PagedResult<Transaction> { TotalItemsCount = 3, PageSize = 10, CurrentPage = 1 };
        _repository.Setup(r => r.GetTransactionsAsync(It.IsAny<PaginationRequest>(), 12345678)).ReturnsAsync(expected);

        var result = await CreateSut().GetTransactionsByWalletNumberAsync(new PaginationRequest(), 12345678);

        Assert.Same(expected, result);
    }

    [Fact]
    public async Task UpdateTransactionStatusAsync_WhenStatusMovesBack_Throws()
    {
        var transaction = new Transaction { TransactionStatus = TransactionStatus.Proceed };

        await Assert.ThrowsAsync<ServiceException>(() =>
            CreateSut().UpdateTransactionStatusAsync(transaction, TransactionStatus.Queued));
    }

    [Fact]
    public async Task UpdateTransactionStatusAsync_WhenSameStatusAndNoFailureCode_ReturnsInputWithoutUpdate()
    {
        var transaction = new Transaction { TransactionStatus = TransactionStatus.Queued };

        var result = await CreateSut().UpdateTransactionStatusAsync(transaction, TransactionStatus.Queued);

        Assert.Same(transaction, result);
        _repository.Verify(r => r.UpdateTransactionAsync(It.IsAny<Defender.Common.DB.Model.UpdateModelRequest<Transaction>>()), Times.Never);
        _updatedProducer.Verify(p => p.ProduceAsync(It.IsAny<string>(), It.IsAny<TransactionStatusUpdatedEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateTransactionStatusAsync_WhenStatusChanges_UpdatesAndPublishesEvent()
    {
        var input = new Transaction
        {
            TransactionId = "TRN-1",
            TransactionStatus = TransactionStatus.Queued,
            TransactionType = TransactionType.Transfer,
            TransactionPurpose = TransactionPurpose.NoPurpose
        };

        var updated = new Transaction
        {
            TransactionId = "TRN-1",
            TransactionStatus = TransactionStatus.Proceed,
            TransactionType = TransactionType.Transfer,
            TransactionPurpose = TransactionPurpose.NoPurpose
        };

        _repository
            .Setup(r => r.UpdateTransactionAsync(It.IsAny<Defender.Common.DB.Model.UpdateModelRequest<Transaction>>()))
            .ReturnsAsync(updated);

        var result = await CreateSut().UpdateTransactionStatusAsync(input, TransactionStatus.Proceed);

        Assert.Equal(TransactionStatus.Proceed, result.TransactionStatus);
        _updatedProducer.Verify(
            p => p.ProduceAsync(
                Topic.TransactionStatusUpdates.GetName(),
                It.Is<TransactionStatusUpdatedEvent>(e => e.TransactionId == "TRN-1" && e.TransactionStatus == TransactionStatus.Proceed),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateTransferTransactionAsync_PersistsAndPublishesToKafka()
    {
        var request = new Transaction.CreateTransactionRequest
        {
            TargetWallet = 87654321,
            Currency = Currency.USD,
            Amount = 150
        };

        _repository
            .Setup(r => r.CreateNewTransactionAsync(It.IsAny<Transaction>()))
            .ReturnsAsync((Transaction t) => t);

        var result = await CreateSut().CreateTransferTransactionAsync(12345678, request);

        Assert.Equal(TransactionType.Transfer, result.TransactionType);
        Assert.Equal(12345678, result.FromWallet);
        Assert.Equal(87654321, result.ToWallet);
        _newProducer.Verify(
            p => p.ProduceAsync(
                KafkaTopic.TransactionsToProcess.GetName(),
                result.TransactionId,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreatePaymentTransactionAsync_PersistsAndPublishesToKafka()
    {
        var request = new Transaction.CreateTransactionRequest
        {
            TargetWallet = 12345678,
            Currency = Currency.EUR,
            Amount = 80
        };

        _repository
            .Setup(r => r.CreateNewTransactionAsync(It.IsAny<Transaction>()))
            .ReturnsAsync((Transaction t) => t);

        var result = await CreateSut().CreatePaymentTransactionAsync(request);

        Assert.Equal(TransactionType.Payment, result.TransactionType);
        Assert.Equal(12345678, result.FromWallet);
        _newProducer.Verify(
            p => p.ProduceAsync(
                KafkaTopic.TransactionsToProcess.GetName(),
                result.TransactionId,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateRechargeTransactionAsync_PersistsAndPublishesToKafka()
    {
        var request = new Transaction.CreateTransactionRequest
        {
            TargetWallet = 33334444,
            Currency = Currency.USD,
            Amount = 80
        };

        _repository
            .Setup(r => r.CreateNewTransactionAsync(It.IsAny<Transaction>()))
            .ReturnsAsync((Transaction t) => t);

        var result = await CreateSut().CreateRechargeTransactionAsync(request);

        Assert.Equal(TransactionType.Recharge, result.TransactionType);
        Assert.Equal(33334444, result.ToWallet);
        _newProducer.Verify(
            p => p.ProduceAsync(
                KafkaTopic.TransactionsToProcess.GetName(),
                result.TransactionId,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CancelTransactionAsync_WhenQueued_UpdatesToCanceled()
    {
        var transaction = new Transaction
        {
            TransactionId = "TX-1",
            TransactionStatus = TransactionStatus.Queued,
            TransactionType = TransactionType.Transfer
        };

        _repository.Setup(r => r.GetTransactionByIdAsync("TX-1")).ReturnsAsync(transaction);
        _repository.Setup(r => r.UpdateTransactionAsync(It.IsAny<Defender.Common.DB.Model.UpdateModelRequest<Transaction>>()))
            .ReturnsAsync(new Transaction
            {
                TransactionId = "TX-1",
                TransactionStatus = TransactionStatus.Canceled,
                TransactionType = TransactionType.Transfer,
                TransactionPurpose = TransactionPurpose.NoPurpose
            });

        var result = await CreateSut().CancelTransactionAsync("TX-1");

        Assert.Equal("TX-1", result.TransactionId);
        _repository.Verify(r => r.UpdateTransactionAsync(It.IsAny<Defender.Common.DB.Model.UpdateModelRequest<Transaction>>()), Times.Once);
    }

    [Fact]
    public async Task CancelTransactionAsync_WhenProceed_CreatesRevertAndQueuesForRevert()
    {
        var original = new Transaction
        {
            TransactionId = "TX-PROCEED",
            TransactionStatus = TransactionStatus.Proceed,
            TransactionType = TransactionType.Transfer,
            FromWallet = 11112222,
            ToWallet = 33334444,
            Amount = 50,
            Currency = Currency.USD,
            TransactionPurpose = TransactionPurpose.NoPurpose
        };

        var revertTx = new Transaction
        {
            TransactionId = "TX-REVERT",
            TransactionStatus = TransactionStatus.Queued,
            TransactionType = TransactionType.Revert,
            ParentTransactionId = "TX-PROCEED",
            FromWallet = 33334444,
            ToWallet = 11112222,
            Amount = 50,
            Currency = Currency.USD,
            TransactionPurpose = TransactionPurpose.NoPurpose
        };

        _repository.Setup(r => r.GetTransactionByIdAsync("TX-PROCEED")).ReturnsAsync(original);
        _repository
            .Setup(r => r.UpdateTransactionAsync(It.IsAny<Defender.Common.DB.Model.UpdateModelRequest<Transaction>>()))
            .ReturnsAsync(new Transaction
            {
                TransactionId = "TX-PROCEED",
                TransactionStatus = TransactionStatus.QueuedForRevert,
                TransactionType = TransactionType.Transfer,
                TransactionPurpose = TransactionPurpose.NoPurpose
            });
        _repository
            .Setup(r => r.CreateNewTransactionAsync(It.IsAny<Transaction>()))
            .ReturnsAsync((Transaction t) => { t.TransactionId = "TX-REVERT"; return t; });

        var result = await CreateSut().CancelTransactionAsync("TX-PROCEED");

        Assert.Equal(TransactionType.Revert, result.TransactionType);
        Assert.Equal("TX-PROCEED", result.ParentTransactionId);
        _repository.Verify(r => r.CreateNewTransactionAsync(It.Is<Transaction>(t =>
            t.TransactionType == TransactionType.Revert &&
            t.FromWallet == 33334444 &&
            t.ToWallet == 11112222)), Times.Once);
        _updatedProducer.Verify(
            p => p.ProduceAsync(It.IsAny<string>(), It.IsAny<TransactionStatusUpdatedEvent>(), It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task CancelTransactionAsync_WhenUnsupportedStatus_Throws()
    {
        var transaction = new Transaction
        {
            TransactionId = "TX-2",
            TransactionStatus = TransactionStatus.Canceled
        };

        _repository.Setup(r => r.GetTransactionByIdAsync("TX-2")).ReturnsAsync(transaction);

        await Assert.ThrowsAsync<ServiceException>(() => CreateSut().CancelTransactionAsync("TX-2"));
    }

    [Fact]
    public async Task UpdateTransactionStatusAsync_WithErrorCode_SetsFailureCode()
    {
        var input = new Transaction
        {
            TransactionId = "TX-ERR",
            TransactionStatus = TransactionStatus.Queued,
            TransactionType = TransactionType.Payment,
            TransactionPurpose = TransactionPurpose.NoPurpose
        };

        _repository
            .Setup(r => r.UpdateTransactionAsync(It.IsAny<Defender.Common.DB.Model.UpdateModelRequest<Transaction>>()))
            .ReturnsAsync(new Transaction
            {
                TransactionId = "TX-ERR",
                TransactionStatus = TransactionStatus.Failed,
                FailureCode = ErrorCode.UnhandledError.ToString(),
                TransactionType = TransactionType.Payment,
                TransactionPurpose = TransactionPurpose.NoPurpose
            });

        var result = await CreateSut().UpdateTransactionStatusAsync(input, TransactionStatus.Failed, ErrorCode.UnhandledError);

        Assert.Equal(TransactionStatus.Failed, result.TransactionStatus);
        Assert.Equal(ErrorCode.UnhandledError.ToString(), result.FailureCode);
    }
}
