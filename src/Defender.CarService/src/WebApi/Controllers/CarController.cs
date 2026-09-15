using AutoMapper;
using Defender.CarService.Application.DTOs;
using Defender.CarService.Application.Requests.History;
using Defender.CarService.Application.Requests.Insurance;
using Defender.CarService.Application.Requests.Maintenance;
using Defender.CarService.Application.Requests.Vehicles;
using Defender.CarService.WebApi.Contracts;
using Defender.Common.Attributes;
using Defender.Common.Consts;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Defender.CarService.WebApi.Controllers;

[ApiController]
[Route("api/V1/car")]
[Auth(Roles.User)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
public sealed class CarController(IMediator mediator, IMapper mapper) : ControllerBase
{
    [HttpGet("vehicles")]
    [ProducesResponseType(typeof(IReadOnlyList<VehicleSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<VehicleSummaryDto>>> GetVehiclesAsync(
        [FromQuery] bool includeArchived = false,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new GetVehiclesQuery { IncludeArchived = includeArchived }, cancellationToken);
        return Ok(result);
    }

    [HttpPost("vehicles")]
    [ProducesResponseType(typeof(VehicleDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<VehicleDto>> CreateVehicleAsync(
        [FromBody] CreateVehicleRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = mapper.Map<CreateVehicleCommand>(request);
        var result = await mediator.Send(command, cancellationToken);
        return CreatedAtRoute("Car_GetVehicle", new { vehicleId = result.Id }, result);
    }

    [HttpGet("vehicles/{vehicleId:guid}", Name = "Car_GetVehicle")]
    [ProducesResponseType(typeof(VehicleDetailDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<VehicleDetailDto>> GetVehicleAsync(
        Guid vehicleId,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new GetVehicleQuery { VehicleId = vehicleId }, cancellationToken);
        return Ok(result);
    }

    [HttpPut("vehicles/{vehicleId:guid}")]
    [ProducesResponseType(typeof(VehicleDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<VehicleDto>> UpdateVehicleAsync(
        Guid vehicleId,
        [FromBody] UpdateVehicleRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = mapper.Map<UpdateVehicleCommand>(request) with { VehicleId = vehicleId };
        var result = await mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpPost("vehicles/{vehicleId:guid}/archive")]
    [ProducesResponseType(typeof(VehicleDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<VehicleDto>> ArchiveVehicleAsync(
        Guid vehicleId,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new ArchiveVehicleCommand { VehicleId = vehicleId }, cancellationToken);
        return Ok(result);
    }

    [HttpPost("vehicles/{vehicleId:guid}/unarchive")]
    [ProducesResponseType(typeof(VehicleDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<VehicleDto>> UnarchiveVehicleAsync(
        Guid vehicleId,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new UnarchiveVehicleCommand { VehicleId = vehicleId }, cancellationToken);
        return Ok(result);
    }

    [HttpGet("vehicles/{vehicleId:guid}/maintenance", Name = "Car_GetMaintenanceItems")]
    [ProducesResponseType(typeof(IReadOnlyList<MaintenanceItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<MaintenanceItemDto>>> GetMaintenanceItemsAsync(
        Guid vehicleId,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new GetMaintenanceItemsQuery { VehicleId = vehicleId }, cancellationToken);
        return Ok(result);
    }

    [HttpPost("vehicles/{vehicleId:guid}/maintenance")]
    [ProducesResponseType(typeof(MaintenanceItemDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<MaintenanceItemDto>> CreateMaintenanceItemAsync(
        Guid vehicleId,
        [FromBody] CreateMaintenanceItemRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = mapper.Map<CreateMaintenanceItemCommand>(request) with { VehicleId = vehicleId };
        var result = await mediator.Send(command, cancellationToken);
        return CreatedAtRoute("Car_GetMaintenanceItems", new { vehicleId }, result);
    }

    [HttpPut("vehicles/{vehicleId:guid}/maintenance/{maintenanceId:guid}")]
    [ProducesResponseType(typeof(MaintenanceItemDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<MaintenanceItemDto>> UpdateMaintenanceItemAsync(
        Guid vehicleId,
        Guid maintenanceId,
        [FromBody] UpdateMaintenanceItemRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = mapper.Map<UpdateMaintenanceItemCommand>(request) with
        {
            VehicleId = vehicleId,
            MaintenanceItemId = maintenanceId,
        };
        var result = await mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("vehicles/{vehicleId:guid}/maintenance/{maintenanceId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteMaintenanceItemAsync(
        Guid vehicleId,
        Guid maintenanceId,
        CancellationToken cancellationToken = default)
    {
        await mediator.Send(
            new DeleteMaintenanceItemCommand
            {
                VehicleId = vehicleId,
                MaintenanceItemId = maintenanceId,
            },
            cancellationToken);
        return NoContent();
    }

    [HttpGet("vehicles/{vehicleId:guid}/history", Name = "Car_GetHistory")]
    [ProducesResponseType(typeof(ServiceHistoryPageDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ServiceHistoryPageDto>> GetHistoryAsync(
        Guid vehicleId,
        [FromQuery] int page = 0,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(
            new GetHistoryQuery
            {
                VehicleId = vehicleId,
                Page = page,
                PageSize = pageSize,
            },
            cancellationToken);
        return Ok(result);
    }

    [HttpPost("vehicles/{vehicleId:guid}/history")]
    [ProducesResponseType(typeof(ServiceHistoryRecordDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<ServiceHistoryRecordDto>> CreateHistoryAsync(
        Guid vehicleId,
        [FromBody] CreateServiceHistoryRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = mapper.Map<CreateHistoryCommand>(request) with { VehicleId = vehicleId };
        var result = await mediator.Send(command, cancellationToken);
        return CreatedAtRoute("Car_GetHistory", new { vehicleId, page = 0, pageSize = 25 }, result);
    }

    [HttpPut("vehicles/{vehicleId:guid}/history/{historyId:guid}")]
    [ProducesResponseType(typeof(ServiceHistoryRecordDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ServiceHistoryRecordDto>> UpdateHistoryAsync(
        Guid vehicleId,
        Guid historyId,
        [FromBody] UpdateServiceHistoryRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = mapper.Map<UpdateHistoryCommand>(request) with
        {
            VehicleId = vehicleId,
            HistoryId = historyId,
        };
        var result = await mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("vehicles/{vehicleId:guid}/history/{historyId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteHistoryAsync(
        Guid vehicleId,
        Guid historyId,
        CancellationToken cancellationToken = default)
    {
        await mediator.Send(
            new DeleteHistoryCommand
            {
                VehicleId = vehicleId,
                HistoryId = historyId,
            },
            cancellationToken);
        return NoContent();
    }

    [HttpGet("vehicles/{vehicleId:guid}/insurance", Name = "Car_GetInsurancePolicies")]
    [ProducesResponseType(typeof(IReadOnlyList<InsurancePolicyDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<InsurancePolicyDto>>> GetInsurancePoliciesAsync(
        Guid vehicleId,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new GetInsurancePoliciesQuery { VehicleId = vehicleId }, cancellationToken);
        return Ok(result);
    }

    [HttpPost("vehicles/{vehicleId:guid}/insurance")]
    [ProducesResponseType(typeof(InsurancePolicyDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<InsurancePolicyDto>> CreateInsurancePolicyAsync(
        Guid vehicleId,
        [FromBody] CreateInsurancePolicyRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = mapper.Map<CreateInsurancePolicyCommand>(request) with { VehicleId = vehicleId };
        var result = await mediator.Send(command, cancellationToken);
        return CreatedAtRoute("Car_GetInsurancePolicies", new { vehicleId }, result);
    }

    [HttpPut("vehicles/{vehicleId:guid}/insurance/{insuranceId:guid}")]
    [ProducesResponseType(typeof(InsurancePolicyDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<InsurancePolicyDto>> UpdateInsurancePolicyAsync(
        Guid vehicleId,
        Guid insuranceId,
        [FromBody] UpdateInsurancePolicyRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = mapper.Map<UpdateInsurancePolicyCommand>(request) with
        {
            VehicleId = vehicleId,
            InsurancePolicyId = insuranceId,
        };
        var result = await mediator.Send(command, cancellationToken);
        return Ok(result);
    }
}
