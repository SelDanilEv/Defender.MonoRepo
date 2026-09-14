using Defender.Portal.Application.DTOs.MyGarage;
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
            .Setup(item => item.GetVehiclesAsync(false, It.IsAny<CancellationToken>()))
            .ThrowsAsync(expected);
        var sut = new CarServiceWrapper(client.Object);

        var actual = await Assert.ThrowsAsync<CarServiceUpstreamException>(() => sut.GetVehiclesAsync(false, CancellationToken.None));

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
}
