using Defender.Common.Attributes;
using Defender.Common.Consts;
using Defender.Common.Interfaces;
using Defender.TravelCalendarService.Application.Common.Interfaces.Services;
using Defender.TravelCalendarService.Application.DTOs;
using Defender.TravelCalendarService.Application.Models.Requests;
using Defender.TravelCalendarService.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Defender.TravelCalendarService.WebApi.Controllers.V1;

[ApiController]
[Route("api/V1/travel-calendar")]
[Auth(Roles.User)]
public class TravelCalendarController(ICurrentAccountAccessor account, ITravelCalendarService service) : ControllerBase
{
    private Guid UserId => account.GetAccountId();
    [HttpGet]
    public Task<TravelCalendarDto> Get([FromQuery] string? from, [FromQuery] string? to, CancellationToken ct)
        => service.GetAsync(UserId, ParseDate(from), ParseDate(to), ct);

    private static DateOnly? ParseDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (DateOnly.TryParseExact(value, "yyyy-MM-dd", out var date)) return date;
        throw new TravelCalendarValidationException("TRAVEL_CALENDAR_INVALID_DATE", "Dates must use the yyyy-MM-dd format.");
    }
    [HttpPatch("theme")] public Task<TravelCalendarMutationResultDto> SetTheme(SetThemeRequest request, CancellationToken ct) => service.SetThemeAsync(UserId, request, ct);
    [HttpPost("queued-trips")] public async Task<ActionResult<TravelCalendarMutationResultDto>> AddQueuedTrip(CreateQueuedTripRequest request, CancellationToken ct) => StatusCode(201, await service.AddQueuedTripAsync(UserId, request, ct));
    [HttpPost("events/from-date")] public async Task<ActionResult<TravelCalendarMutationResultDto>> CreateFromDate(CreateEventFromDateRequest request, CancellationToken ct) => StatusCode(201, await service.CreateEventFromDateAsync(UserId, request, ct));
    [HttpPut("events/{eventId:guid}")] public Task<TravelCalendarMutationResultDto> UpdateEvent(Guid eventId, UpdateTravelEventRequest request, CancellationToken ct) => service.UpdateEventAsync(UserId, eventId, request, ct);
    [HttpDelete("events/{eventId:guid}")] public Task<TravelCalendarMutationResultDto> RemoveEvent(Guid eventId, VersionedRequest request, CancellationToken ct) => service.RemoveEventAsync(UserId, eventId, request, ct);
    [HttpPost("events/{eventId:guid}/auto-schedule")] public Task<TravelCalendarMutationResultDto> AutoSchedule(Guid eventId, VersionedRequest request, CancellationToken ct) => service.AutoScheduleAsync(UserId, eventId, request, ct);
    [HttpPost("events/{eventId:guid}/points")] public async Task<ActionResult<TravelCalendarMutationResultDto>> AddPoint(Guid eventId, AddPointRequest request, CancellationToken ct) => StatusCode(201, await service.AddPointAsync(UserId, eventId, request, ct));
    [HttpPatch("events/{eventId:guid}/points/{pointId:guid}")] public Task<TravelCalendarMutationResultDto> UpdatePoint(Guid eventId, Guid pointId, UpdatePointRequest request, CancellationToken ct) => service.UpdatePointAsync(UserId, eventId, pointId, request, ct);
    [HttpDelete("events/{eventId:guid}/points/{pointId:guid}")] public Task<TravelCalendarMutationResultDto> RemovePoint(Guid eventId, Guid pointId, VersionedRequest request, CancellationToken ct) => service.RemovePointAsync(UserId, eventId, pointId, request, ct);
    [HttpPost("events/{eventId:guid}/participants")] public async Task<ActionResult<TravelCalendarMutationResultDto>> AddParticipant(Guid eventId, AddParticipantRequest request, CancellationToken ct) => StatusCode(201, await service.AddParticipantAsync(UserId, eventId, request, ct));
    [HttpDelete("events/{eventId:guid}/participants/{participantUserId:guid}")] public Task<TravelCalendarMutationResultDto> RemoveParticipant(Guid eventId, Guid participantUserId, VersionedRequest request, CancellationToken ct) => service.RemoveParticipantAsync(UserId, eventId, participantUserId, request, ct);
    [HttpPatch("events/{eventId:guid}/my-participation")] public Task<TravelCalendarMutationResultDto> UpdateMyParticipation(Guid eventId, UpdateMyParticipationRequest request, CancellationToken ct) => service.UpdateMyParticipationAsync(UserId, eventId, request, ct);
    [HttpPost("packing-items")] public async Task<ActionResult<TravelCalendarMutationResultDto>> AddPacking(AddPackingItemRequest request, CancellationToken ct) => StatusCode(201, await service.AddPackingItemAsync(UserId, request, ct));
    [HttpPatch("packing-items/{itemId:guid}")] public Task<TravelCalendarMutationResultDto> UpdatePacking(Guid itemId, UpdatePackingItemRequest request, CancellationToken ct) => service.UpdatePackingItemAsync(UserId, itemId, request, ct);
    [HttpDelete("packing-items/{itemId:guid}")] public Task<TravelCalendarMutationResultDto> RemovePacking(Guid itemId, VersionedRequest request, CancellationToken ct) => service.RemovePackingItemAsync(UserId, itemId, request, ct);
}
