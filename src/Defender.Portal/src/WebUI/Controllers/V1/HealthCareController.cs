using Defender.Common.Attributes;
using Defender.Common.Consts;
using Defender.Portal.Application.Common.Interfaces.Wrappers;
using Defender.Portal.Application.DTOs.HealthCare;
using Defender.Portal.Application.Models.ApiRequests.HealthCare;
using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Defender.Portal.WebUI.Controllers.V1;

public class HealthCareController(
    IMediator mediator,
    IMapper mapper,
    IHealthCareWrapper healthCareWrapper) : BaseApiController(mediator, mapper)
{
    [HttpGet("events")]
    [Auth(Roles.User)]
    [ProducesResponseType(typeof(IReadOnlyList<PortalHealthEventDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult> GetEvents([FromQuery] DateTimeOffset? from, [FromQuery] DateTimeOffset? to, CancellationToken cancellationToken)
    {
        return Ok(await healthCareWrapper.GetEventsAsync(from, to, cancellationToken));
    }

    [HttpPost("events")]
    [Auth(Roles.User)]
    [ProducesResponseType(typeof(PortalHealthEventDto), StatusCodes.Status201Created)]
    public async Task<ActionResult> CreateEvent([FromBody] PortalHealthEventDto healthEvent, CancellationToken cancellationToken)
    {
        var result = await healthCareWrapper.CreateEventAsync(healthEvent, cancellationToken);
        return CreatedAtAction(nameof(GetEvents), new { id = result.Id }, result);
    }

    [HttpPut("events/{id:guid}")]
    [Auth(Roles.User)]
    [ProducesResponseType(typeof(PortalHealthEventDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> UpdateEvent(Guid id, [FromBody] PortalHealthEventDto healthEvent, CancellationToken cancellationToken)
    {
        var result = await healthCareWrapper.UpdateEventAsync(id, healthEvent, cancellationToken);
        return result == null ? NotFound() : Ok(result);
    }

    [HttpDelete("events/{id:guid}")]
    [Auth(Roles.User)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteEvent(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await healthCareWrapper.DeleteEventAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }

    [HttpPost("chart-shares")]
    [Auth(Roles.User)]
    [ProducesResponseType(typeof(PortalHealthChartShareDto), StatusCodes.Status201Created)]
    public async Task<ActionResult> CreateShare([FromBody] CreateHealthChartShareRequest request, CancellationToken cancellationToken)
    {
        var result = await healthCareWrapper.CreateShareAsync(request, cancellationToken);
        var publicUrl = $"/health-care/share/{result.Token}";
        return Created(publicUrl, result with { PublicUrl = publicUrl });
    }

    [HttpGet("chart-shares/current")]
    [Auth(Roles.User)]
    [ProducesResponseType(typeof(PortalHealthChartShareDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GetCurrentShare(CancellationToken cancellationToken)
    {
        var result = await healthCareWrapper.GetCurrentShareAsync(cancellationToken);
        return result == null ? NotFound() : Ok(result with { PublicUrl = $"/health-care/share/{result.Token}" });
    }

    [HttpPut("chart-shares/status")]
    [Auth(Roles.User)]
    [ProducesResponseType(typeof(PortalHealthChartShareDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> UpdateShareStatus([FromBody] UpdateHealthChartShareStatusRequest request, CancellationToken cancellationToken)
    {
        var result = await healthCareWrapper.UpdateShareStatusAsync(request, cancellationToken);
        return result == null ? NotFound() : Ok(result with { PublicUrl = $"/health-care/share/{result.Token}" });
    }

    [HttpGet("public/chart-shares/{token}")]
    [ProducesResponseType(typeof(PortalHealthChartShareDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GetPublicShare(string token, CancellationToken cancellationToken)
    {
        var result = await healthCareWrapper.GetPublicShareAsync(token, cancellationToken);
        return result == null ? NotFound() : Ok(result with { PublicUrl = $"/health-care/share/{result.Token}" });
    }
}
