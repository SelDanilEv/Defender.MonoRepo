using Defender.Common.DTOs;
using Defender.Common.Enums;
using Defender.Common.Modules.Home.Queries;
using MediatR;
using WebApi.Controllers.V1;

namespace Defender.WalletService.Tests.Controllers;

public class HomeControllerTests
{
    private readonly Mock<IMediator> _mediator = new();
    private readonly HomeController _controller;

    public HomeControllerTests()
    {
        _controller = new HomeController(_mediator.Object, Mock.Of<AutoMapper.IMapper>());
    }

    [Fact]
    public async Task HealthCheckAsync_WhenCalled_DispatchesHealthCheckQuery()
    {
        var expected = new HealthCheckDto("OK");
        object? sentRequest = null;
        _mediator.Setup(x => x.Send(It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .Callback<object, CancellationToken>((request, _) => sentRequest = request)
            .ReturnsAsync(expected);

        var result = await _controller.HealthCheckAsync();

        Assert.IsType<HealthCheckQuery>(sentRequest);
        Assert.Same(expected, result);
    }

    [Fact]
    public async Task AuthorizationCheckAsync_WhenCalled_DispatchesAuthCheckQuery()
    {
        var expected = new AuthCheckDto(Guid.NewGuid(), Role.User);
        object? sentRequest = null;
        _mediator.Setup(x => x.Send(It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .Callback<object, CancellationToken>((request, _) => sentRequest = request)
            .ReturnsAsync(expected);

        var result = await _controller.AuthorizationCheckAsync();

        Assert.IsType<AuthCheckQuery>(sentRequest);
        Assert.Same(expected, result);
    }

    [Fact]
    public async Task GetConfigurationAsync_WhenCalled_DispatchesGetConfigurationQueryWithLevel()
    {
        var expected = new Dictionary<string, string>
        {
            { "k", "v" }
        };
        object? sentRequest = null;
        _mediator.Setup(x => x.Send(It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .Callback<object, CancellationToken>((request, _) => sentRequest = request)
            .ReturnsAsync(expected);

        var result = await _controller.GetConfigurationAsync(ConfigurationLevel.Admin);

        var query = Assert.IsType<GetConfigurationQuery>(sentRequest);
        Assert.Equal(ConfigurationLevel.Admin, query.Level);
        Assert.Equal("v", result["k"]);
    }
}
