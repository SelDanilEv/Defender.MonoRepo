using Defender.Common.DB.Pagination;
using Defender.Common.DB.SharedStorage.Enums;
using Defender.WalletService.Application.DTOs;
using Defender.WalletService.Application.Modules.Transactions.Commands;
using Defender.WalletService.Application.Modules.Transactions.Queries;
using Defender.WalletService.Domain.Entities.Transactions;
using Defender.WalletService.Domain.Enums;
using MediatR;
using Moq;
using WebApi.Controllers.V1;

namespace Defender.WalletService.Tests.Controllers;

public class TransactionControllerTests
{
    private readonly Mock<IMediator> _mediator = new();
    private readonly Mock<AutoMapper.IMapper> _mapper = new();
    private readonly TransactionController _controller;

    public TransactionControllerTests()
    {
        _controller = new TransactionController(_mediator.Object, _mapper.Object);
    }

    [Fact]
    public async Task GetUserTransactionsAsync_ByTransactionId_DispatchesQuery_ReturnsAnonymousTransactionDto()
    {
        var query = new GetUserTransactionByTransactionIdQuery { TransactionId = "TX-1" };
        var transaction = new Transaction { TransactionId = "TX-1", TransactionStatus = Defender.Common.DB.SharedStorage.Enums.TransactionStatus.Proceed };
        var dto = new AnonymousTransactionDto { TransactionId = "TX-1" };

        _mediator.Setup(m => m.Send(It.IsAny<GetUserTransactionByTransactionIdQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync(transaction);
        _mapper.Setup(m => m.Map<AnonymousTransactionDto>(It.IsAny<object>())).Returns(dto);

        var result = await _controller.GetUserTransactionsAsync(query);

        Assert.Equal(dto.TransactionId, result.TransactionId);
    }

    [Fact]
    public async Task GetUserTransactionsAsync_History_DispatchesQuery_ReturnsPagedTransactionDto()
    {
        var query = new GetTransactionHistoryQuery { WalletId = Guid.NewGuid() };
        var paged = new PagedResult<Transaction> { TotalItemsCount = 1, CurrentPage = 1, PageSize = 10, Items = [] };
        var pagedDto = new PagedResult<TransactionDto> { TotalItemsCount = 1, CurrentPage = 1, PageSize = 10, Items = [] };

        _mediator.Setup(m => m.Send(It.IsAny<GetTransactionHistoryQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync(paged);
        _mapper.Setup(m => m.Map<PagedResult<TransactionDto>>(It.IsAny<object>())).Returns(pagedDto);

        var result = await _controller.GetUserTransactionsAsync(query);

        Assert.Equal(pagedDto.TotalItemsCount, result.TotalItemsCount);
    }

    [Fact]
    public async Task CreatePaymentTransactionAsync_DispatchesCommand_ReturnsTransactionDto()
    {
        var command = new StartPaymentTransactionCommand { TargetWalletNumber = 12345678, Amount = 50, Currency = Currency.USD };
        var transaction = new Transaction { TransactionId = "PAY-1", TransactionType = TransactionType.Payment };
        var dto = new TransactionDto { TransactionId = "PAY-1" };

        _mediator.Setup(m => m.Send(It.IsAny<StartPaymentTransactionCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(transaction);
        _mapper.Setup(m => m.Map<TransactionDto>(It.IsAny<object>())).Returns(dto);

        var result = await _controller.CreatePaymentTransactionAsync(command);

        Assert.Equal(dto.TransactionId, result.TransactionId);
    }

    [Fact]
    public async Task CreateRechargeTransactionAsync_DispatchesCommand_ReturnsTransactionDto()
    {
        var command = new StartRechargeTransactionCommand { TargetWalletNumber = 12345678, Amount = 100, Currency = Currency.EUR };
        var transaction = new Transaction { TransactionId = "RCH-1", TransactionType = TransactionType.Recharge };
        var dto = new TransactionDto { TransactionId = "RCH-1" };

        _mediator.Setup(m => m.Send(It.IsAny<StartRechargeTransactionCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(transaction);
        _mapper.Setup(m => m.Map<TransactionDto>(It.IsAny<object>())).Returns(dto);

        var result = await _controller.CreateRechargeTransactionAsync(command);

        Assert.Equal(dto.TransactionId, result.TransactionId);
    }

    [Fact]
    public async Task CreateTransferTransactionAsync_DispatchesCommand_ReturnsTransactionDto()
    {
        var command = new StartTransferTransactionCommand { TargetWalletNumber = 87654321, Amount = 25, Currency = Currency.USD };
        var transaction = new Transaction { TransactionId = "TRN-1", TransactionType = TransactionType.Transfer };
        var dto = new TransactionDto { TransactionId = "TRN-1" };

        _mediator.Setup(m => m.Send(It.IsAny<StartTransferTransactionCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(transaction);
        _mapper.Setup(m => m.Map<TransactionDto>(It.IsAny<object>())).Returns(dto);

        var result = await _controller.CreateTransferTransactionAsync(command);

        Assert.Equal(dto.TransactionId, result.TransactionId);
    }

    [Fact]
    public async Task CancelTransactionAsync_DispatchesCommand_ReturnsTransactionDto()
    {
        var command = new CancelTransactionCommand { TransactionId = "TX-CANCEL" };
        var transaction = new Transaction { TransactionId = "TX-CANCEL", TransactionStatus = Defender.Common.DB.SharedStorage.Enums.TransactionStatus.Canceled };
        var dto = new TransactionDto { TransactionId = "TX-CANCEL" };

        _mediator.Setup(m => m.Send(It.IsAny<CancelTransactionCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(transaction);
        _mapper.Setup(m => m.Map<TransactionDto>(It.IsAny<object>())).Returns(dto);

        var result = await _controller.CancelTransactionAsync(command);

        Assert.Equal(dto.TransactionId, result.TransactionId);
    }
}
