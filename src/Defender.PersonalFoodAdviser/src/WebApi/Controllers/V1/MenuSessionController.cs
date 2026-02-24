using Defender.Common.Attributes;
using Defender.Common.Consts;
using Defender.PersonalFoodAdviser.Application.Common.Interfaces.Services;
using Defender.PersonalFoodAdviser.Application.DTOs;
using Defender.PersonalFoodAdviser.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Roles = Defender.Common.Consts.Roles;

namespace WebApi.Controllers.V1;

[Route("api/V1/[controller]")]
[ApiController]
public class MenuSessionController(
    IMenuSessionService menuSessionService,
    Defender.PersonalFoodAdviser.Application.Common.Interfaces.Services.IImageUploadService imageUploadService,
    Defender.Common.Interfaces.ICurrentAccountAccessor currentAccountAccessor,
    ILogger<MenuSessionController> logger) : ControllerBase
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
        var session = await menuSessionService.CreateAsync(userId, cancellationToken);
        logger.LogInformation("Menu session {SessionId} created for user {UserId}", session.Id, userId);
        return CreatedAtAction(nameof(Get), new { sessionId = session.Id }, session);
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
        var session = await menuSessionService.GetByIdAsync(sessionId, userId, cancellationToken);
        if (session == null)
        {
            logger.LogWarning("Menu session not found or not owned: session {SessionId}, user {UserId}", sessionId, userId);
            return NotFound();
        }
        logger.LogInformation("Menu session loaded: session {SessionId}, status {Status}", sessionId, session.Status);
        return Ok(session);
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

        var session = await menuSessionService.GetByIdAsync(sessionId, userId, cancellationToken);
        if (session == null)
        {
            logger.LogWarning("Upload rejected: session {SessionId} not found or not owned by user {UserId}", sessionId, userId);
            return NotFound();
        }
        var list = new List<(Stream Stream, string ContentType)>();
        foreach (var file in files)
        {
            var stream = file.OpenReadStream();
            list.Add((stream, file.ContentType ?? "image/jpeg"));
        }
        try
        {
            var refs = await imageUploadService.UploadAsync(sessionId, list, cancellationToken);
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
        var session = await menuSessionService.UpdateImageRefsAsync(sessionId, userId, imageRefs ?? [], cancellationToken);
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

        var session = await menuSessionService.ConfirmAsync(
            sessionId,
            userId,
            request?.ConfirmedItems ?? [],
            request?.TrySomethingNew ?? false,
            cancellationToken);
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
        var session = await menuSessionService.GetByIdAsync(sessionId, userId, cancellationToken);
        if (session == null)
        {
            logger.LogWarning("Request parsing rejected: session {SessionId} not found or not owned by user {UserId}", sessionId, userId);
            return NotFound();
        }
        await menuSessionService.RequestParsingAsync(sessionId, userId, cancellationToken);
        logger.LogInformation("Request parsing queued: session {SessionId}, imageRefsCount {ImageRefsCount}", sessionId, session.ImageRefs.Count);
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
        var session = await menuSessionService.GetByIdAsync(sessionId, userId, cancellationToken);
        if (session == null)
        {
            logger.LogWarning("Request recommendations rejected: session {SessionId} not found or not owned by user {UserId}", sessionId, userId);
            return NotFound();
        }
        await menuSessionService.RequestRecommendationsAsync(sessionId, userId, cancellationToken);
        logger.LogInformation("Request recommendations queued: session {SessionId}, confirmedItemsCount {ConfirmedCount}", sessionId, session.ConfirmedItems.Count);
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
        var items = await menuSessionService.GetRecommendationsAsync(sessionId, userId, cancellationToken);
        if (items == null || items.Count == 0)
        {
            logger.LogInformation("No recommendations available: session {SessionId}, user {UserId}", sessionId, userId);
            return NoContent();
        }
        logger.LogInformation("Recommendations returned: session {SessionId}, user {UserId}, count {Count}", sessionId, userId, items.Count);
        return Ok(items);
    }

    private static bool IsImageFile(string? contentType)
        => !string.IsNullOrWhiteSpace(contentType)
           && contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
}
