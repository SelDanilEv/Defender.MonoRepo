using AutoMapper;
using Defender.Common.Attributes;
using Defender.Common.Consts;
using Defender.Common.DB.Pagination;
using Defender.Portal.Application.Common.Interfaces.Wrappers;
using Defender.Portal.Application.DTOs.TravelCalendar;
using Defender.Portal.Application.Models.ApiRequests.TravelCalendar;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Defender.Portal.WebUI.Controllers.V1;

public class TravelCalendarController(IMediator mediator, IMapper mapper, ITravelCalendarWrapper wrapper, IUserManagementWrapper userManagementWrapper) : BaseApiController(mediator, mapper)
{
    [HttpGet, Auth(Roles.User)] public async Task<IActionResult> Get(CancellationToken ct) => Ok(await wrapper.GetAsync(ct));
    [HttpGet("users"), Auth(Roles.User)] public async Task<IActionResult> SearchUsers([FromQuery] string query, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Ok(Array.Empty<TravelCalendarUserOptionDto>());
        }

        var trimmed = query.Trim();
        var users = await userManagementWrapper.GetUsersInfoAsync(new PaginationRequest { Page = 0, PageSize = 50 });
        var result = users.Items
            .Where(item =>
                (!string.IsNullOrWhiteSpace(item.Nickname) && item.Nickname.Contains(trimmed, StringComparison.OrdinalIgnoreCase))
                || (!string.IsNullOrWhiteSpace(item.Email) && item.Email.Contains(trimmed, StringComparison.OrdinalIgnoreCase)))
            .Take(10)
            .Select(item => new TravelCalendarUserOptionDto(item.Id, item.Nickname ?? item.Email ?? item.Id.ToString(), item.Email ?? string.Empty, null))
            .ToArray();

        return Ok(result);
    }
    [HttpPatch("theme"), Auth(Roles.User)] public async Task<IActionResult> Theme([FromBody] SetThemeRequest request, CancellationToken ct) => Ok(await wrapper.SendAsync(HttpMethod.Patch, "/theme", request, ct));
    [HttpPost("queued-trips"), Auth(Roles.User)] public async Task<IActionResult> Queue([FromBody] CreateQueuedTripRequest request, CancellationToken ct) => StatusCode(201, await wrapper.SendAsync(HttpMethod.Post, "/queued-trips", request, ct));
    [HttpPost("events/from-date"), Auth(Roles.User)] public async Task<IActionResult> CreateFromDate([FromBody] CreateEventFromDateRequest request, CancellationToken ct) => StatusCode(201, await wrapper.SendAsync(HttpMethod.Post, "/events/from-date", request, ct));
    [HttpPut("events/{eventId:guid}"), Auth(Roles.User)] public async Task<IActionResult> UpdateEvent(Guid eventId, [FromBody] UpdateTravelEventRequest request, CancellationToken ct) => Ok(await wrapper.SendAsync(HttpMethod.Put, $"/events/{eventId}", request, ct));
    [HttpDelete("events/{eventId:guid}"), Auth(Roles.User)] public async Task<IActionResult> DeleteEvent(Guid eventId, [FromBody] VersionedRequest request, CancellationToken ct) => Ok(await wrapper.SendAsync(HttpMethod.Delete, $"/events/{eventId}", request, ct));
    [HttpPost("events/{eventId:guid}/auto-schedule"), Auth(Roles.User)] public async Task<IActionResult> AutoSchedule(Guid eventId, [FromBody] VersionedRequest request, CancellationToken ct) => Ok(await wrapper.SendAsync(HttpMethod.Post, $"/events/{eventId}/auto-schedule", request, ct));
    [HttpPost("events/{eventId:guid}/points"), Auth(Roles.User)] public async Task<IActionResult> AddPoint(Guid eventId, [FromBody] AddPointRequest request, CancellationToken ct) => StatusCode(201, await wrapper.SendAsync(HttpMethod.Post, $"/events/{eventId}/points", request, ct));
    [HttpPatch("events/{eventId:guid}/points/{pointId:guid}"), Auth(Roles.User)] public async Task<IActionResult> UpdatePoint(Guid eventId, Guid pointId, [FromBody] UpdatePointRequest request, CancellationToken ct) => Ok(await wrapper.SendAsync(HttpMethod.Patch, $"/events/{eventId}/points/{pointId}", request, ct));
    [HttpDelete("events/{eventId:guid}/points/{pointId:guid}"), Auth(Roles.User)] public async Task<IActionResult> DeletePoint(Guid eventId, Guid pointId, [FromBody] VersionedRequest request, CancellationToken ct) => Ok(await wrapper.SendAsync(HttpMethod.Delete, $"/events/{eventId}/points/{pointId}", request, ct));
    [HttpPost("events/{eventId:guid}/participants"), Auth(Roles.User)] public async Task<IActionResult> AddParticipant(Guid eventId, [FromBody] AddParticipantRequest request, CancellationToken ct) => StatusCode(201, await wrapper.SendAsync(HttpMethod.Post, $"/events/{eventId}/participants", request, ct));
    [HttpDelete("events/{eventId:guid}/participants/{participantUserId:guid}"), Auth(Roles.User)] public async Task<IActionResult> DeleteParticipant(Guid eventId, Guid participantUserId, [FromBody] VersionedRequest request, CancellationToken ct) => Ok(await wrapper.SendAsync(HttpMethod.Delete, $"/events/{eventId}/participants/{participantUserId}", request, ct));
    [HttpPatch("events/{eventId:guid}/my-participation"), Auth(Roles.User)] public async Task<IActionResult> UpdateMyParticipation(Guid eventId, [FromBody] UpdateMyParticipationRequest request, CancellationToken ct) => Ok(await wrapper.SendAsync(HttpMethod.Patch, $"/events/{eventId}/my-participation", request, ct));
    [HttpPost("packing-items"), Auth(Roles.User)] public async Task<IActionResult> AddPacking([FromBody] AddPackingItemRequest request, CancellationToken ct) => StatusCode(201, await wrapper.SendAsync(HttpMethod.Post, "/packing-items", request, ct));
    [HttpPatch("packing-items/{itemId:guid}"), Auth(Roles.User)] public async Task<IActionResult> UpdatePacking(Guid itemId, [FromBody] UpdatePackingItemRequest request, CancellationToken ct) => Ok(await wrapper.SendAsync(HttpMethod.Patch, $"/packing-items/{itemId}", request, ct));
    [HttpDelete("packing-items/{itemId:guid}"), Auth(Roles.User)] public async Task<IActionResult> DeletePacking(Guid itemId, [FromBody] VersionedRequest request, CancellationToken ct) => Ok(await wrapper.SendAsync(HttpMethod.Delete, $"/packing-items/{itemId}", request, ct));
}
