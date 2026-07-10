using Defender.TravelCalendarService.Domain.Entities;
using Defender.TravelCalendarService.Domain.Exceptions;
using Defender.TravelCalendarService.Domain.ValueObjects;

namespace Defender.TravelCalendarService.Tests.Domain;

public class TravelEventTests
{
    [Fact]
    public void AddParticipant_WhenOwnerInvitesUser_AddsPendingParticipant()
    {
        var ownerId = Guid.NewGuid();
        var participantId = Guid.NewGuid();
        var travelEvent = TravelEvent.Scheduled(ownerId, "Trip", TravelEventType.OvernightTrip, new DateOnly(2026, 7, 4), new DateOnly(2026, 7, 5));

        travelEvent.AddParticipant(ownerId, participantId, "Bob", "avatar.png", DateTimeOffset.UtcNow);

        var participant = Assert.Single(travelEvent.Participants);
        Assert.Equal(participantId, participant.UserId);
        Assert.Equal(TravelParticipantStatus.Pending, participant.Status);
    }

    [Fact]
    public void AddParticipant_WhenParticipantAlreadyExists_ThrowsConflict()
    {
        var ownerId = Guid.NewGuid();
        var participantId = Guid.NewGuid();
        var travelEvent = TravelEvent.Scheduled(ownerId, "Trip", TravelEventType.OvernightTrip, new DateOnly(2026, 7, 4), new DateOnly(2026, 7, 5));
        travelEvent.AddParticipant(ownerId, participantId, "Bob", null, DateTimeOffset.UtcNow);

        Assert.Throws<TravelCalendarConflictException>(() => travelEvent.AddParticipant(ownerId, participantId, "Bob", null, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void RespondToInvitation_WhenParticipantAccepts_UpdatesStatus()
    {
        var ownerId = Guid.NewGuid();
        var participantId = Guid.NewGuid();
        var travelEvent = TravelEvent.Scheduled(ownerId, "Trip", TravelEventType.OvernightTrip, new DateOnly(2026, 7, 4), new DateOnly(2026, 7, 5));
        travelEvent.AddParticipant(ownerId, participantId, "Bob", null, DateTimeOffset.UtcNow);

        travelEvent.RespondToInvitation(participantId, TravelParticipantStatus.Accepted, DateTimeOffset.UtcNow);

        Assert.Equal(TravelParticipantStatus.Accepted, Assert.Single(travelEvent.Participants).Status);
    }

    [Fact]
    public void UpdateSharedDetails_WhenActorIsNotOwner_ThrowsConflict()
    {
        var ownerId = Guid.NewGuid();
        var participantId = Guid.NewGuid();
        var travelEvent = TravelEvent.Scheduled(ownerId, "Trip", TravelEventType.OvernightTrip, new DateOnly(2026, 7, 4), new DateOnly(2026, 7, 5));
        travelEvent.AddParticipant(ownerId, participantId, "Bob", null, DateTimeOffset.UtcNow);

        Assert.Throws<TravelCalendarConflictException>(() => travelEvent.UpdateSharedDetails(
            participantId,
            "Updated",
            TravelEventType.DayTrip,
            new DateOnly(2026, 7, 7),
            new DateOnly(2026, 7, 7),
            null,
            null,
            0,
            null,
            0,
            DateTimeOffset.UtcNow));
    }
}
