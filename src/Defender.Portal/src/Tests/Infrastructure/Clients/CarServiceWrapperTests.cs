using Defender.Portal.Application.DTOs.MyGarage;
using Defender.Portal.Application.Models.ApiRequests.MyGarage;
using Defender.Portal.Infrastructure.Clients.CarService;

namespace Defender.Portal.Tests.Infrastructure.Clients;

public sealed class CarServiceWrapperTests
{
    [Fact]
    public async Task GetVehiclesAsync_WhenClientThrowsUpstreamException_PreservesSameException()
    {
        var expected = new CarServiceUpstreamException(409, "CAR_CONCURRENCY_CONFLICT", "conflict");
        var client = new Mock<ICarServiceClient>();
        client
            .Setup(item => item.GetVehiclesAsync(false, 0, 25, It.IsAny<CancellationToken>()))
            .ThrowsAsync(expected);
        var sut = new CarServiceWrapper(client.Object);

        var actual = await Assert.ThrowsAsync<CarServiceUpstreamException>(() => sut.GetVehiclesAsync(false, cancellationToken: CancellationToken.None));

        Assert.Same(expected, actual);
    }

    [Fact]
    public async Task DeleteHistoryAsync_WhenClientReturns_PreservesNoContentOperation()
    {
        var client = new Mock<ICarServiceClient>();
        var vehicleId = Guid.NewGuid();
        var historyId = Guid.NewGuid();
        var sut = new CarServiceWrapper(client.Object);

        await sut.DeleteHistoryAsync(vehicleId, historyId, CancellationToken.None);

        client.Verify(item => item.DeleteHistoryAsync(vehicleId, historyId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AllMethods_WhenCalled_DelegateEveryRouteWithAllArguments()
    {
        var client = new Mock<ICarServiceClient>(MockBehavior.Strict);
        var token = new CancellationTokenSource().Token;
        var vehicleId = Guid.NewGuid();
        var maintenanceId = Guid.NewGuid();
        var historyId = Guid.NewGuid();
        var insuranceId = Guid.NewGuid();
        var vehicleRequest = new CreateVehicleRequest();
        var updateVehicleRequest = new UpdateVehicleRequest();
        var maintenanceRequest = new CreateMaintenanceItemRequest();
        var updateMaintenanceRequest = new UpdateMaintenanceItemRequest();
        var historyRequest = new CreateServiceHistoryRequest();
        var updateHistoryRequest = new UpdateServiceHistoryRequest();
        var insuranceRequest = new CreateInsurancePolicyRequest();
        var updateInsuranceRequest = new UpdateInsurancePolicyRequest();

        client.Setup(item => item.GetVehiclesAsync(true, 2, 10, token)).ReturnsAsync(new VehiclePageDto());
        client.Setup(item => item.CreateVehicleAsync(vehicleRequest, token)).ReturnsAsync(new VehicleDto());
        client.Setup(item => item.GetVehicleAsync(vehicleId, token)).ReturnsAsync(new VehicleDetailDto());
        client.Setup(item => item.UpdateVehicleAsync(vehicleId, updateVehicleRequest, token)).ReturnsAsync(new VehicleDto());
        client.Setup(item => item.ArchiveVehicleAsync(vehicleId, token)).ReturnsAsync(new VehicleDto());
        client.Setup(item => item.UnarchiveVehicleAsync(vehicleId, token)).ReturnsAsync(new VehicleDto());
        client.Setup(item => item.GetMaintenanceItemsAsync(vehicleId, token)).ReturnsAsync([]);
        client.Setup(item => item.CreateMaintenanceItemAsync(vehicleId, maintenanceRequest, token)).ReturnsAsync(new MaintenanceItemDto());
        client.Setup(item => item.UpdateMaintenanceItemAsync(vehicleId, maintenanceId, updateMaintenanceRequest, token)).ReturnsAsync(new MaintenanceItemDto());
        client.Setup(item => item.DeleteMaintenanceItemAsync(vehicleId, maintenanceId, token)).Returns(Task.CompletedTask);
        client.Setup(item => item.GetHistoryAsync(vehicleId, 2, 10, token)).ReturnsAsync(new ServiceHistoryPageDto());
        client.Setup(item => item.CreateHistoryAsync(vehicleId, historyRequest, token)).ReturnsAsync(new ServiceHistoryRecordDto());
        client.Setup(item => item.UpdateHistoryAsync(vehicleId, historyId, updateHistoryRequest, token)).ReturnsAsync(new ServiceHistoryRecordDto());
        client.Setup(item => item.DeleteHistoryAsync(vehicleId, historyId, token)).Returns(Task.CompletedTask);
        client.Setup(item => item.GetInsurancePoliciesAsync(vehicleId, token)).ReturnsAsync([]);
        client.Setup(item => item.CreateInsurancePolicyAsync(vehicleId, insuranceRequest, token)).ReturnsAsync(new InsurancePolicyDto());
        client.Setup(item => item.UpdateInsurancePolicyAsync(vehicleId, insuranceId, updateInsuranceRequest, token)).ReturnsAsync(new InsurancePolicyDto());
        client.Setup(item => item.DeleteInsurancePolicyAsync(vehicleId, insuranceId, token)).Returns(Task.CompletedTask);

        var sut = new CarServiceWrapper(client.Object);

        await sut.GetVehiclesAsync(true, 2, 10, token);
        await sut.CreateVehicleAsync(vehicleRequest, token);
        await sut.GetVehicleAsync(vehicleId, token);
        await sut.UpdateVehicleAsync(vehicleId, updateVehicleRequest, token);
        await sut.ArchiveVehicleAsync(vehicleId, token);
        await sut.UnarchiveVehicleAsync(vehicleId, token);
        await sut.GetMaintenanceItemsAsync(vehicleId, token);
        await sut.CreateMaintenanceItemAsync(vehicleId, maintenanceRequest, token);
        await sut.UpdateMaintenanceItemAsync(vehicleId, maintenanceId, updateMaintenanceRequest, token);
        await sut.DeleteMaintenanceItemAsync(vehicleId, maintenanceId, token);
        await sut.GetHistoryAsync(vehicleId, 2, 10, token);
        await sut.CreateHistoryAsync(vehicleId, historyRequest, token);
        await sut.UpdateHistoryAsync(vehicleId, historyId, updateHistoryRequest, token);
        await sut.DeleteHistoryAsync(vehicleId, historyId, token);
        await sut.GetInsurancePoliciesAsync(vehicleId, token);
        await sut.CreateInsurancePolicyAsync(vehicleId, insuranceRequest, token);
        await sut.UpdateInsurancePolicyAsync(vehicleId, insuranceId, updateInsuranceRequest, token);
        await sut.DeleteInsurancePolicyAsync(vehicleId, insuranceId, token);

        client.VerifyAll();
    }
}
