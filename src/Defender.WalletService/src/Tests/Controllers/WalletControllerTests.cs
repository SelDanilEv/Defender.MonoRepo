using Defender.WalletService.Application.DTOs;
using Defender.WalletService.Application.Modules.Wallets.Commands;
using Defender.WalletService.Application.Modules.Wallets.Queries;
using Defender.WalletService.Domain.Entities.Wallets;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using WebApi.Controllers.V1;

namespace Defender.WalletService.Tests.Controllers;

public class WalletControllerTests
{
    private readonly Mock<IMediator> _mediator = new();
    private readonly Mock<AutoMapper.IMapper> _mapper = new();
    private readonly WalletController _controller;

    public WalletControllerTests()
    {
        _controller = new WalletController(_mediator.Object, _mapper.Object);
    }

    [Fact]
    public async Task GetOrCreateWalletAsync_DispatchesGetOrCreateWalletCommand_ReturnsMappedResult()
    {
        var command = new GetOrCreateWalletCommand();
        var wallet = new Wallet { Id = Guid.NewGuid(), WalletNumber = 12345678 };
        var dto = new WalletDto { OwnerId = wallet.Id, WalletNumber = 12345678 };

        _mediator.Setup(m => m.Send(It.IsAny<GetOrCreateWalletCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(wallet);
        _mapper.Setup(m => m.Map<WalletDto>(It.IsAny<object>())).Returns(dto);

        var result = await _controller.GetOrCreateWalletAsync(command);

        Assert.Equal(dto.OwnerId, result.OwnerId);
    }

    [Fact]
    public async Task GetPublicWalletInfoByNumberAsync_DispatchesGetWalletInfoByNumberQuery_ReturnsPublicWalletInfoDto()
    {
        var query = new GetWalletInfoByNumberQuery { WalletNumber = 12345678 };
        var wallet = new Wallet { Id = Guid.NewGuid(), WalletNumber = 12345678 };
        var dto = new PublicWalletInfoDto { OwnerId = wallet.Id, WalletNumber = 12345678 };

        _mediator.Setup(m => m.Send(It.IsAny<GetWalletInfoByNumberQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync(wallet);
        _mapper.Setup(m => m.Map<PublicWalletInfoDto>(It.IsAny<object>())).Returns(dto);

        var result = await _controller.GetPublicWalletInfoByNumberAsync(query);

        Assert.Equal(dto.OwnerId, result.OwnerId);
    }

    [Fact]
    public async Task AddCurrencyAccountAsync_DispatchesAddCurrencyAccountCommand_ReturnsMappedResult()
    {
        var command = new AddCurrencyAccountCommand { Currency = Domain.Enums.Currency.EUR, IsDefault = false };
        var wallet = new Wallet { Id = Guid.NewGuid() };
        var dto = new WalletDto { OwnerId = wallet.Id };

        _mediator.Setup(m => m.Send(It.IsAny<AddCurrencyAccountCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(wallet);
        _mapper.Setup(m => m.Map<WalletDto>(It.IsAny<object>())).Returns(dto);

        var result = await _controller.AddCurrencyAccountAsync(command);

        Assert.Equal(dto.OwnerId, result.OwnerId);
    }

    [Fact]
    public async Task SetDefaultCurrencyAccountAsync_DispatchesSetDefaultCurrencyAccountCommand_ReturnsMappedResult()
    {
        var command = new SetDefaultCurrencyAccountCommand { Currency = Domain.Enums.Currency.USD };
        var wallet = new Wallet { Id = Guid.NewGuid() };
        var dto = new WalletDto { OwnerId = wallet.Id };

        _mediator.Setup(m => m.Send(It.IsAny<SetDefaultCurrencyAccountCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(wallet);
        _mapper.Setup(m => m.Map<WalletDto>(It.IsAny<object>())).Returns(dto);

        var result = await _controller.SetDefaultCurrencyAccountAsync(command);

        Assert.Equal(dto.OwnerId, result.OwnerId);
    }

    [Fact]
    public async Task GetPrivateWalletInfoByNumberAsync_DispatchesGetWalletInfoByNumberQuery_ReturnsWalletDto()
    {
        var query = new GetWalletInfoByNumberQuery { WalletNumber = 12345678 };
        var wallet = new Wallet { Id = Guid.NewGuid(), WalletNumber = 12345678 };
        var dto = new WalletDto { OwnerId = wallet.Id, WalletNumber = 12345678 };

        _mediator.Setup(m => m.Send(It.IsAny<GetWalletInfoByNumberQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync(wallet);
        _mapper.Setup(m => m.Map<WalletDto>(It.IsAny<object>())).Returns(dto);

        var result = await _controller.GetPrivateWalletInfoByNumberAsync(query);

        Assert.Equal(dto.OwnerId, result.OwnerId);
        Assert.Equal(dto.WalletNumber, result.WalletNumber);
    }
}
