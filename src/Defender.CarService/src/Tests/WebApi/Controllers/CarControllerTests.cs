using System.Reflection;
using AutoMapper;
using Defender.CarService.Application.DTOs;
using Defender.CarService.Application.Requests.Maintenance;
using Defender.CarService.Application.Requests.Vehicles;
using Defender.CarService.WebApi.Controllers;
using Defender.CarService.WebApi.Contracts;
using Defender.CarService.WebApi.Mapping;
using Defender.Common.Attributes;
using Defender.Common.Consts;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Defender.CarService.Tests.WebApi.Controllers;

public sealed class CarControllerTests
{
    [Fact]
    public void Controller_UsesGarageRouteAndUserAuthorization()
    {
        var route = typeof(CarController).GetCustomAttribute<RouteAttribute>();
        var authorization = typeof(CarController).GetCustomAttribute<AuthAttribute>();

        Assert.Equal("api/V1/car", route?.Template);
        Assert.Equal(Roles.User, authorization?.Roles);
    }

    [Fact]
    public void Controller_ExposesOnlyApprovedRouteTemplates()
    {
        var templates = typeof(CarController)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .SelectMany(method => method.GetCustomAttributes<HttpMethodAttribute>())
            .Select(attribute => attribute.Template)
            .Where(template => template is not null)
            .ToArray();

        Assert.Contains("vehicles", templates);
        Assert.Contains("vehicles/{vehicleId:guid}/maintenance/{maintenanceId:guid}", templates);
        Assert.Contains("vehicles/{vehicleId:guid}/history/{historyId:guid}", templates);
        Assert.Contains("vehicles/{vehicleId:guid}/insurance/{insuranceId:guid}", templates);
        Assert.DoesNotContain(templates, template => template!.Contains("tax", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(templates, template => template!.Contains("import", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(templates, template => template!.Contains("migration", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(templates, template => template!.Contains("notification", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(templates, template =>
            template!.Contains("insurance/{insuranceId:guid}", StringComparison.OrdinalIgnoreCase)
            && typeof(CarController)
                .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Where(method => method.GetCustomAttributes<HttpDeleteAttribute>().Any())
                .SelectMany(method => method.GetCustomAttributes<HttpMethodAttribute>())
                .Any(attribute => attribute.Template == template));
    }

    [Fact]
    public async Task CreateVehicle_ReturnsCreatedAtActionAndUsesRouteFreeRequest()
    {
        var vehicle = new VehicleDto { Id = Guid.NewGuid(), DisplayName = "Daily" };
        var mediator = new Mock<IMediator>();
        mediator
            .Setup(item => item.Send(It.IsAny<CreateVehicleCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(vehicle);
        var mapper = new MapperConfiguration(
            configuration => configuration.AddProfile<CarApiMappingProfile>(),
            NullLoggerFactory.Instance).CreateMapper();
        var controller = new CarController(mediator.Object, mapper);

        var result = await controller.CreateVehicleAsync(
            new CreateVehicleRequest
            {
                DisplayName = "Daily",
                Make = "Make",
                Model = "Model",
                Year = 2026,
                Plate = "ABC",
            },
            CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(nameof(CarController.GetVehicleAsync), created.ActionName);
        Assert.Equal(vehicle, created.Value);
        mediator.Verify(item => item.Send(
            It.Is<CreateVehicleCommand>(command => command.DisplayName == "Daily" && command.GetType().GetProperties().All(property => property.Name != "UserId")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteHistory_ReturnsNoContent()
    {
        var mediator = new Mock<IMediator>();
        mediator
            .Setup(item => item.Send(It.IsAny<IRequest<Unit>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Unit.Value);
        var mapper = new MapperConfiguration(
            configuration => configuration.AddProfile<CarApiMappingProfile>(),
            NullLoggerFactory.Instance).CreateMapper();
        var controller = new CarController(mediator.Object, mapper);

        var result = await controller.DeleteHistoryAsync(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public void MaintenanceRequestMapping_PreservesManualBaselineFields()
    {
        var mapper = new MapperConfiguration(
            configuration => configuration.AddProfile<CarApiMappingProfile>(),
            NullLoggerFactory.Instance).CreateMapper();
        var date = new DateOnly(2025, 1, 2);

        var create = mapper.Map<CreateMaintenanceItemCommand>(new CreateMaintenanceItemRequest
        {
            ManualBaselineDate = date,
            ManualBaselineOdometerKm = 40_000,
        });
        var update = mapper.Map<UpdateMaintenanceItemCommand>(new UpdateMaintenanceItemRequest
        {
            ManualBaselineDate = date,
            ManualBaselineOdometerKm = 40_000,
        });

        Assert.Equal(date, create.ManualBaselineDate);
        Assert.Equal(40_000, create.ManualBaselineOdometerKm);
        Assert.Equal(date, update.ManualBaselineDate);
        Assert.Equal(40_000, update.ManualBaselineOdometerKm);
    }

}
