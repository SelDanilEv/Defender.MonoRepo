using Defender.Common.Attributes;
using Defender.Common.Consts;
using Defender.PersonalFoodAdviser.Application.Common.Interfaces.Services;
using Defender.PersonalFoodAdviser.Application.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Roles = Defender.Common.Consts.Roles;

namespace WebApi.Controllers.V1;

[Route("api/V1/[controller]")]
[ApiController]
public class RatingController(
    IRatingService ratingService,
    Defender.Common.Interfaces.ICurrentAccountAccessor currentAccountAccessor,
    ILogger<RatingController> logger) : ControllerBase
{
    [HttpPost]
    [Auth(Roles.User)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> Submit([FromBody] SubmitRatingRequest request, CancellationToken cancellationToken)
    {
        var userId = currentAccountAccessor.GetAccountId();
        logger.LogInformation("Submit rating requested: user {UserId}, session {SessionId}, hasDishName {HasDishName}, rating {Rating}", userId, request?.SessionId, !string.IsNullOrWhiteSpace(request?.DishName), request?.Rating ?? 0);
        await ratingService.SubmitRatingAsync(
            userId,
            request?.DishName ?? string.Empty,
            request?.Rating ?? 0,
            request?.SessionId,
            cancellationToken);
        logger.LogInformation("Submit rating completed: user {UserId}, session {SessionId}", userId, request?.SessionId);
        return NoContent();
    }
}
