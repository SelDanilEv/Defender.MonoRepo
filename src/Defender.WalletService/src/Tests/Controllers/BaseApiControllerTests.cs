using MediatR;
using WebApi.Controllers;

namespace Defender.WalletService.Tests.Controllers;

public class BaseApiControllerTests
{
    [Fact]
    public async Task ProcessApiCallAsync_WhenRequestSent_MapsMediatorResponse()
    {
        var request = new { Name = "req" };
        var response = new { Value = 42 };
        var mediator = new Mock<IMediator>();
        mediator.Setup(x => x.Send(It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);
        var mapper = new Mock<AutoMapper.IMapper>();
        mapper.Setup(x => x.Map<string>(response)).Returns("mapped");
        var controller = new TestController(mediator.Object, mapper.Object);

        var result = await controller.CallProcessApiCallAsync<object, string>(request);

        Assert.Equal("mapped", result);
    }

    [Fact]
    public async Task ProcessApiCallAsyncWithoutResult_WhenRequestSent_CallsMediator()
    {
        var request = new { Name = "req" };
        var mediator = new Mock<IMediator>();
        mediator.Setup(x => x.Send(It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new object());
        var controller = new TestController(mediator.Object, Mock.Of<AutoMapper.IMapper>());

        await controller.CallProcessApiCallAsync(request);

        mediator.Verify(x => x.Send(It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessApiCallWithoutMappingAsync_WhenRequestSent_ReturnsTypedMediatorResponse()
    {
        var expected = new Dictionary<string, string> { { "x", "y" } };
        var mediator = new Mock<IMediator>();
        mediator.Setup(x => x.Send(It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);
        var controller = new TestController(mediator.Object, Mock.Of<AutoMapper.IMapper>());

        var result = await controller.CallProcessApiCallWithoutMappingAsync<object, Dictionary<string, string>>(new { });

        Assert.Equal("y", result["x"]);
    }

    private sealed class TestController(IMediator mediator, AutoMapper.IMapper mapper)
        : BaseApiController(mediator, mapper)
    {
        public Task<TResult> CallProcessApiCallAsync<TRequest, TResult>(TRequest request)
        {
            return ProcessApiCallAsync<TRequest, TResult>(request);
        }

        public Task CallProcessApiCallAsync<TRequest>(TRequest request)
        {
            return ProcessApiCallAsync(request);
        }

        public Task<TResult> CallProcessApiCallWithoutMappingAsync<TRequest, TResult>(TRequest request)
        {
            return ProcessApiCallWithoutMappingAsync<TRequest, TResult>(request);
        }
    }
}
