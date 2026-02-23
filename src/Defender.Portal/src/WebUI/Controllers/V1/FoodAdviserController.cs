using Defender.Common.Attributes;
using Defender.Common.Consts;
using Defender.Portal.Application.Common.Interfaces.Wrappers;
using Defender.Portal.Application.DTOs.FoodAdviser;
using Defender.Portal.Application.Models.ApiRequests.FoodAdviser;
using Defender.Portal.Application.Modules.FoodAdviser.Commands;
using Defender.Portal.Application.Modules.FoodAdviser.Queries;
using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Defender.Portal.WebUI.Controllers.V1;

public class FoodAdviserController(
    IMediator mediator,
    IMapper mapper,
    IPersonalFoodAdviserWrapper foodAdviserWrapper) : BaseApiController(mediator, mapper)
{
    [HttpGet("preferences")]
    [Auth(Roles.User)]
    [ProducesResponseType(typeof(PortalPreferencesDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> GetPreferences(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetPreferencesQuery(), cancellationToken);
        if (result == null) return NoContent();
        return Ok(result);
    }

    [HttpPut("preferences")]
    [Auth(Roles.User)]
    [ProducesResponseType(typeof(PortalPreferencesDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> UpdatePreferences([FromBody] UpdatePreferencesCommand command, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpPost("session")]
    [Auth(Roles.User)]
    [ProducesResponseType(typeof(PortalMenuSessionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> CreateSession(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new CreateSessionCommand(), cancellationToken);
        return CreatedAtAction(nameof(GetSession), new { sessionId = result.Id }, result);
    }

    [HttpGet("session/{sessionId:guid}")]
    [Auth(Roles.User)]
    [ProducesResponseType(typeof(PortalMenuSessionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> GetSession(Guid sessionId, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetSessionQuery(sessionId), cancellationToken);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpPost("session/{sessionId:guid}/upload")]
    [Auth(Roles.User)]
    [ProducesResponseType(typeof(IReadOnlyList<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> UploadSessionImages(Guid sessionId, [FromForm] IFormFileCollection? files, CancellationToken cancellationToken)
    {
        if (files == null || files.Count == 0)
            return BadRequest("At least one image is required.");
        if (files.Count > 10)
            return BadRequest("Maximum 10 images allowed.");
        var streams = new List<Stream>();
        var contentTypes = new List<string>();
        try
        {
            foreach (var file in files)
            {
                streams.Add(file.OpenReadStream());
                contentTypes.Add(file.ContentType ?? "image/jpeg");
            }
            var refs = await foodAdviserWrapper.UploadSessionImagesAsync(sessionId, streams.ToArray(), contentTypes.ToArray(), cancellationToken);
            return Ok(refs);
        }
        finally
        {
            foreach (var s in streams)
                await s.DisposeAsync();
        }
    }

    [HttpPatch("session/{sessionId:guid}/confirm")]
    [Auth(Roles.User)]
    [ProducesResponseType(typeof(PortalMenuSessionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> ConfirmMenu(Guid sessionId, [FromBody] ConfirmMenuRequest body, CancellationToken cancellationToken)
    {
        var command = new ConfirmMenuCommand(sessionId, body?.ConfirmedItems ?? [], body?.TrySomethingNew ?? false);
        var result = await mediator.Send(command, cancellationToken);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpPost("session/{sessionId:guid}/request-parsing")]
    [Auth(Roles.User)]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> RequestParsing(Guid sessionId, CancellationToken cancellationToken)
    {
        await mediator.Send(new RequestParsingCommand(sessionId), cancellationToken);
        return Accepted();
    }

    [HttpPost("session/{sessionId:guid}/request-recommendations")]
    [Auth(Roles.User)]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> RequestRecommendations(Guid sessionId, CancellationToken cancellationToken)
    {
        await mediator.Send(new RequestRecommendationsCommand(sessionId), cancellationToken);
        return Accepted();
    }

    [HttpGet("session/{sessionId:guid}/recommendations")]
    [Auth(Roles.User)]
    [ProducesResponseType(typeof(IReadOnlyList<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> GetRecommendations(Guid sessionId, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetRecommendationsQuery(sessionId), cancellationToken);
        if (result == null || result.Count == 0) return NoContent();
        return Ok(result);
    }

    [HttpPost("rating")]
    [Auth(Roles.User)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> SubmitRating([FromBody] SubmitRatingCommand command, CancellationToken cancellationToken)
    {
        await mediator.Send(command, cancellationToken);
        return NoContent();
    }
}
