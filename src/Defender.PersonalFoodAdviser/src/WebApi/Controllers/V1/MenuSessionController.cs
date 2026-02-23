using Defender.Common.Attributes;
using Defender.Common.Consts;
using Defender.PersonalFoodAdviser.Application.Common.Interfaces.Services;
using Defender.PersonalFoodAdviser.Application.DTOs;
using Defender.PersonalFoodAdviser.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Roles = Defender.Common.Consts.Roles;

namespace WebApi.Controllers.V1;

[Route("api/V1/[controller]")]
[ApiController]
public class MenuSessionController(
    IMenuSessionService menuSessionService,
    Defender.PersonalFoodAdviser.Application.Common.Interfaces.Services.IImageUploadService imageUploadService,
    Defender.Common.Interfaces.ICurrentAccountAccessor currentAccountAccessor) : ControllerBase
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
        var session = await menuSessionService.CreateAsync(userId, cancellationToken);
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
        var session = await menuSessionService.GetByIdAsync(sessionId, userId, cancellationToken);
        if (session == null)
            return NotFound();
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
            return BadRequest("At least one image is required.");
        if (files.Count > MaxImageCount)
            return BadRequest($"Maximum {MaxImageCount} images allowed.");
        if (files.Any(file => file.Length <= 0))
            return BadRequest("Uploaded file is empty.");
        if (files.Any(file => file.Length > MaxImageSizeBytes))
            return BadRequest($"Each image must be {MaxImageSizeBytes / (1024 * 1024)} MB or less.");
        if (files.Any(file => !IsImageFile(file.ContentType)))
            return BadRequest("Only image files are allowed.");

        var userId = currentAccountAccessor.GetAccountId();
        var session = await menuSessionService.GetByIdAsync(sessionId, userId, cancellationToken);
        if (session == null)
            return NotFound();
        var list = new List<(Stream Stream, string ContentType)>();
        foreach (var file in files)
        {
            var stream = file.OpenReadStream();
            list.Add((stream, file.ContentType ?? "image/jpeg"));
        }
        try
        {
            var refs = await imageUploadService.UploadAsync(sessionId, list, cancellationToken);
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
        var session = await menuSessionService.UpdateImageRefsAsync(sessionId, userId, imageRefs ?? [], cancellationToken);
        if (session == null)
            return NotFound();
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
        var session = await menuSessionService.ConfirmAsync(
            sessionId,
            userId,
            request?.ConfirmedItems ?? [],
            request?.TrySomethingNew ?? false,
            cancellationToken);
        if (session == null)
            return NotFound();
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
        var session = await menuSessionService.GetByIdAsync(sessionId, userId, cancellationToken);
        if (session == null)
            return NotFound();
        await menuSessionService.RequestParsingAsync(sessionId, userId, cancellationToken);
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
        var session = await menuSessionService.GetByIdAsync(sessionId, userId, cancellationToken);
        if (session == null)
            return NotFound();
        await menuSessionService.RequestRecommendationsAsync(sessionId, userId, cancellationToken);
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
        var items = await menuSessionService.GetRecommendationsAsync(sessionId, userId, cancellationToken);
        if (items == null || items.Count == 0)
            return NoContent();
        return Ok(items);
    }

    private static bool IsImageFile(string? contentType)
        => !string.IsNullOrWhiteSpace(contentType)
           && contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
}
