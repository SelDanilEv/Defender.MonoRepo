import type { TravelEvent } from "src/api/travelCalendar";
import { toLocalDate } from "../calendarMath";

// Answered invitations/fully-answered sent events older than this drop off the panel
// (pure declutter - the events themselves are untouched, and anything still actionable
// ignores this window entirely, see receivedInvitations/sentInvitations below).
export const INVITATION_LOOKBACK_DAYS = 90;

const addDays = (date: string, amount: number) => {
  const [year, month, day] = date.split("-").map(Number);
  return toLocalDate(new Date(year, month - 1, day + amount));
};

export const isPast = (event: TravelEvent, today: string) => Boolean(event.endDate && event.endDate < today);

const withinLookback = (event: TravelEvent, today: string) =>
  !event.endDate || event.endDate >= addDays(today, -INVITATION_LOOKBACK_DAYS);

// Events the viewer was invited to (someone else's event, viewer is a participant).
// Pending entries are never time-limited: they're actionable, and the page hero's
// "pendingInvites" chip count reads the same underlying calendar.events array, so hiding
// an old-but-still-pending invite here would make that count silently disagree with what
// the panel shows.
export const receivedInvitations = (events: TravelEvent[], today: string): TravelEvent[] =>
  events.filter((event) => {
    if (event.canEdit || !event.myParticipationStatus) {
      return false;
    }

    return event.myParticipationStatus === "Pending" || withinLookback(event, today);
  });

// Events the viewer organizes and has invited at least one participant to. Same
// never-time-limited reasoning as receivedInvitations, sender side: as long as anyone
// hasn't answered yet, the event stays actionable/relevant to the organizer.
export const sentInvitations = (events: TravelEvent[], today: string): TravelEvent[] =>
  events.filter((event) => {
    if (!event.canEdit || event.participants.length === 0) {
      return false;
    }

    return event.participants.some((participant) => participant.status === "Pending") || withinLookback(event, today);
  });

export interface ParticipantSummary {
  total: number;
  accepted: number;
  declined: number;
  pending: number;
}

export const participantSummary = (event: TravelEvent): ParticipantSummary => {
  const summary: ParticipantSummary = { total: event.participants.length, accepted: 0, declined: 0, pending: 0 };
  event.participants.forEach((participant) => {
    if (participant.status === "Accepted") {
      summary.accepted += 1;
    } else if (participant.status === "Declined") {
      summary.declined += 1;
    } else {
      summary.pending += 1;
    }
  });

  return summary;
};

// Upcoming/ongoing first (ascending by startDate, soonest on top), then undated events,
// then past events (descending by startDate, most-recently-ended on top). Required, not
// cosmetic: useTravelCalendar's mergeCalendarPage rebuilds calendar.events from a Map, so
// array order reaching this panel is not the backend's sort order and can't be assumed.
export const compareByRelevance = (today: string) => (a: TravelEvent, b: TravelEvent): number => {
  const rank = (event: TravelEvent) => (!event.startDate ? 1 : isPast(event, today) ? 2 : 0);
  const rankOfA = rank(a);
  const rankOfB = rank(b);
  if (rankOfA !== rankOfB || !a.startDate || !b.startDate) {
    return rankOfA - rankOfB;
  }

  return rankOfA === 2 ? b.startDate.localeCompare(a.startDate) : a.startDate.localeCompare(b.startDate);
};
