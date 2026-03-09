using Defender.Common.Attributes;
using Defender.Common.Consts;
using Defender.PersonalFoodAdvisor.Application.DTOs;
using Defender.PersonalFoodAdvisor.Application.Modules.Ratings.Commands;
using Defender.PersonalFoodAdvisor.Application.Modules.Ratings.Queries;
using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Roles = Defender.Common.Consts.Roles;

namespace WebApi.Controllers.V1;

[Route("api/V1/[controller]")]
[ApiController]
public class RatingController(
    IMediator mediator,
    IMapper mapper,
    Defender.Common.Interfaces.ICurrentAccountAccessor currentAccountAccessor,
    ILogger<RatingController> logger) : BaseApiController(mediator, mapper)
{
    [HttpGet]
    [Auth(Roles.User)]
    [ProducesResponseType(typeof(IReadOnlyList<Defender.PersonalFoodAdvisor.Domain.Entities.DishRating>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IReadOnlyList<Defender.PersonalFoodAdvisor.Domain.Entities.DishRating>>> Get(CancellationToken cancellationToken)
    {
        var userId = currentAccountAccessor.GetAccountId();
        logger.LogInformation("Get ratings requested for user {UserId}", userId);

        var query = new GetUserRatingsQuery
        {
            UserId = userId
        };

        var ratings = await ProcessApiCallWithoutMappingAsync<GetUserRatingsQuery, IReadOnlyList<Defender.PersonalFoodAdvisor.Domain.Entities.DishRating>>(query);

        logger.LogInformation("Returning {Count} ratings for user {UserId}", ratings.Count, userId);
        return Ok(ratings);
    }

    [HttpPost]
    [Auth(Roles.User)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> Submit([FromBody] SubmitRatingRequest request, CancellationToken cancellationToken)
    {
        var userId = currentAccountAccessor.GetAccountId();
        logger.LogInformation("Submit rating requested: user {UserId}, session {SessionId}, hasDishName {HasDishName}, rating {Rating}", userId, request?.SessionId, !string.IsNullOrWhiteSpace(request?.DishName), request?.Rating ?? 0);

        var command = new SubmitUserRatingCommand
        {
            UserId = userId,
            DishName = request?.DishName ?? string.Empty,
            Rating = request?.Rating ?? 0,
            SessionId = request?.SessionId
        };

        await ProcessApiCallAsync(command);

        logger.LogInformation("Submit rating completed: user {UserId}, session {SessionId}", userId, request?.SessionId);
        return NoContent();
    }
}
