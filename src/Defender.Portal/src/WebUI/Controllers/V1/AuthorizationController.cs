using AutoMapper;
using Defender.Common.Helpers;
using Defender.Portal.Application.DTOs.Auth;
using Defender.Portal.Application.Modules.Authorization.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Defender.Portal.WebUI.Controllers.V1;

public class AuthorizationController(IMediator mediator, IMapper mapper) : BaseApiController(mediator, mapper)
{
    [HttpPost("login")]
    [ProducesResponseType(typeof(SessionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> LoginWithPasswordAsync(
        [FromBody] LoginWithPasswordCommand command)
    {
        var session = await _mediator.Send(command);
        AuthCookieHelper.SetAuthCookie(Response, session.Token ?? string.Empty);

        return Ok(session);
    }

    [HttpPost("google")]
    [ProducesResponseType(typeof(SessionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> LoginWithGoogleTokenAsync(
        [FromBody] LoginWithGoogleTokenCommand command)
    {
        var session = await _mediator.Send(command);
        AuthCookieHelper.SetAuthCookie(Response, session.Token ?? string.Empty);

        return Ok(session);
    }

    [HttpPost("create")]
    [ProducesResponseType(typeof(SessionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> CreateUserAsync(
        [FromBody] CreateAccountCommand command)
    {
        var session = await _mediator.Send(command);
        AuthCookieHelper.SetAuthCookie(Response, session.Token ?? string.Empty);

        return Ok(session);
    }

    [HttpPost("logout")]
    [ProducesResponseType(typeof(void), StatusCodes.Status200OK)]
    public ActionResult Logout()
    {
        AuthCookieHelper.ClearAuthCookie(Response);
        return Ok();
    }
}
