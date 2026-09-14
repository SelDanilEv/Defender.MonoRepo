using Defender.Common.Attributes;
using Defender.Common.Consts;
using Defender.Portal.Application.Common.Interfaces.Wrappers;
using Defender.Portal.Application.DTOs.MyGarage;
using Defender.Portal.Application.Models.ApiRequests.MyGarage;
using Defender.Portal.WebUI.Controllers.V1;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace Defender.Portal.Tests.Controllers;

public sealed class MyGarageControllerTests
{
    [Fact]
    public async Task GetVehiclesAsync_WhenCalled_DelegatesIncludeArchivedAndReturnsOk()
    {
        var wrapper = new Mock<ICarServiceWrapper>();
        var expected = new List<VehicleSummaryDto>();
        wrapper.Setup(item => item.GetVehiclesAsync(true, It.IsAny<CancellationToken>())).ReturnsAsync(expected);
        var sut = CreateController(wrapper.Object);

        var result = await sut.GetVehiclesAsync(true, CancellationToken.None);

        var response = Assert.IsType<OkObjectResult>(result);
        Assert.Same(expected, response.Value);
    }

    [Fact]
    public async Task CreateVehicleAsync_WhenCalled_ReturnsCreated()
    {
        var wrapper = new Mock<ICarServiceWrapper>();
        var expected = new VehicleDto();
        var request = new CreateVehicleRequest();
        wrapper.Setup(item => item.CreateVehicleAsync(request, It.IsAny<CancellationToken>())).ReturnsAsync(expected);
        var sut = CreateController(wrapper.Object);

        var result = await sut.CreateVehicleAsync(request, CancellationToken.None);

        Assert.Same(expected, Assert.IsType<ObjectResult>(result).Value);
        Assert.Equal(StatusCodes.Status201Created, Assert.IsType<ObjectResult>(result).StatusCode);
    }

    [Fact]
    public async Task DeleteChildAsync_WhenCalled_ReturnsNoContent()
    {
        var wrapper = new Mock<ICarServiceWrapper>();
        var vehicleId = Guid.NewGuid();
        var maintenanceId = Guid.NewGuid();
        wrapper.Setup(item => item.DeleteMaintenanceItemAsync(vehicleId, maintenanceId, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var sut = CreateController(wrapper.Object);

        var result = await sut.DeleteMaintenanceItemAsync(vehicleId, maintenanceId, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public void Controller_WhenInspected_UsesMyGarageRouteAndUserRole()
    {
        var type = typeof(MyGarageController);
        var route = type.GetCustomAttributes(typeof(RouteAttribute), inherit: true).Cast<RouteAttribute>().Single();
        var auth = type.GetCustomAttributes(typeof(AuthAttribute), inherit: true).Cast<AuthAttribute>().Single();

        Assert.Equal("api/my-garage", route.Template);
        Assert.Equal(Roles.User, auth.Roles);
    }

    [Fact]
    public void Controller_WhenInspected_MirrorsEveryCarServiceSuffix()
    {
        var routes = typeof(MyGarageController)
            .GetMethods()
            .SelectMany(method => method
                .GetCustomAttributes(typeof(HttpMethodAttribute), inherit: false)
                .Cast<HttpMethodAttribute>()
                .Select(attribute => $"{attribute.HttpMethods.Single()} {attribute.Template}"))
            .ToHashSet(StringComparer.Ordinal);

        var expected = new HashSet<string>(StringComparer.Ordinal)
        {
            "GET vehicles",
            "POST vehicles",
            "GET vehicles/{vehicleId:guid}",
            "PUT vehicles/{vehicleId:guid}",
            "POST vehicles/{vehicleId:guid}/archive",
            "POST vehicles/{vehicleId:guid}/unarchive",
            "GET vehicles/{vehicleId:guid}/maintenance",
            "POST vehicles/{vehicleId:guid}/maintenance",
            "PUT vehicles/{vehicleId:guid}/maintenance/{maintenanceId:guid}",
            "DELETE vehicles/{vehicleId:guid}/maintenance/{maintenanceId:guid}",
            "GET vehicles/{vehicleId:guid}/history",
            "POST vehicles/{vehicleId:guid}/history",
            "PUT vehicles/{vehicleId:guid}/history/{historyId:guid}",
            "DELETE vehicles/{vehicleId:guid}/history/{historyId:guid}",
            "GET vehicles/{vehicleId:guid}/insurance",
            "POST vehicles/{vehicleId:guid}/insurance",
            "PUT vehicles/{vehicleId:guid}/insurance/{insuranceId:guid}",
        };

        Assert.Equal(expected, routes);
    }

    private static MyGarageController CreateController(ICarServiceWrapper wrapper) => new(wrapper);
}
