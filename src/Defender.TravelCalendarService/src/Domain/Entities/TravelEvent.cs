using Defender.Common.Entities;
using Defender.TravelCalendarService.Domain.Exceptions;
using Defender.TravelCalendarService.Domain.Services;
using Defender.TravelCalendarService.Domain.ValueObjects;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Defender.TravelCalendarService.Domain.Entities;

[BsonIgnoreExtraElements]
public class TravelEvent : IBaseModel
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public Guid Id { get; set; } = Guid.NewGuid();

    [BsonRepresentation(BsonType.String)]
    public Guid OwnerUserId { get; set; }

    public long Version { get; set; }
    public string Title { get; set; } = string.Empty;
    public TravelEventType Type { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public string? Notes { get; set; }
    public bool IsMustVisit { get; set; }
    public string? TemplateKey { get; set; }
    public int QueueOrder { get; set; }
    public HotelDetails? Hotel { get; set; }
    public TripDetails? Trip { get; set; }
    public decimal OtherCostPln { get; set; }
    public List<TravelParticipant> Participants { get; set; } = [];
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }

    public static TravelEvent Scheduled(
        Guid ownerUserId,
        string title,
        TravelEventType type,
        DateOnly start,
        DateOnly end,
        decimal distanceKm = 0,
        decimal hotelCostPln = 0,
        decimal otherCostPln = 0) => new()
        {
            OwnerUserId = ownerUserId,
            Title = RequireText(title, 120, "TRAVEL_CALENDAR_TITLE_REQUIRED"),
            Type = type,
            StartDate = start,
            EndDate = end,
            Hotel = type == TravelEventType.OvernightTrip ? new HotelDetails { CostPln = hotelCostPln } : null,
            Trip = type is TravelEventType.OvernightTrip or TravelEventType.DayTrip ? new TripDetails { DistanceKm = distanceKm } : null,
            OtherCostPln = otherCostPln,
        };

    public static TravelEvent Queued(Guid ownerUserId, string title, int queueOrder, DateTimeOffset now) => new()
    {
        OwnerUserId = ownerUserId,
        Title = RequireText(title, 120, "TRAVEL_CALENDAR_TITLE_REQUIRED"),
        Type = TravelEventType.OvernightTrip,
        IsMustVisit = true,
        QueueOrder = queueOrder,
        Hotel = new HotelDetails(),
        Trip = new TripDetails(),
        CreatedAtUtc = now,
        UpdatedAtUtc = now,
    };

    public bool IsVisibleTo(Guid userId) => OwnerUserId == userId || Participants.Any(item => item.UserId == userId);

    public TravelParticipantStatus? GetParticipationStatus(Guid userId)
        => OwnerUserId == userId ? null : Participants.FirstOrDefault(item => item.UserId == userId)?.Status;

    public bool CanEdit(Guid userId) => OwnerUserId == userId;

    public bool CanRespond(Guid userId) => OwnerUserId != userId && Participants.Any(item => item.UserId == userId);

    public void UpdateSharedDetails(
        Guid actorUserId,
        string title,
        TravelEventType type,
        DateOnly start,
        DateOnly end,
        string? notes,
        HotelDetails? hotel,
        decimal distanceKm,
        string? mainPoint,
        decimal otherCostPln,
        DateTimeOffset now)
    {
        EnsureOwner(actorUserId);
        if (end < start)
        {
            (start, end) = (end, start);
        }

        if (distanceKm < 0 || otherCostPln < 0 || (hotel?.CostPln ?? 0) < 0)
        {
            throw new TravelCalendarValidationException("TRAVEL_CALENDAR_INVALID_COST", "Costs and distance must be non-negative.");
        }

        Title = RequireText(title, 120, "TRAVEL_CALENDAR_TITLE_REQUIRED");
        Type = type;
        StartDate = start;
        EndDate = end;
        Notes = notes?.Trim();
        OtherCostPln = otherCostPln;

        if (type == TravelEventType.OvernightTrip)
        {
            Hotel = hotel ?? new HotelDetails();
        }
        else
        {
            Hotel = null;
        }

        if (type is TravelEventType.OvernightTrip or TravelEventType.DayTrip)
        {
            Trip ??= new TripDetails();
            Trip.DistanceKm = distanceKm;
            Trip.MainPoint = mainPoint?.Trim();
        }
        else
        {
            Trip = null;
        }

        Touch(now);
    }

    public void Unschedule(Guid actorUserId, DateTimeOffset now)
    {
        EnsureOwner(actorUserId);
        StartDate = null;
        EndDate = null;
        Hotel ??= new HotelDetails();
        Trip ??= new TripDetails();
        Participants.Clear();
        Touch(now);
    }

    public void AddParticipant(Guid actorUserId, Guid participantUserId, string displayName, string? avatarUrl, DateTimeOffset now)
    {
        EnsureOwner(actorUserId);
        if (participantUserId == OwnerUserId)
        {
            throw new TravelCalendarConflictException("TRAVEL_CALENDAR_OWNER_CANNOT_INVITE_SELF", "Owner cannot invite themselves.");
        }

        if (Participants.Any(item => item.UserId == participantUserId))
        {
            throw new TravelCalendarConflictException("TRAVEL_CALENDAR_DUPLICATE_PARTICIPANT", "Participant already exists.");
        }

        Participants.Add(new TravelParticipant
        {
            UserId = participantUserId,
            DisplayName = RequireText(displayName, 120, "TRAVEL_CALENDAR_PARTICIPANT_NAME_REQUIRED"),
            AvatarUrl = avatarUrl?.Trim(),
            Status = TravelParticipantStatus.Pending,
            InvitedAtUtc = now,
        });

        Touch(now);
    }

    public void RemoveParticipant(Guid actorUserId, Guid participantUserId, DateTimeOffset now)
    {
        EnsureOwner(actorUserId);
        var participant = Participants.FirstOrDefault(item => item.UserId == participantUserId)
            ?? throw new TravelCalendarNotFoundException("Participant was not found.");
        Participants.Remove(participant);
        Touch(now);
    }

    public void Leave(Guid participantUserId, DateTimeOffset now)
    {
        var participant = Participants.FirstOrDefault(item => item.UserId == participantUserId)
            ?? throw new TravelCalendarNotFoundException("Participant was not found.");
        Participants.Remove(participant);
        Touch(now);
    }

    public void RespondToInvitation(Guid participantUserId, TravelParticipantStatus status, DateTimeOffset now)
    {
        if (status == TravelParticipantStatus.Pending)
        {
            throw new TravelCalendarValidationException("TRAVEL_CALENDAR_INVALID_PARTICIPATION_STATUS", "Pending is not a valid response.");
        }

        var participant = Participants.FirstOrDefault(item => item.UserId == participantUserId)
            ?? throw new TravelCalendarNotFoundException("Participant was not found.");
        participant.Status = status;
        participant.RespondedAtUtc = now;
        Touch(now);
    }

    public Guid AddPoint(Guid actorUserId, string text, DateTimeOffset now)
    {
        EnsureOwner(actorUserId);
        if (Type is not (TravelEventType.OvernightTrip or TravelEventType.DayTrip))
        {
            throw new TravelCalendarValidationException("TRAVEL_CALENDAR_POINTS_NOT_ALLOWED", "Points are only available for trips.");
        }

        Trip ??= new TripDetails();
        if (Trip.Points.Count >= 50)
        {
            throw new TravelCalendarValidationException("TRAVEL_CALENDAR_POINT_LIMIT", "A trip can contain at most 50 points.");
        }

        var point = new PointOfInterest { Text = RequireText(text, 160, "TRAVEL_CALENDAR_POINT_REQUIRED") };
        Trip.Points.Add(point);
        Touch(now);
        return point.Id;
    }

    public void UpdatePoint(Guid actorUserId, Guid pointId, string? text, bool? isChecked, DateTimeOffset now)
    {
        EnsureOwner(actorUserId);
        var point = Trip?.Points.FirstOrDefault(value => value.Id == pointId)
            ?? throw new TravelCalendarNotFoundException("Point was not found.");
        if (text != null)
        {
            point.Text = RequireText(text, 160, "TRAVEL_CALENDAR_POINT_REQUIRED");
        }

        if (isChecked != null)
        {
            point.IsChecked = isChecked.Value;
        }

        Touch(now);
    }

    public void RemovePoint(Guid actorUserId, Guid pointId, DateTimeOffset now)
    {
        EnsureOwner(actorUserId);
        var points = Trip?.Points ?? throw new TravelCalendarNotFoundException("Point was not found.");
        var point = points.FirstOrDefault(value => value.Id == pointId)
            ?? throw new TravelCalendarNotFoundException("Point was not found.");
        points.Remove(point);
        Touch(now);
    }

    public void AutoSchedule(Guid actorUserId, DateOnly seasonStart, DateOnly seasonEnd, IEnumerable<TravelEvent> visibleEvents, DateTimeOffset now)
    {
        EnsureOwner(actorUserId);
        if (StartDate != null)
        {
            throw new TravelCalendarConflictException("TRAVEL_CALENDAR_ALREADY_SCHEDULED", "Event is already scheduled.");
        }

        var slot = WeekendSlotProvider.GetSlots(seasonStart, seasonEnd)
            .FirstOrDefault(candidate => visibleEvents
                .Where(item => item.Id != Id && item.StartDate != null && item.EndDate != null)
                .All(item => candidate.End < item.StartDate || candidate.Start > item.EndDate));

        if (slot == null)
        {
            throw new TravelCalendarConflictException("TRAVEL_CALENDAR_NO_FREE_WEEKEND", "No free weekend remains.");
        }

        StartDate = slot.Start;
        EndDate = slot.End;
        Type = TravelEventType.OvernightTrip;
        Hotel ??= new HotelDetails();
        Trip ??= new TripDetails();
        Touch(now);
    }

    public bool Overlaps(DateOnly start, DateOnly end) => StartDate != null && EndDate != null && !(end < StartDate || start > EndDate);

    private void EnsureOwner(Guid actorUserId)
    {
        if (OwnerUserId != actorUserId)
        {
            throw new TravelCalendarConflictException("TRAVEL_CALENDAR_OWNER_ONLY", "Only the event owner can modify shared event details.");
        }
    }

    private static string RequireText(string value, int maxLength, string code)
    {
        var result = value.Trim();
        if (result.Length == 0 || result.Length > maxLength)
        {
            throw new TravelCalendarValidationException(code, $"Text must contain 1-{maxLength} characters.");
        }

        return result;
    }

    private void Touch(DateTimeOffset now)
    {
        Version++;
        UpdatedAtUtc = now;
        if (CreatedAtUtc == default)
        {
            CreatedAtUtc = now;
        }
    }
}
