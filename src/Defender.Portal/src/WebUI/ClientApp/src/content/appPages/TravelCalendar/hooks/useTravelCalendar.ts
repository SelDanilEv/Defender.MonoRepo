import { useCallback, useEffect, useRef, useState } from "react";
import useUtils from "src/appUtils";
import {
  MutationResult,
  TravelCalendar,
  TravelCalendarUserOption,
  TravelParticipantStatus,
  UpdateEventRequest,
  travelCalendarApi,
} from "src/api/travelCalendar";

export const useTravelCalendar = () => {
  const utils = useUtils();
  const utilsRef = useRef(utils);
  utilsRef.current = utils;

  const [calendar, setCalendar] = useState<TravelCalendar | null>(null);
  const [loading, setLoading] = useState(true);
  const [mutating, setMutating] = useState(false);
  const [error, setError] = useState("");
  const [activeEventId, setActiveEventId] = useState<string | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    setError("");
    try {
      setCalendar(await travelCalendarApi.get(utilsRef.current));
    } catch {
      setError(utilsRef.current.t("travelCalendar:errors.loadFailed"));
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    load();
  }, [load]);

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
      if (failure?.status === 409) {
        await load();
      }

      setError(utilsRef.current.t("travelCalendar:errors.saveFailed"));
      return null;
    } finally {
      setMutating(false);
    }
  }, [calendar, mutating, load]);

  const getEventVersion = useCallback((eventId: string) => calendar?.events.find((item) => item.id === eventId)?.version, [calendar]);
  const activeEvent = calendar?.events.find((item) => item.id === activeEventId) ?? null;

  return {
    calendar,
    loading,
    mutating,
    error,
    activeEvent,
    setActiveEventId,
    retry: load,
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
    createFromDate: async (date: string) => {
      const result = await run((current) => travelCalendarApi.createFromDate(current.version, date, utilsRef.current));
      if (result?.affectedEventId) {
        setActiveEventId(result.affectedEventId);
      }
    },
    saveEvent: (id: string, request: UpdateEventRequest) => {
      const version = getEventVersion(id);
      return version == null ? Promise.resolve(null) : run(() => travelCalendarApi.updateEvent(version, id, request, utilsRef.current));
    },
    removeEvent: async (id: string) => {
      const version = getEventVersion(id);
      const result = version == null ? null : await run(() => travelCalendarApi.removeEvent(version, id, utilsRef.current));
      if (result) {
        setActiveEventId(null);
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
