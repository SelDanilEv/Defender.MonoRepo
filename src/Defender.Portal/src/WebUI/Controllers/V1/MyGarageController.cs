using Defender.Common.Attributes;
using Defender.Common.Consts;
using Defender.Portal.Application.Common.Interfaces.Wrappers;
using Defender.Portal.Application.Models.ApiRequests.MyGarage;
using Microsoft.AspNetCore.Mvc;

namespace Defender.Portal.WebUI.Controllers.V1;

[ApiController]
[Route("api/my-garage")]
[Auth(Roles.User)]
public sealed class MyGarageController(ICarServiceWrapper wrapper) : ControllerBase
{
    [HttpGet("vehicles")]
    public async Task<IActionResult> GetVehiclesAsync(
        [FromQuery] bool includeArchived = false,
        [FromQuery] int page = 0,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default) =>
        Ok(await wrapper.GetVehiclesAsync(includeArchived, page, pageSize, cancellationToken));

    [HttpPost("vehicles")]
    public async Task<IActionResult> CreateVehicleAsync(
        [FromBody] CreateVehicleRequest request,
        CancellationToken cancellationToken = default) =>
        StatusCode(StatusCodes.Status201Created, await wrapper.CreateVehicleAsync(request, cancellationToken));

    [HttpGet("vehicles/{vehicleId:guid}")]
    public async Task<IActionResult> GetVehicleAsync(
        Guid vehicleId,
        CancellationToken cancellationToken = default) =>
        Ok(await wrapper.GetVehicleAsync(vehicleId, cancellationToken));

    [HttpPut("vehicles/{vehicleId:guid}")]
    public async Task<IActionResult> UpdateVehicleAsync(
        Guid vehicleId,
        [FromBody] UpdateVehicleRequest request,
        CancellationToken cancellationToken = default) =>
        Ok(await wrapper.UpdateVehicleAsync(vehicleId, request, cancellationToken));

    [HttpPost("vehicles/{vehicleId:guid}/archive")]
    public async Task<IActionResult> ArchiveVehicleAsync(
        Guid vehicleId,
        CancellationToken cancellationToken = default) =>
        Ok(await wrapper.ArchiveVehicleAsync(vehicleId, cancellationToken));

    [HttpPost("vehicles/{vehicleId:guid}/unarchive")]
    public async Task<IActionResult> UnarchiveVehicleAsync(
        Guid vehicleId,
        CancellationToken cancellationToken = default) =>
        Ok(await wrapper.UnarchiveVehicleAsync(vehicleId, cancellationToken));

    [HttpGet("vehicles/{vehicleId:guid}/maintenance")]
    public async Task<IActionResult> GetMaintenanceItemsAsync(
        Guid vehicleId,
        CancellationToken cancellationToken = default) =>
        Ok(await wrapper.GetMaintenanceItemsAsync(vehicleId, cancellationToken));

    [HttpPost("vehicles/{vehicleId:guid}/maintenance")]
    public async Task<IActionResult> CreateMaintenanceItemAsync(
        Guid vehicleId,
        [FromBody] CreateMaintenanceItemRequest request,
        CancellationToken cancellationToken = default) =>
        StatusCode(StatusCodes.Status201Created, await wrapper.CreateMaintenanceItemAsync(vehicleId, request, cancellationToken));

    [HttpPut("vehicles/{vehicleId:guid}/maintenance/{maintenanceId:guid}")]
    public async Task<IActionResult> UpdateMaintenanceItemAsync(
        Guid vehicleId,
        Guid maintenanceId,
        [FromBody] UpdateMaintenanceItemRequest request,
        CancellationToken cancellationToken = default) =>
        Ok(await wrapper.UpdateMaintenanceItemAsync(vehicleId, maintenanceId, request, cancellationToken));

    [HttpDelete("vehicles/{vehicleId:guid}/maintenance/{maintenanceId:guid}")]
    public async Task<IActionResult> DeleteMaintenanceItemAsync(
        Guid vehicleId,
        Guid maintenanceId,
        CancellationToken cancellationToken = default)
    {
        await wrapper.DeleteMaintenanceItemAsync(vehicleId, maintenanceId, cancellationToken);
        return NoContent();
    }

    [HttpGet("vehicles/{vehicleId:guid}/history")]
    public async Task<IActionResult> GetHistoryAsync(
        Guid vehicleId,
        [FromQuery] int page = 0,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default) =>
        Ok(await wrapper.GetHistoryAsync(vehicleId, page, pageSize, cancellationToken));

    [HttpPost("vehicles/{vehicleId:guid}/history")]
    public async Task<IActionResult> CreateHistoryAsync(
        Guid vehicleId,
        [FromBody] CreateServiceHistoryRequest request,
        CancellationToken cancellationToken = default) =>
        StatusCode(StatusCodes.Status201Created, await wrapper.CreateHistoryAsync(vehicleId, request, cancellationToken));

    [HttpPut("vehicles/{vehicleId:guid}/history/{historyId:guid}")]
    public async Task<IActionResult> UpdateHistoryAsync(
        Guid vehicleId,
        Guid historyId,
        [FromBody] UpdateServiceHistoryRequest request,
        CancellationToken cancellationToken = default) =>
        Ok(await wrapper.UpdateHistoryAsync(vehicleId, historyId, request, cancellationToken));

    [HttpDelete("vehicles/{vehicleId:guid}/history/{historyId:guid}")]
    public async Task<IActionResult> DeleteHistoryAsync(
        Guid vehicleId,
        Guid historyId,
        CancellationToken cancellationToken = default)
    {
        await wrapper.DeleteHistoryAsync(vehicleId, historyId, cancellationToken);
        return NoContent();
    }

    [HttpGet("vehicles/{vehicleId:guid}/insurance")]
    public async Task<IActionResult> GetInsurancePoliciesAsync(
        Guid vehicleId,
        CancellationToken cancellationToken = default) =>
        Ok(await wrapper.GetInsurancePoliciesAsync(vehicleId, cancellationToken));

    [HttpPost("vehicles/{vehicleId:guid}/insurance")]
    public async Task<IActionResult> CreateInsurancePolicyAsync(
        Guid vehicleId,
        [FromBody] CreateInsurancePolicyRequest request,
        CancellationToken cancellationToken = default) =>
        StatusCode(StatusCodes.Status201Created, await wrapper.CreateInsurancePolicyAsync(vehicleId, request, cancellationToken));

    [HttpPut("vehicles/{vehicleId:guid}/insurance/{insuranceId:guid}")]
    public async Task<IActionResult> UpdateInsurancePolicyAsync(
        Guid vehicleId,
        Guid insuranceId,
        [FromBody] UpdateInsurancePolicyRequest request,
        CancellationToken cancellationToken = default) =>
        Ok(await wrapper.UpdateInsurancePolicyAsync(vehicleId, insuranceId, request, cancellationToken));

    [HttpDelete("vehicles/{vehicleId:guid}/insurance/{insuranceId:guid}")]
    public async Task<IActionResult> DeleteInsurancePolicyAsync(
        Guid vehicleId,
        Guid insuranceId,
        CancellationToken cancellationToken = default)
    {
        await wrapper.DeleteInsurancePolicyAsync(vehicleId, insuranceId, cancellationToken);
        return NoContent();
    }
}
