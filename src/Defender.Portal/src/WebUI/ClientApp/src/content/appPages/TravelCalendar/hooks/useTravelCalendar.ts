import { useCallback, useEffect, useRef, useState } from "react";
import useUtils from "src/appUtils";
import {
  MutationResult,
  TravelCalendar,
  TravelCalendarUserOption,
  TravelEvent,
  TravelParticipantStatus,
  UpdateEventRequest,
  travelCalendarApi,
} from "src/api/travelCalendar";
import { CalendarMonth, calendarMonths, currentCalendarMonth, monthKey, monthRange } from "../monthNavigation";
import { createDraftEvent } from "../draftEvent";

const overlaps = (event: TravelEvent, from: string, to: string) => Boolean(event.startDate && event.endDate && event.startDate <= to && event.endDate >= from);

const ERROR_MESSAGE_KEYS: Record<string, string> = {
  TRAVEL_CALENDAR_VERSION_CONFLICT: "travelCalendar:errors.saveFailed",
  TRAVEL_CALENDAR_DATE_OVERLAP: "travelCalendar:errors.dateOverlap",
  TRAVEL_CALENDAR_DATE_OUTSIDE_SEASON: "travelCalendar:errors.dateOutsideSeason",
  TRAVEL_CALENDAR_OWNER_ONLY: "travelCalendar:errors.ownerOnly",
};

const mergeCalendarPage = (previous: TravelCalendar | null, page: TravelCalendar, from: string, to: string): TravelCalendar => {
  if (!previous) {
    return page;
  }

  const events = new Map(previous.events.filter((item) => !overlaps(item, from, to)).map((item) => [item.id, item]));
  page.events.forEach((item) => events.set(item.id, item));
  return { ...page, events: [...events.values()] };
};

export const useTravelCalendar = (initialMonthCount: number) => {
  const utils = useUtils();
  const utilsRef = useRef(utils);
  const loadedMonths = useRef(new Set<string>());
  const loadingMonths = useRef(new Set<string>());
  const initialLoadStarted = useRef(false);
  utilsRef.current = utils;

  const [calendar, setCalendar] = useState<TravelCalendar | null>(null);
  const [loading, setLoading] = useState(true);
  const [mutating, setMutating] = useState(false);
  const [error, setError] = useState("");
  const [activeEventId, setActiveEventId] = useState<string | null>(null);
  const [draftEvent, setDraftEvent] = useState<TravelEvent | null>(null);

  const loadMonth = useCallback(async (month: CalendarMonth) => {
    const key = monthKey(month);
    if (loadedMonths.current.has(key) || loadingMonths.current.has(key)) {
      return;
    }

    loadingMonths.current.add(key);
    try {
      const range = monthRange(month);
      const page = await travelCalendarApi.get(range.from, range.to, utilsRef.current);
      loadedMonths.current.add(key);
      setCalendar((current) => mergeCalendarPage(current, page, range.from, range.to));
    } finally {
      loadingMonths.current.delete(key);
    }
  }, []);

  const load = useCallback(async () => {
    setLoading(true);
    setError("");
    loadedMonths.current.clear();
    loadingMonths.current.clear();
    try {
      // Fetch the full calendar unbounded (no from/to) instead of an initial N-month
      // window. calendar.events must stay complete regardless of which months are
      // scrolled into view: activeEvent/getEventVersion read it directly (a stale/narrow
      // window made responding to an out-of-window invitation silently no-op), and the
      // Received/Sent invitation feeds need every invitation, not just ones in the
      // initial window. Data volume is trivial (bounded by one travel season), so this
      // is cheap, and it also simplifies the BFF cache to one canonical
      // {userId}_all_all key instead of one entry per initial window.
      // Tripwire: if the event corpus ever outgrows a single season, the correct next
      // step is a dedicated GET /invitations?since= endpoint WITH repository-level date
      // filtering added (doesn't exist today) - not re-narrowing this call.
      const page = await travelCalendarApi.get(undefined, undefined, utilsRef.current);
      const initialMonths = calendarMonths(currentCalendarMonth(), initialMonthCount);
      initialMonths.forEach((month) => loadedMonths.current.add(monthKey(month)));
      setCalendar(page);
    } catch {
      setError(utilsRef.current.t("travelCalendar:errors.loadFailed"));
    } finally {
      setLoading(false);
    }
  }, [initialMonthCount]);

  useEffect(() => {
    if (!initialLoadStarted.current) {
      initialLoadStarted.current = true;
      load();
    }
  }, [load]);

  useEffect(() => {
    const refreshWhenVisible = () => {
      if (document.visibilityState === "visible") {
        void load();
      }
    };

    window.addEventListener("focus", refreshWhenVisible);
    document.addEventListener("visibilitychange", refreshWhenVisible);
    return () => {
      window.removeEventListener("focus", refreshWhenVisible);
      document.removeEventListener("visibilitychange", refreshWhenVisible);
    };
  }, [load]);

  const ensureMonths = useCallback(async (months: CalendarMonth[]) => {
    try {
      for (const month of months) {
        await loadMonth(month);
      }
    } catch {
      setError(utilsRef.current.t("travelCalendar:errors.loadFailed"));
    }
  }, [loadMonth]);

  const run = useCallback(async (operation: (current: TravelCalendar) => Promise<MutationResult>) => {
    if (!calendar || mutating) {
      return null;
    }

    setMutating(true);
    setError("");
    try {
      const result = await operation(calendar);
      setCalendar(result.calendar);
      return result;
    } catch (failure: any) {
      const code = failure?.code as string | undefined;
      if (code === "TRAVEL_CALENDAR_VERSION_CONFLICT" || (!code && failure?.status === 409)) {
        await load();
      }

      setError(utilsRef.current.t(ERROR_MESSAGE_KEYS[code ?? ""] ?? "travelCalendar:errors.saveFailedGeneric"));
      return null;
    } finally {
      setMutating(false);
    }
  }, [calendar, mutating, load]);

  const getEventVersion = useCallback((eventId: string) => calendar?.events.find((item) => item.id === eventId)?.version, [calendar]);
  const activeEvent = draftEvent ?? calendar?.events.find((item) => item.id === activeEventId) ?? null;
  const closeActiveEvent = () => {
    setDraftEvent(null);
    setActiveEventId(null);
  };
  const openEvent = (eventId: string) => {
    setDraftEvent(null);
    setActiveEventId(eventId);
  };

  return {
    calendar,
    loading,
    mutating,
    error,
    activeEvent,
    draftEvent,
    openEvent,
    closeActiveEvent,
    retry: load,
    clearError: () => setError(""),
    ensureMonths,
    searchUsers: async (query: string): Promise<TravelCalendarUserOption[]> => {
      if (!query.trim()) {
        return [];
      }

      try {
        return await travelCalendarApi.searchUsers(query, utilsRef.current);
      } catch {
        return [];
      }
    },
    addTrip: (title: string) => run((current) => travelCalendarApi.addQueuedTrip(current.version, title, utilsRef.current)),
    createDraft: (date: string) => {
      setActiveEventId(null);
      setDraftEvent(createDraftEvent(date));
    },
    saveDraft: async (request: UpdateEventRequest) => {
      const result = await run((current) => travelCalendarApi.createEvent(current.version, request, utilsRef.current));
      if (result) {
        setDraftEvent(null);
      }

      return result;
    },
    saveEvent: (id: string, request: UpdateEventRequest) => {
      const version = getEventVersion(id);
      return version == null ? Promise.resolve(null) : run(() => travelCalendarApi.updateEvent(version, id, request, utilsRef.current));
    },
    removeEvent: async (id: string) => {
      const version = getEventVersion(id);
      const result = version == null ? null : await run(() => travelCalendarApi.removeEvent(version, id, utilsRef.current));
      if (result) {
        closeActiveEvent();
      }
    },
    autoSchedule: async (id: string) => {
      const version = getEventVersion(id);
      const result = version == null ? null : await run(() => travelCalendarApi.autoSchedule(version, id, utilsRef.current));
      if (result?.affectedEventId) {
        setActiveEventId(result.affectedEventId);
      }
    },
    addPoint: (id: string, text: string) => {
      const version = getEventVersion(id);
      return version == null ? Promise.resolve(null) : run(() => travelCalendarApi.addPoint(version, id, text, utilsRef.current));
    },
    updatePoint: (id: string, pointId: string, patch: { text?: string; isChecked?: boolean }) => {
      const version = getEventVersion(id);
      return version == null ? Promise.resolve(null) : run(() => travelCalendarApi.updatePoint(version, id, pointId, patch, utilsRef.current));
    },
    removePoint: (id: string, pointId: string) => {
      const version = getEventVersion(id);
      return version == null ? Promise.resolve(null) : run(() => travelCalendarApi.removePoint(version, id, pointId, utilsRef.current));
    },
    addParticipant: (eventId: string, userId: string, displayName: string, avatarUrl?: string) => {
      const version = getEventVersion(eventId);
      return version == null ? Promise.resolve(null) : run(() => travelCalendarApi.addParticipant(version, eventId, userId, displayName, avatarUrl, utilsRef.current));
    },
    removeParticipant: (eventId: string, participantUserId: string) => {
      const version = getEventVersion(eventId);
      return version == null ? Promise.resolve(null) : run(() => travelCalendarApi.removeParticipant(version, eventId, participantUserId, utilsRef.current));
    },
    updateMyParticipation: (eventId: string, status: TravelParticipantStatus) => {
      const version = getEventVersion(eventId);
      return version == null ? Promise.resolve(null) : run(() => travelCalendarApi.updateMyParticipation(version, eventId, status, utilsRef.current));
    },
    addPacking: (text: string) => run((current) => travelCalendarApi.addPacking(current.version, text, utilsRef.current)),
    updatePacking: (id: string, patch: { text?: string; isChecked?: boolean }) => run((current) => travelCalendarApi.updatePacking(current.version, id, patch, utilsRef.current)),
    removePacking: (id: string) => run((current) => travelCalendarApi.removePacking(current.version, id, utilsRef.current)),
  };
};
