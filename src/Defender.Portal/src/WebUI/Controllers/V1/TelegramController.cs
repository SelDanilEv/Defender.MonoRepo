using Defender.Common.Attributes;
using Defender.Common.Consts;
using Defender.Common.Helpers;
using Defender.Common.Interfaces;
using Defender.Portal.Application.DTOs.Auth;
using Defender.Portal.Application.Modules.Telegram;
using Defender.Portal.WebUI.Telegram;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Defender.Portal.WebUI.Controllers.V1;

[ApiController]
[Route("api/telegram")]
public sealed class TelegramController(
    ITelegramSessionService sessionService,
    ITelegramWebhookSecretValidator webhookSecretValidator,
    ITelegramWebhookService webhookService,
    ICurrentAccountAccessor currentAccountAccessor) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("session")]
    [ProducesResponseType(typeof(SessionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<SessionDto>> CreateSessionAsync(
        [FromBody] TelegramInitDataRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await sessionService.CreateSessionAsync(request.InitData, cancellationToken);
            AuthCookieHelper.ClearAuthCookie(Response);
            TelegramSessionCookieHelper.SetCookie(Response, result.CookieToken);
            return Ok(result.Session);
        }
        catch (TelegramInitDataValidationException)
        {
            return Unauthorized();
        }
        catch (TelegramAccountNotLinkedException)
        {
            return Unauthorized();
        }
    }

    [HttpPost("link")]
    [Auth(Roles.User)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult> LinkAsync(
        [FromBody] TelegramInitDataRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            await sessionService.LinkAsync(currentAccountAccessor.GetAccountId(), request.InitData, cancellationToken);
            return NoContent();
        }
        catch (TelegramInitDataValidationException)
        {
            return Unauthorized();
        }
        catch (TelegramAccountLinkConflictException)
        {
            return Conflict();
        }
    }

    [HttpDelete("link")]
    [Auth(Roles.User)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult> UnlinkAsync(CancellationToken cancellationToken)
    {
        await sessionService.UnlinkAsync(currentAccountAccessor.GetAccountId(), cancellationToken);
        TelegramSessionCookieHelper.ClearCookie(Response);
        return NoContent();
    }

    [AllowAnonymous]
    [HttpPost("/api/integrations/telegram/webhook")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> ReceiveWebhookAsync(
        [FromHeader(Name = "X-Telegram-Bot-Api-Secret-Token")] string? secret,
        [FromBody] TelegramWebhookUpdate? update,
        CancellationToken cancellationToken)
    {
        try
        {
            webhookSecretValidator.Validate(secret);
        }
        catch (TelegramWebhookValidationException)
        {
            return Unauthorized();
        }

        if (update is null || update.UpdateId < 0)
        {
            return BadRequest();
        }

        if (await webhookService.TryRecordAsync(update.UpdateId, cancellationToken))
        {
            await webhookService.HandleAsync(update, cancellationToken);
        }
        return Ok();
    }
}

public sealed record TelegramInitDataRequest(string InitData);
