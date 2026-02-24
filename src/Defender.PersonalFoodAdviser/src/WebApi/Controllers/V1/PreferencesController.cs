using Defender.Common.Attributes;
using Defender.Common.Consts;
using Defender.PersonalFoodAdviser.Application.Common.Interfaces.Services;
using Roles = Defender.Common.Consts.Roles;
using Defender.PersonalFoodAdviser.Application.DTOs;
using Defender.PersonalFoodAdviser.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace WebApi.Controllers.V1;

[Route("api/V1/[controller]")]
[ApiController]
public class PreferencesController(
    IPreferencesService preferencesService,
    Defender.Common.Interfaces.ICurrentAccountAccessor currentAccountAccessor,
    ILogger<PreferencesController> logger) : ControllerBase
{
    [HttpGet]
    [Auth(Roles.User)]
    [ProducesResponseType(typeof(UserPreferences), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<UserPreferences>> Get(CancellationToken cancellationToken)
    {
        var userId = currentAccountAccessor.GetAccountId();
        logger.LogInformation("Get preferences requested by user {UserId}", userId);
        var preferences = await preferencesService.GetByUserIdAsync(userId, cancellationToken);
        logger.LogInformation("Preferences returned for user {UserId}: likes {LikesCount}, dislikes {DislikesCount}", userId, preferences.Likes.Count, preferences.Dislikes.Count);
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
        logger.LogInformation(
            "Update preferences requested by user {UserId}: likes {LikesCount}, dislikes {DislikesCount}",
            userId,
            request?.Likes?.Count ?? 0,
            request?.Dislikes?.Count ?? 0);

        var preferences = await preferencesService.UpdateAsync(
            userId,
            request?.Likes ?? [],
            request?.Dislikes ?? [],
            cancellationToken);

        logger.LogInformation("Preferences updated for user {UserId}: likes {LikesCount}, dislikes {DislikesCount}", userId, preferences.Likes.Count, preferences.Dislikes.Count);
        return Ok(preferences);
    }
}
