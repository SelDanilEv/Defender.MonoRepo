using Defender.CarService.Application.Common.Interfaces.Services;
using Defender.CarService.Application.DTOs;
using Defender.CarService.Application.Handlers.Insurance;
using Defender.CarService.Application.Handlers.Vehicles;
using Defender.CarService.Application.Requests.Insurance;
using Defender.CarService.Application.Requests.Vehicles;
using MediatR;
using Moq;

namespace Defender.CarService.Tests.Application.Handlers;

public sealed class HandlerDelegationTests
{
    [Fact]
    public async Task GetVehiclesHandler_UsesApplicationService()
    {
        var query = new GetVehiclesQuery();
        var page = new VehiclePageDto { Items = [], TotalItemsCount = 0 };
        var service = new Mock<IMyGarageApplicationService>();
        service.Setup(item => item.GetVehiclesAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(page);

        var result = await new GetVehiclesQueryHandler(service.Object).Handle(
            query,
            CancellationToken.None);

        Assert.Empty(result.Items);
        service.Verify(item => item.GetVehiclesAsync(query, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteInsurancePolicyHandler_UsesApplicationService()
    {
        var command = new DeleteInsurancePolicyCommand { VehicleId = Guid.NewGuid(), InsuranceId = Guid.NewGuid() };
        var service = new Mock<IMyGarageApplicationService>();
        service.Setup(item => item.DeleteInsurancePolicyAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Unit.Value);

        var result = await new DeleteInsurancePolicyCommandHandler(service.Object).Handle(
            command,
            CancellationToken.None);

        Assert.Equal(Unit.Value, result);
        service.Verify(item => item.DeleteInsurancePolicyAsync(command, It.IsAny<CancellationToken>()), Times.Once);
    }
}
