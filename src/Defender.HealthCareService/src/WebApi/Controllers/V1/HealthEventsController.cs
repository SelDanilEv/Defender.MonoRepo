using Defender.Common.Attributes;
using Defender.Common.Consts;
using Defender.HealthCareService.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers.V1;

public class HealthEventsController : ControllerBase
{
    private static readonly List<HealthEvent> Events = [];

    [HttpGet("api/health-events")]
    [Auth(Roles.User)]
    [ProducesResponseType(typeof(IReadOnlyList<HealthEvent>), StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<HealthEvent>> GetEvents([FromQuery] Guid userId, [FromQuery] DateTimeOffset? from, [FromQuery] DateTimeOffset? to)
    {
        var result = Events
            .Where(e => userId == Guid.Empty || e.UserId == userId)
            .Where(e => from == null || e.StartedAt >= from)
            .Where(e => to == null || e.StartedAt <= to)
            .OrderBy(e => e.StartedAt)
            .ToList();

        return Ok(result);
    }

    [HttpPost("api/health-events")]
    [Auth(Roles.User)]
    [ProducesResponseType(typeof(HealthEvent), StatusCodes.Status201Created)]
    public ActionResult<HealthEvent> CreateEvent([FromBody] HealthEvent request)
    {
        request.Id = request.Id == Guid.Empty ? Guid.NewGuid() : request.Id;
        request.StartedAt = SnapToHalfHour(request.StartedAt);
        if (request.EndedAt != null)
        {
            request.EndedAt = SnapToHalfHour(request.EndedAt.Value);
        }

        Events.Add(request);
        return Created($"/api/health-events/{request.Id}", request);
    }

    [HttpDelete("api/health-events/{id:guid}")]
    [Auth(Roles.User)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult DeleteEvent(Guid id)
    {
        var removed = Events.RemoveAll(e => e.Id == id) > 0;
        return removed ? NoContent() : NotFound();
    }

    private static DateTimeOffset SnapToHalfHour(DateTimeOffset value)
    {
        var minutes = value.Minute < 30 ? 0 : 30;
        return new DateTimeOffset(value.Year, value.Month, value.Day, value.Hour, minutes, 0, value.Offset);
    }
}
