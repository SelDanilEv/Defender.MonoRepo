import type { TravelEvent, TravelParticipant } from "src/api/travelCalendar";
import {
  INVITATION_LOOKBACK_DAYS,
  compareByRelevance,
  isPast,
  participantSummary,
  receivedInvitations,
  sentInvitations,
} from "./invitationFeed";

const TODAY = "2026-08-15";

const participant = (overrides: Partial<TravelParticipant> = {}): TravelParticipant => ({
  userId: "participant-1",
  displayName: "Bob",
  status: "Pending",
  ...overrides,
});

const event = (overrides: Partial<TravelEvent> = {}): TravelEvent => ({
  id: "event-1",
  version: 1,
  ownerUserId: "owner-1",
  ownerDisplayName: "Alice",
  title: "Trip",
  type: "DayTrip",
  startDate: "2026-08-20",
  endDate: "2026-08-20",
  isMustVisit: false,
  queueOrder: 0,
  participants: [],
  canEdit: false,
  canRespond: false,
  points: [],
  otherCostPln: 0,
  transportCostPln: 0,
  totalCostPln: 0,
  ...overrides,
});

describe("INVITATION_LOOKBACK_DAYS", () => {
  test("INVITATION_LOOKBACK_DAYS_Is90", () => {
    expect(INVITATION_LOOKBACK_DAYS).toBe(90);
  });
});

describe("receivedInvitations", () => {
  test("ReceivedInvitations_ExcludesEventsTheViewerOwns", () => {
    expect(receivedInvitations([event({ canEdit: true, myParticipationStatus: "Pending" })], TODAY)).toEqual([]);
  });

  test("ReceivedInvitations_ExcludesEventsWithNoParticipationStatus", () => {
    expect(receivedInvitations([event({ canEdit: false, myParticipationStatus: undefined })], TODAY)).toEqual([]);
  });

  test("ReceivedInvitations_KeepsPendingRegardlessOfHowOldTheEventIs", () => {
    const oldPending = event({
      id: "old-pending",
      canEdit: false,
      myParticipationStatus: "Pending",
      startDate: "2020-01-01",
      endDate: "2020-01-01",
    });

    expect(receivedInvitations([oldPending], TODAY)).toEqual([oldPending]);
  });

  test("ReceivedInvitations_DropsAnsweredEventsOlderThanTheLookbackWindow", () => {
    const staleDeclined = event({
      id: "stale-declined",
      canEdit: false,
      myParticipationStatus: "Declined",
      startDate: "2020-01-01",
      endDate: "2020-01-01",
    });

    expect(receivedInvitations([staleDeclined], TODAY)).toEqual([]);
  });

  test("ReceivedInvitations_KeepsAnsweredEventsWithinTheLookbackWindow", () => {
    const recentAccepted = event({
      id: "recent-accepted",
      canEdit: false,
      myParticipationStatus: "Accepted",
      startDate: "2026-08-01",
      endDate: "2026-08-01",
    });

    expect(receivedInvitations([recentAccepted], TODAY)).toEqual([recentAccepted]);
  });

  test("ReceivedInvitations_KeepsUndatedAnsweredEvents", () => {
    const undatedAccepted = event({
      id: "undated",
      canEdit: false,
      myParticipationStatus: "Accepted",
      startDate: undefined,
      endDate: undefined,
    });

    expect(receivedInvitations([undatedAccepted], TODAY)).toEqual([undatedAccepted]);
  });
});

describe("sentInvitations", () => {
  test("SentInvitations_ExcludesEventsTheViewerDoesNotOwn", () => {
    expect(sentInvitations([event({ canEdit: false, participants: [participant()] })], TODAY)).toEqual([]);
  });

  test("SentInvitations_ExcludesOwnedEventsWithNoParticipants", () => {
    expect(sentInvitations([event({ canEdit: true, participants: [] })], TODAY)).toEqual([]);
  });

  test("SentInvitations_IgnoresTheLookbackWindowWhileAnyParticipantIsStillPending", () => {
    const oldButPending = event({
      id: "old-pending-participant",
      canEdit: true,
      startDate: "2020-01-01",
      endDate: "2020-01-01",
      participants: [participant({ status: "Pending" }), participant({ userId: "p2", status: "Accepted" })],
    });

    expect(sentInvitations([oldButPending], TODAY)).toEqual([oldButPending]);
  });

  test("SentInvitations_DropsFullyAnsweredEventsOlderThanTheLookbackWindow", () => {
    const staleFullyAnswered = event({
      id: "stale-answered",
      canEdit: true,
      startDate: "2020-01-01",
      endDate: "2020-01-01",
      participants: [participant({ status: "Accepted" }), participant({ userId: "p2", status: "Declined" })],
    });

    expect(sentInvitations([staleFullyAnswered], TODAY)).toEqual([]);
  });

  test("SentInvitations_KeepsRecentFullyAnsweredEvents", () => {
    const recentFullyAnswered = event({
      id: "recent-answered",
      canEdit: true,
      startDate: "2026-08-01",
      endDate: "2026-08-01",
      participants: [participant({ status: "Accepted" })],
    });

    expect(sentInvitations([recentFullyAnswered], TODAY)).toEqual([recentFullyAnswered]);
  });
});

describe("participantSummary", () => {
  test("ParticipantSummary_CountsEachStatusSeparately", () => {
    const withMixedParticipants = event({
      participants: [
        participant({ userId: "p1", status: "Accepted" }),
        participant({ userId: "p2", status: "Accepted" }),
        participant({ userId: "p3", status: "Declined" }),
        participant({ userId: "p4", status: "Pending" }),
      ],
    });

    expect(participantSummary(withMixedParticipants)).toEqual({ total: 4, accepted: 2, declined: 1, pending: 1 });
  });

  test("ParticipantSummary_WhenThereAreNoParticipants_ReturnsAllZeroes", () => {
    expect(participantSummary(event({ participants: [] }))).toEqual({ total: 0, accepted: 0, declined: 0, pending: 0 });
  });
});

describe("isPast", () => {
  test("IsPast_WhenEndDateIsBeforeToday_ReturnsTrue", () => {
    expect(isPast(event({ endDate: "2026-08-01" }), TODAY)).toBe(true);
  });

  test("IsPast_WhenEndDateIsTodayOrLater_ReturnsFalse", () => {
    expect(isPast(event({ endDate: TODAY }), TODAY)).toBe(false);
    expect(isPast(event({ endDate: "2026-08-20" }), TODAY)).toBe(false);
  });

  test("IsPast_WhenUndated_ReturnsFalse", () => {
    expect(isPast(event({ startDate: undefined, endDate: undefined }), TODAY)).toBe(false);
  });
});

describe("compareByRelevance", () => {
  const compare = compareByRelevance(TODAY);

  test("CompareByRelevance_SortsUpcomingEventsAscendingByStartDate", () => {
    const soon = event({ id: "soon", startDate: "2026-08-16", endDate: "2026-08-16" });
    const later = event({ id: "later", startDate: "2026-08-25", endDate: "2026-08-25" });

    expect([later, soon].sort(compare)).toEqual([soon, later]);
  });

  test("CompareByRelevance_SortsPastEventsDescendingByStartDate_MostRecentFirst", () => {
    const longAgo = event({ id: "long-ago", startDate: "2026-01-01", endDate: "2026-01-01" });
    const recentPast = event({ id: "recent-past", startDate: "2026-08-01", endDate: "2026-08-01" });

    expect([longAgo, recentPast].sort(compare)).toEqual([recentPast, longAgo]);
  });

  test("CompareByRelevance_OrdersUpcomingBeforeUndatedBeforePast", () => {
    const upcoming = event({ id: "upcoming", startDate: "2026-08-20", endDate: "2026-08-20" });
    const undated = event({ id: "undated", startDate: undefined, endDate: undefined });
    const past = event({ id: "past", startDate: "2026-01-01", endDate: "2026-01-01" });

    expect([past, undated, upcoming].sort(compare)).toEqual([upcoming, undated, past]);
  });

  test("CompareByRelevance_TreatsAnOngoingEventAsUpcomingNotPast", () => {
    const ongoing = event({ id: "ongoing", startDate: "2026-08-10", endDate: "2026-08-20" });
    const upcoming = event({ id: "upcoming", startDate: "2026-08-25", endDate: "2026-08-25" });

    expect([upcoming, ongoing].sort(compare)).toEqual([ongoing, upcoming]);
  });
});
