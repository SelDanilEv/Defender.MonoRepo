using Defender.Common.Attributes;
using Defender.Common.Consts;
using Defender.PersonalFoodAdviser.Application.Common.Interfaces.Services;
using Roles = Defender.Common.Consts.Roles;
using Defender.PersonalFoodAdviser.Application.DTOs;
using Defender.PersonalFoodAdviser.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers.V1;

[Route("api/V1/[controller]")]
[ApiController]
public class PreferencesController(
    IPreferencesService preferencesService,
    Defender.Common.Interfaces.ICurrentAccountAccessor currentAccountAccessor) : ControllerBase
{
    [HttpGet]
    [Auth(Roles.User)]
    [ProducesResponseType(typeof(UserPreferences), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<UserPreferences>> Get(CancellationToken cancellationToken)
    {
        var userId = currentAccountAccessor.GetAccountId();
        var preferences = await preferencesService.GetByUserIdAsync(userId, cancellationToken);
        return Ok(preferences);
    }

    [HttpPut]
    [Auth(Roles.User)]
    [ProducesResponseType(typeof(UserPreferences), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<UserPreferences>> Update([FromBody] UpdatePreferencesRequest request, CancellationToken cancellationToken)
    {
        var userId = currentAccountAccessor.GetAccountId();
        var preferences = await preferencesService.UpdateAsync(
            userId,
            request?.Likes ?? [],
            request?.Dislikes ?? [],
            cancellationToken);
        return Ok(preferences);
    }
}
