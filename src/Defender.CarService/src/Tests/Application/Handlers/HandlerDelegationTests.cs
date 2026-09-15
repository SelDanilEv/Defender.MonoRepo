using Defender.CarService.Application.Common.Interfaces.Services;
using Defender.CarService.Application.DTOs;
using Defender.CarService.Application.Handlers.Vehicles;
using Defender.CarService.Application.Requests.Vehicles;
using Moq;

namespace Defender.CarService.Tests.Application.Handlers;

public sealed class HandlerDelegationTests
{
    [Fact]
    public async Task GetVehiclesHandler_UsesApplicationService()
    {
        var service = new Mock<IMyGarageApplicationService>();
        service.Setup(item => item.GetVehiclesAsync(false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<VehicleSummaryDto>());

        var result = await new GetVehiclesQueryHandler(service.Object).Handle(
            new GetVehiclesQuery(),
            CancellationToken.None);

        Assert.Empty(result);
        service.Verify(item => item.GetVehiclesAsync(false, It.IsAny<CancellationToken>()), Times.Once);
    }
}
