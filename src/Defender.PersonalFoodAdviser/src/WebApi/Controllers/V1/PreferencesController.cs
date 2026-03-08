using Defender.Common.Attributes;
using Defender.Common.Consts;
using Defender.PersonalFoodAdviser.Application.DTOs;
using Defender.PersonalFoodAdviser.Application.Modules.Preferences.Commands;
using Defender.PersonalFoodAdviser.Application.Modules.Preferences.Queries;
using Defender.PersonalFoodAdviser.Domain.Entities;
using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Roles = Defender.Common.Consts.Roles;

namespace WebApi.Controllers.V1;

[Route("api/V1/[controller]")]
[ApiController]
public class PreferencesController(
    IMediator mediator,
    IMapper mapper,
    Defender.Common.Interfaces.ICurrentAccountAccessor currentAccountAccessor,
    ILogger<PreferencesController> logger) : BaseApiController(mediator, mapper)
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

        var query = new GetUserPreferencesQuery
        {
            UserId = userId
        };

        var preferences = await ProcessApiCallWithoutMappingAsync<GetUserPreferencesQuery, UserPreferences>(query);

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

        var command = new UpdateUserPreferencesCommand
        {
            UserId = userId,
            Likes = request?.Likes ?? [],
            Dislikes = request?.Dislikes ?? []
        };

        var preferences = await ProcessApiCallWithoutMappingAsync<UpdateUserPreferencesCommand, UserPreferences>(command);

        logger.LogInformation("Preferences updated for user {UserId}: likes {LikesCount}, dislikes {DislikesCount}", userId, preferences.Likes.Count, preferences.Dislikes.Count);
        return Ok(preferences);
    }
}
