using Defender.Common.Attributes;
using Defender.Common.Consts;
using Defender.PersonalFoodAdviser.Application.DTOs;
using Defender.PersonalFoodAdviser.Application.Modules.MenuSessions.Commands;
using Defender.PersonalFoodAdviser.Application.Modules.MenuSessions.Queries;
using Defender.PersonalFoodAdviser.Domain.Entities;
using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Roles = Defender.Common.Consts.Roles;

namespace WebApi.Controllers.V1;

[Route("api/V1/[controller]")]
[ApiController]
public class MenuSessionController(
    IMediator mediator,
    IMapper mapper,
    Defender.Common.Interfaces.ICurrentAccountAccessor currentAccountAccessor,
    ILogger<MenuSessionController> logger) : BaseApiController(mediator, mapper)
{
    private const int MaxImageCount = 10;
    private const long MaxImageSizeBytes = 10 * 1024 * 1024;

    [HttpPost]
    [Auth(Roles.User)]
    [ProducesResponseType(typeof(MenuSession), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<MenuSession>> Create(CancellationToken cancellationToken)
    {
        var userId = currentAccountAccessor.GetAccountId();
        logger.LogInformation("Create menu session requested by user {UserId}", userId);

        var command = new CreateMenuSessionCommand
        {
            UserId = userId
        };

        var session = await ProcessApiCallWithoutMappingAsync<CreateMenuSessionCommand, MenuSession>(command);

        logger.LogInformation("Menu session {SessionId} created for user {UserId}", session.Id, userId);
        return CreatedAtAction(nameof(Get), new { sessionId = session.Id }, session);
    }

    [HttpGet]
    [Auth(Roles.User)]
    [ProducesResponseType(typeof(IReadOnlyList<MenuSession>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IReadOnlyList<MenuSession>>> GetAll(CancellationToken cancellationToken)
    {
        var userId = currentAccountAccessor.GetAccountId();
        logger.LogInformation("Get all menu sessions requested by user {UserId}", userId);

        var query = new GetMenuSessionsByUserQuery
        {
            UserId = userId
        };

        var sessions = await ProcessApiCallWithoutMappingAsync<GetMenuSessionsByUserQuery, IReadOnlyList<MenuSession>>(query);

        logger.LogInformation("Returning {Count} menu sessions for user {UserId}", sessions.Count, userId);
        return Ok(sessions);
    }

    [HttpGet("{sessionId:guid}")]
    [Auth(Roles.User)]
    [ProducesResponseType(typeof(MenuSession), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<MenuSession>> Get(Guid sessionId, CancellationToken cancellationToken)
    {
        var userId = currentAccountAccessor.GetAccountId();
        logger.LogInformation("Get menu session requested: session {SessionId}, user {UserId}", sessionId, userId);

        var query = new GetMenuSessionByIdQuery
        {
            SessionId = sessionId,
            UserId = userId
        };

        var session = await ProcessApiCallWithoutMappingAsync<GetMenuSessionByIdQuery, MenuSession?>(query);
        if (session == null)
        {
            logger.LogWarning("Menu session not found or not owned: session {SessionId}, user {UserId}", sessionId, userId);
            return NotFound();
        }
        logger.LogInformation("Menu session loaded: session {SessionId}, status {Status}", sessionId, session.Status);
        return Ok(session);
    }

    [HttpDelete("{sessionId:guid}")]
    [Auth(Roles.User)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> Delete(Guid sessionId, CancellationToken cancellationToken)
    {
        var userId = currentAccountAccessor.GetAccountId();
        logger.LogInformation("Delete menu session requested: session {SessionId}, user {UserId}", sessionId, userId);

        var command = new DeleteMenuSessionCommand
        {
            SessionId = sessionId,
            UserId = userId
        };

        var deleted = await ProcessApiCallWithoutMappingAsync<DeleteMenuSessionCommand, bool>(command);

        if (!deleted)
        {
            logger.LogWarning("Delete menu session failed: session {SessionId} not found or not owned by user {UserId}", sessionId, userId);
            return NotFound();
        }

        logger.LogInformation("Delete menu session completed: session {SessionId}, user {UserId}", sessionId, userId);
        return NoContent();
    }

    [HttpPost("{sessionId:guid}/upload")]
    [Auth(Roles.User)]
    [ProducesResponseType(typeof(IReadOnlyList<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IReadOnlyList<string>>> Upload(Guid sessionId, [FromForm] IFormFileCollection files, CancellationToken cancellationToken)
    {
        if (files == null || files.Count == 0)
        {
            logger.LogWarning("Upload rejected for session {SessionId}: no files provided", sessionId);
            return BadRequest("At least one image is required.");
        }
        if (files.Count > MaxImageCount)
        {
            logger.LogWarning("Upload rejected for session {SessionId}: file count {Count} exceeds limit {Limit}", sessionId, files.Count, MaxImageCount);
            return BadRequest($"Maximum {MaxImageCount} images allowed.");
        }
        if (files.Any(file => file.Length <= 0))
        {
            logger.LogWarning("Upload rejected for session {SessionId}: one or more files are empty", sessionId);
            return BadRequest("Uploaded file is empty.");
        }
        if (files.Any(file => file.Length > MaxImageSizeBytes))
        {
            logger.LogWarning("Upload rejected for session {SessionId}: one or more files exceed max size {MaxBytes}", sessionId, MaxImageSizeBytes);
            return BadRequest($"Each image must be {MaxImageSizeBytes / (1024 * 1024)} MB or less.");
        }
        if (files.Any(file => !IsImageFile(file.ContentType)))
        {
            logger.LogWarning("Upload rejected for session {SessionId}: one or more files are not image content types", sessionId);
            return BadRequest("Only image files are allowed.");
        }

        var userId = currentAccountAccessor.GetAccountId();
        logger.LogInformation(
            "Upload requested: session {SessionId}, user {UserId}, fileCount {FileCount}, totalBytes {TotalBytes}",
            sessionId,
            userId,
            files.Count,
            files.Sum(file => file.Length));

        var list = new List<(Stream Stream, string ContentType)>();
        foreach (var file in files)
        {
            var stream = file.OpenReadStream();
            list.Add((stream, file.ContentType ?? "image/jpeg"));
        }

        var command = new UploadMenuSessionImagesCommand
        {
            SessionId = sessionId,
            UserId = userId,
            Files = list
        };

        try
        {
            var refs = await ProcessApiCallWithoutMappingAsync<UploadMenuSessionImagesCommand, IReadOnlyList<string>>(command);
            if (refs.Count == 0)
            {
                logger.LogWarning("Upload rejected: session {SessionId} not found or not owned by user {UserId}", sessionId, userId);
                return NotFound();
            }

            logger.LogInformation("Upload completed: session {SessionId}, user {UserId}, uploadedRefsCount {RefsCount}", sessionId, userId, refs.Count);
            return Ok(refs);
        }
        finally
        {
            foreach (var (stream, _) in list)
                await stream.DisposeAsync();
        }
    }

    [HttpPatch("{sessionId:guid}/images")]
    [Auth(Roles.User)]
    [ProducesResponseType(typeof(MenuSession), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<MenuSession>> UpdateImageRefs(Guid sessionId, [FromBody] IReadOnlyList<string> imageRefs, CancellationToken cancellationToken)
    {
        var userId = currentAccountAccessor.GetAccountId();
        logger.LogInformation("Update image refs requested: session {SessionId}, user {UserId}, refCount {RefCount}", sessionId, userId, imageRefs?.Count ?? 0);

        var command = new UpdateMenuSessionImageRefsCommand
        {
            SessionId = sessionId,
            UserId = userId,
            ImageRefs = imageRefs ?? []
        };

        var session = await ProcessApiCallWithoutMappingAsync<UpdateMenuSessionImageRefsCommand, MenuSession?>(command);

        if (session == null)
        {
            logger.LogWarning("Update image refs failed: session {SessionId} not found or not owned by user {UserId}", sessionId, userId);
            return NotFound();
        }
        logger.LogInformation("Update image refs completed: session {SessionId}, newRefCount {RefCount}", sessionId, session.ImageRefs.Count);
        return Ok(session);
    }

    [HttpPatch("{sessionId:guid}/confirm")]
    [Auth(Roles.User)]
    [ProducesResponseType(typeof(MenuSession), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<MenuSession>> Confirm(Guid sessionId, [FromBody] ConfirmMenuRequest request, CancellationToken cancellationToken)
    {
        var userId = currentAccountAccessor.GetAccountId();
        logger.LogInformation(
            "Confirm menu requested: session {SessionId}, user {UserId}, confirmedCount {ConfirmedCount}, trySomethingNew {TrySomethingNew}",
            sessionId,
            userId,
            request?.ConfirmedItems?.Count ?? 0,
            request?.TrySomethingNew ?? false);

        var command = new ConfirmMenuSessionCommand
        {
            SessionId = sessionId,
            UserId = userId,
            ConfirmedItems = request?.ConfirmedItems ?? [],
            TrySomethingNew = request?.TrySomethingNew ?? false
        };

        var session = await ProcessApiCallWithoutMappingAsync<ConfirmMenuSessionCommand, MenuSession?>(command);

        if (session == null)
        {
            logger.LogWarning("Confirm menu failed: session {SessionId} not found or not owned by user {UserId}", sessionId, userId);
            return NotFound();
        }
        logger.LogInformation("Confirm menu completed: session {SessionId}, status {Status}, confirmedCount {ConfirmedCount}", sessionId, session.Status, session.ConfirmedItems.Count);
        return Ok(session);
    }

    [HttpPost("{sessionId:guid}/request-parsing")]
    [Auth(Roles.User)]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> RequestParsing(Guid sessionId, CancellationToken cancellationToken)
    {
        var userId = currentAccountAccessor.GetAccountId();
        logger.LogInformation("Request parsing received: session {SessionId}, user {UserId}", sessionId, userId);

        var command = new RequestMenuSessionParsingCommand
        {
            SessionId = sessionId,
            UserId = userId
        };

        var parsed = await ProcessApiCallWithoutMappingAsync<RequestMenuSessionParsingCommand, bool>(command);
        if (!parsed)
        {
            logger.LogWarning("Request parsing rejected: session {SessionId} not found or not owned by user {UserId}", sessionId, userId);
            return NotFound();
        }

        logger.LogInformation("Request parsing queued: session {SessionId}", sessionId);
        return Accepted();
    }

    [HttpPost("{sessionId:guid}/request-recommendations")]
    [Auth(Roles.User)]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> RequestRecommendations(Guid sessionId, CancellationToken cancellationToken)
    {
        var userId = currentAccountAccessor.GetAccountId();
        logger.LogInformation("Request recommendations received: session {SessionId}, user {UserId}", sessionId, userId);

        var command = new RequestMenuSessionRecommendationsCommand
        {
            SessionId = sessionId,
            UserId = userId
        };

        var requested = await ProcessApiCallWithoutMappingAsync<RequestMenuSessionRecommendationsCommand, bool>(command);
        if (!requested)
        {
            logger.LogWarning("Request recommendations rejected: session {SessionId} not found or not owned by user {UserId}", sessionId, userId);
            return NotFound();
        }

        logger.LogInformation("Request recommendations queued: session {SessionId}", sessionId);
        return Accepted();
    }

    [HttpGet("{sessionId:guid}/recommendations")]
    [Auth(Roles.User)]
    [ProducesResponseType(typeof(IReadOnlyList<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IReadOnlyList<string>>> GetRecommendations(Guid sessionId, CancellationToken cancellationToken)
    {
        var userId = currentAccountAccessor.GetAccountId();
        logger.LogInformation("Get recommendations requested: session {SessionId}, user {UserId}", sessionId, userId);

        var query = new GetMenuSessionRecommendationsQuery
        {
            SessionId = sessionId,
            UserId = userId
        };

        var recommendations = await ProcessApiCallWithoutMappingAsync<GetMenuSessionRecommendationsQuery, IReadOnlyList<string>?>(query);
        if (recommendations == null)
        {
            logger.LogWarning("Get recommendations failed: session {SessionId} not found or not owned by user {UserId}", sessionId, userId);
            return NotFound();
        }

        if (recommendations.Count == 0)
        {
            logger.LogInformation("No recommendations available: session {SessionId}, user {UserId}", sessionId, userId);
            return NoContent();
        }

        logger.LogInformation("Recommendations returned: session {SessionId}, user {UserId}, count {Count}", sessionId, userId, recommendations.Count);
        return Ok(recommendations);
    }

    private static bool IsImageFile(string? contentType)
        => !string.IsNullOrWhiteSpace(contentType)
           && contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
}
