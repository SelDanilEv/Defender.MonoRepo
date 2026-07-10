using Defender.TravelCalendarService.Application.DTOs;
using Defender.TravelCalendarService.Application.Models.Requests;

namespace Defender.TravelCalendarService.Application.Common.Interfaces.Services;

public interface ITravelCalendarService
{
    Task<TravelCalendarDto> GetAsync(Guid userId, CancellationToken cancellationToken);
    Task<TravelCalendarMutationResultDto> SetThemeAsync(Guid userId, SetThemeRequest request, CancellationToken cancellationToken);
    Task<TravelCalendarMutationResultDto> AddQueuedTripAsync(Guid userId, CreateQueuedTripRequest request, CancellationToken cancellationToken);
    Task<TravelCalendarMutationResultDto> CreateEventFromDateAsync(Guid userId, CreateEventFromDateRequest request, CancellationToken cancellationToken);
    Task<TravelCalendarMutationResultDto> UpdateEventAsync(Guid userId, Guid eventId, UpdateTravelEventRequest request, CancellationToken cancellationToken);
    Task<TravelCalendarMutationResultDto> RemoveEventAsync(Guid userId, Guid eventId, VersionedRequest request, CancellationToken cancellationToken);
    Task<TravelCalendarMutationResultDto> AutoScheduleAsync(Guid userId, Guid eventId, VersionedRequest request, CancellationToken cancellationToken);
    Task<TravelCalendarMutationResultDto> AddPointAsync(Guid userId, Guid eventId, AddPointRequest request, CancellationToken cancellationToken);
    Task<TravelCalendarMutationResultDto> UpdatePointAsync(Guid userId, Guid eventId, Guid pointId, UpdatePointRequest request, CancellationToken cancellationToken);
    Task<TravelCalendarMutationResultDto> RemovePointAsync(Guid userId, Guid eventId, Guid pointId, VersionedRequest request, CancellationToken cancellationToken);
    Task<TravelCalendarMutationResultDto> AddParticipantAsync(Guid userId, Guid eventId, AddParticipantRequest request, CancellationToken cancellationToken);
    Task<TravelCalendarMutationResultDto> RemoveParticipantAsync(Guid userId, Guid eventId, Guid participantUserId, VersionedRequest request, CancellationToken cancellationToken);
    Task<TravelCalendarMutationResultDto> UpdateMyParticipationAsync(Guid userId, Guid eventId, UpdateMyParticipationRequest request, CancellationToken cancellationToken);
    Task<TravelCalendarMutationResultDto> AddPackingItemAsync(Guid userId, AddPackingItemRequest request, CancellationToken cancellationToken);
    Task<TravelCalendarMutationResultDto> UpdatePackingItemAsync(Guid userId, Guid itemId, UpdatePackingItemRequest request, CancellationToken cancellationToken);
    Task<TravelCalendarMutationResultDto> RemovePackingItemAsync(Guid userId, Guid itemId, VersionedRequest request, CancellationToken cancellationToken);
}
