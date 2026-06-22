using Defender.Common.Attributes;
using Defender.Common.Consts;
using Defender.Common.Interfaces;
using Defender.HealthCareService.Application.Common.Interfaces.Repositories;
using Defender.HealthCareService.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers.V1;

public record HealthChartShareRequest(DateTimeOffset? From, DateTimeOffset? To);
public record HealthChartShareStatusRequest(bool IsEnabled);
public record HealthChartShareDto(
    string Token,
    string PublicUrl,
    IReadOnlyList<HealthEvent> Events,
    DateTimeOffset? From,
    DateTimeOffset? To,
    bool IsEnabled,
    DateTimeOffset CreatedAtUtc);

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
        var share = await healthChartShareRepository.GetHealthChartShareByUserIdAsync(userId);

        if (share == null)
        {
            share = await healthChartShareRepository.AddHealthChartShareAsync(new HealthChartShare
            {
                Token = Guid.NewGuid().ToString("N"),
                UserId = userId,
                From = request.From,
                To = request.To,
                IsEnabled = true,
                CreatedAtUtc = DateTimeOffset.UtcNow,
            });
        }
        else
        {
            share.From = request.From;
            share.To = request.To;
            share.IsEnabled = true;
            share = await healthChartShareRepository.UpdateHealthChartShareAsync(share);
        }

        await healthChartShareRepository.DisableOtherHealthChartSharesAsync(userId, share.Id);

        var events = await healthEventRepository.GetHealthEventsAsync(userId, request.From, request.To);
        var shareDto = ToDto(share, events);

        return Created(shareDto.PublicUrl, shareDto);
    }

    [HttpGet("api/health-chart-shares/current")]
    [Auth(Roles.User)]
    [ProducesResponseType(typeof(HealthChartShareDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<HealthChartShareDto>> GetCurrentShare()
    {
        var share = await healthChartShareRepository.GetHealthChartShareByUserIdAsync(currentAccountAccessor.GetAccountId());

        return share == null ? NotFound() : Ok(ToDto(share, []));
    }

    [HttpPut("api/health-chart-shares/status")]
    [Auth(Roles.User)]
    [ProducesResponseType(typeof(HealthChartShareDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<HealthChartShareDto>> UpdateShareStatus([FromBody] HealthChartShareStatusRequest request)
    {
        var share = await healthChartShareRepository.GetHealthChartShareByUserIdAsync(currentAccountAccessor.GetAccountId());

        if (share == null)
        {
            return NotFound();
        }

        share.IsEnabled = request.IsEnabled;
        share = await healthChartShareRepository.UpdateHealthChartShareAsync(share);
        await healthChartShareRepository.DisableOtherHealthChartSharesAsync(share.UserId, share.Id);

        return Ok(ToDto(share, []));
    }

    [HttpGet("api/public/health-chart-shares/{token}")]
    [ProducesResponseType(typeof(HealthChartShareDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<HealthChartShareDto>> GetPublicShare(string token)
    {
        var share = await healthChartShareRepository.GetHealthChartShareByTokenAsync(token);

        if (share == null || !share.IsEnabled)
        {
            return NotFound();
        }

        var (from, to) = GetEffectivePublicRange(share);
        var events = await healthEventRepository.GetHealthEventsAsync(share.UserId, from, to);
        return Ok(ToDto(share, events, from, to));
    }

    private static (DateTimeOffset? From, DateTimeOffset? To) GetEffectivePublicRange(HealthChartShare share)
    {
        if (share.From == null || share.To == null)
        {
            return (share.From, share.To);
        }

        var range = share.To.Value - share.From.Value;

        if (range <= TimeSpan.Zero)
        {
            return (share.From, share.To);
        }

        var to = DateTimeOffset.UtcNow;
        return (to - range, to);
    }

    private static HealthChartShareDto ToDto(
        HealthChartShare share,
        IReadOnlyList<HealthEvent> events,
        DateTimeOffset? from = null,
        DateTimeOffset? to = null) =>
        new(
            share.Token,
            $"/api/public/health-chart-shares/{share.Token}",
            events,
            from ?? share.From,
            to ?? share.To,
            share.IsEnabled,
            share.CreatedAtUtc);
}
