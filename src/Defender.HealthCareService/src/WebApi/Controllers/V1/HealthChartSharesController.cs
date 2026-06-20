using Defender.Common.Attributes;
using Defender.Common.Consts;
using Defender.Common.Interfaces;
using Defender.HealthCareService.Application.Common.Interfaces.Repositories;
using Defender.HealthCareService.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers.V1;

public record HealthChartShareRequest(DateTimeOffset? From, DateTimeOffset? To);
public record HealthChartShareDto(string Token, string PublicUrl, IReadOnlyList<HealthEvent> Events, DateTimeOffset CreatedAtUtc);

public class HealthChartSharesController(
    ICurrentAccountAccessor currentAccountAccessor,
    IHealthEventRepository healthEventRepository,
    IHealthChartShareRepository healthChartShareRepository) : ControllerBase
{
    [HttpPost("api/health-chart-shares")]
    [Auth(Roles.User)]
    [ProducesResponseType(typeof(HealthChartShareDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<HealthChartShareDto>> CreateShare([FromBody] HealthChartShareRequest request)
    {
        var userId = currentAccountAccessor.GetAccountId();
        var share = await healthChartShareRepository.AddHealthChartShareAsync(new HealthChartShare
        {
            Token = Guid.NewGuid().ToString("N"),
            UserId = userId,
            From = request.From,
            To = request.To,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        });
        var events = await healthEventRepository.GetHealthEventsAsync(userId, request.From, request.To);
        var shareDto = new HealthChartShareDto(
            share.Token,
            $"/api/public/health-chart-shares/{share.Token}",
            events,
            share.CreatedAtUtc);

        return Created(shareDto.PublicUrl, shareDto);
    }

    [HttpGet("api/public/health-chart-shares/{token}")]
    [ProducesResponseType(typeof(HealthChartShareDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<HealthChartShareDto>> GetPublicShare(string token)
    {
        var share = await healthChartShareRepository.GetHealthChartShareByTokenAsync(token);

        if (share == null)
        {
            return NotFound();
        }

        var events = await healthEventRepository.GetHealthEventsAsync(share.UserId, share.From, share.To);
        return Ok(new HealthChartShareDto(
            share.Token,
            $"/api/public/health-chart-shares/{share.Token}",
            events,
            share.CreatedAtUtc));
    }
}
