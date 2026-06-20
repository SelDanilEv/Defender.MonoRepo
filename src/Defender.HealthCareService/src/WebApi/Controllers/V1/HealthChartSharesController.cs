using Defender.Common.Attributes;
using Defender.Common.Consts;
using Defender.HealthCareService.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers.V1;

public record HealthChartShareRequest(IReadOnlyList<HealthEvent> Events);
public record HealthChartShareDto(string Token, string PublicUrl, IReadOnlyList<HealthEvent> Events, DateTimeOffset CreatedAtUtc);

public class HealthChartSharesController : ControllerBase
{
    private static readonly Dictionary<string, HealthChartShareDto> Shares = new();

    [HttpPost("api/health-chart-shares")]
    [Auth(Roles.User)]
    [ProducesResponseType(typeof(HealthChartShareDto), StatusCodes.Status201Created)]
    public ActionResult<HealthChartShareDto> CreateShare([FromBody] HealthChartShareRequest request)
    {
        var token = Guid.NewGuid().ToString("N");
        var share = new HealthChartShareDto(
            token,
            $"/api/public/health-chart-shares/{token}",
            request.Events.OrderBy(e => e.StartedAt).ToList(),
            DateTimeOffset.UtcNow);

        Shares[token] = share;
        return Created(share.PublicUrl, share);
    }

    [HttpGet("api/public/health-chart-shares/{token}")]
    [ProducesResponseType(typeof(HealthChartShareDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<HealthChartShareDto> GetPublicShare(string token)
    {
        return Shares.TryGetValue(token, out var share) ? Ok(share) : NotFound();
    }
}
