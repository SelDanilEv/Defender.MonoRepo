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
        var expected = new VehiclePageDto();
        wrapper.Setup(item => item.GetVehiclesAsync(true, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(expected);
        var sut = CreateController(wrapper.Object);

        var result = await sut.GetVehiclesAsync(true, cancellationToken: CancellationToken.None);

        var response = Assert.IsType<OkObjectResult>(result);
        Assert.Same(expected, response.Value);
    }

    [Fact]
    public async Task GetVehiclesAsync_WhenCalled_DelegatesPageAndPageSize()
    {
        var wrapper = new Mock<ICarServiceWrapper>();
        var expected = new VehiclePageDto();
        wrapper.Setup(item => item.GetVehiclesAsync(false, 3, 15, It.IsAny<CancellationToken>())).ReturnsAsync(expected);
        var sut = CreateController(wrapper.Object);

        var result = await sut.GetVehiclesAsync(page: 3, pageSize: 15, cancellationToken: CancellationToken.None);

        var response = Assert.IsType<OkObjectResult>(result);
        Assert.Same(expected, response.Value);
    }

    [Fact]
    public async Task DeleteInsurancePolicyAsync_WhenCalled_ReturnsNoContent()
    {
        var wrapper = new Mock<ICarServiceWrapper>();
        var vehicleId = Guid.NewGuid();
        var insuranceId = Guid.NewGuid();
        wrapper.Setup(item => item.DeleteInsurancePolicyAsync(vehicleId, insuranceId, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var sut = CreateController(wrapper.Object);

        var result = await sut.DeleteInsurancePolicyAsync(vehicleId, insuranceId, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
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
            "DELETE vehicles/{vehicleId:guid}/insurance/{insuranceId:guid}",
        };

        Assert.Equal(expected, routes);
    }

    [Fact]
    public async Task AllActions_WhenCalled_DelegateEveryRouteAndPreserveStatuses()
    {
        var wrapper = new Mock<ICarServiceWrapper>(MockBehavior.Strict);
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

        wrapper.Setup(item => item.GetVehiclesAsync(true, 2, 10, token)).ReturnsAsync(new VehiclePageDto());
        wrapper.Setup(item => item.CreateVehicleAsync(vehicleRequest, token)).ReturnsAsync(new VehicleDto());
        wrapper.Setup(item => item.GetVehicleAsync(vehicleId, token)).ReturnsAsync(new VehicleDetailDto());
        wrapper.Setup(item => item.UpdateVehicleAsync(vehicleId, updateVehicleRequest, token)).ReturnsAsync(new VehicleDto());
        wrapper.Setup(item => item.ArchiveVehicleAsync(vehicleId, token)).ReturnsAsync(new VehicleDto());
        wrapper.Setup(item => item.UnarchiveVehicleAsync(vehicleId, token)).ReturnsAsync(new VehicleDto());
        wrapper.Setup(item => item.GetMaintenanceItemsAsync(vehicleId, token)).ReturnsAsync([]);
        wrapper.Setup(item => item.CreateMaintenanceItemAsync(vehicleId, maintenanceRequest, token)).ReturnsAsync(new MaintenanceItemDto());
        wrapper.Setup(item => item.UpdateMaintenanceItemAsync(vehicleId, maintenanceId, updateMaintenanceRequest, token)).ReturnsAsync(new MaintenanceItemDto());
        wrapper.Setup(item => item.DeleteMaintenanceItemAsync(vehicleId, maintenanceId, token)).Returns(Task.CompletedTask);
        wrapper.Setup(item => item.GetHistoryAsync(vehicleId, 2, 10, token)).ReturnsAsync(new ServiceHistoryPageDto());
        wrapper.Setup(item => item.CreateHistoryAsync(vehicleId, historyRequest, token)).ReturnsAsync(new ServiceHistoryRecordDto());
        wrapper.Setup(item => item.UpdateHistoryAsync(vehicleId, historyId, updateHistoryRequest, token)).ReturnsAsync(new ServiceHistoryRecordDto());
        wrapper.Setup(item => item.DeleteHistoryAsync(vehicleId, historyId, token)).Returns(Task.CompletedTask);
        wrapper.Setup(item => item.GetInsurancePoliciesAsync(vehicleId, token)).ReturnsAsync([]);
        wrapper.Setup(item => item.CreateInsurancePolicyAsync(vehicleId, insuranceRequest, token)).ReturnsAsync(new InsurancePolicyDto());
        wrapper.Setup(item => item.UpdateInsurancePolicyAsync(vehicleId, insuranceId, updateInsuranceRequest, token)).ReturnsAsync(new InsurancePolicyDto());
        wrapper.Setup(item => item.DeleteInsurancePolicyAsync(vehicleId, insuranceId, token)).Returns(Task.CompletedTask);

        var sut = CreateController(wrapper.Object);

        Assert.IsType<OkObjectResult>(await sut.GetVehiclesAsync(true, 2, 10, token));
        Assert.Equal(StatusCodes.Status201Created, Assert.IsType<ObjectResult>(await sut.CreateVehicleAsync(vehicleRequest, token)).StatusCode);
        Assert.IsType<OkObjectResult>(await sut.GetVehicleAsync(vehicleId, token));
        Assert.IsType<OkObjectResult>(await sut.UpdateVehicleAsync(vehicleId, updateVehicleRequest, token));
        Assert.IsType<OkObjectResult>(await sut.ArchiveVehicleAsync(vehicleId, token));
        Assert.IsType<OkObjectResult>(await sut.UnarchiveVehicleAsync(vehicleId, token));
        Assert.IsType<OkObjectResult>(await sut.GetMaintenanceItemsAsync(vehicleId, token));
        Assert.Equal(StatusCodes.Status201Created, Assert.IsType<ObjectResult>(await sut.CreateMaintenanceItemAsync(vehicleId, maintenanceRequest, token)).StatusCode);
        Assert.IsType<OkObjectResult>(await sut.UpdateMaintenanceItemAsync(vehicleId, maintenanceId, updateMaintenanceRequest, token));
        Assert.IsType<NoContentResult>(await sut.DeleteMaintenanceItemAsync(vehicleId, maintenanceId, token));
        Assert.IsType<OkObjectResult>(await sut.GetHistoryAsync(vehicleId, 2, 10, token));
        Assert.Equal(StatusCodes.Status201Created, Assert.IsType<ObjectResult>(await sut.CreateHistoryAsync(vehicleId, historyRequest, token)).StatusCode);
        Assert.IsType<OkObjectResult>(await sut.UpdateHistoryAsync(vehicleId, historyId, updateHistoryRequest, token));
        Assert.IsType<NoContentResult>(await sut.DeleteHistoryAsync(vehicleId, historyId, token));
        Assert.IsType<OkObjectResult>(await sut.GetInsurancePoliciesAsync(vehicleId, token));
        Assert.Equal(StatusCodes.Status201Created, Assert.IsType<ObjectResult>(await sut.CreateInsurancePolicyAsync(vehicleId, insuranceRequest, token)).StatusCode);
        Assert.IsType<OkObjectResult>(await sut.UpdateInsurancePolicyAsync(vehicleId, insuranceId, updateInsuranceRequest, token));
        Assert.IsType<NoContentResult>(await sut.DeleteInsurancePolicyAsync(vehicleId, insuranceId, token));

        wrapper.VerifyAll();
    }

    private static MyGarageController CreateController(ICarServiceWrapper wrapper) => new(wrapper);
}
