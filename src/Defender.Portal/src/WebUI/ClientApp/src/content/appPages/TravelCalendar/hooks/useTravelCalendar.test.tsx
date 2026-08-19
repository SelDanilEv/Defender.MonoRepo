import { act, fireEvent, renderHook, waitFor } from "@testing-library/react";

import { TravelCalendar, TravelEvent, UpdateEventRequest, travelCalendarApi } from "src/api/travelCalendar";
import { useTravelCalendar } from "./useTravelCalendar";
import ErrorToast from "src/components/Toast/DefaultErrorToast";

vi.mock("src/appUtils", () => ({
  default: () => ({ t: (key: string) => key }),
}));

vi.mock("src/components/Toast/DefaultErrorToast", () => ({ default: vi.fn() }));

vi.mock("src/api/travelCalendar", () => ({
  travelCalendarApi: {
    get: vi.fn(),
    createEvent: vi.fn(),
    createFromDate: vi.fn(),
    searchUsers: vi.fn(),
    updateMyParticipation: vi.fn(),
  },
}));

const calendar = {
  id: "11111111-1111-4111-8111-111111111111",
  version: 1,
  baseCity: "Warsaw",
  currency: "PLN",
  theme: "Light",
  vehicle: { name: "Car", fuelConsumptionLitersPer100Km: 7, fuelPricePlnPerLiter: 6 },
  holidays: [],
  events: [],
  packingItems: [],
  summary: { overnightTripCount: 0, hotelTotalPln: 0, transportTotalPln: 0, otherTotalPln: 0, grandTotalPln: 0, details: [] },
  updatedAtUtc: "2026-07-01T00:00:00Z",
} as TravelCalendar;

const request: UpdateEventRequest = {
  title: "Museum",
  type: "Event",
  startDate: "2026-07-18",
  endDate: "2026-07-18",
  notes: "Modern art",
  hotelBooked: false,
  hotelCostPln: 0,
  transportCostPln: 0,
  otherCostPln: 25,
};

describe("useTravelCalendar", () => {
  beforeEach(() => {
    vi.mocked(travelCalendarApi.get).mockResolvedValue(calendar);
    vi.mocked(travelCalendarApi.createEvent).mockResolvedValue({ calendar, affectedEventId: "22222222-2222-4222-8222-222222222222" });
    vi.clearAllMocks();
  });

  test("CreateDraft_WhenDateClicked_DoesNotPersistUntilSave", async () => {
    const { result } = renderHook(() => useTravelCalendar(1));
    await waitFor(() => expect(result.current.loading).toBe(false));

    act(() => result.current.createDraft("2026-07-18"));

    expect(result.current.activeEvent).toMatchObject({ id: "draft", startDate: "2026-07-18" });
    expect(travelCalendarApi.createFromDate).not.toHaveBeenCalled();
    expect(travelCalendarApi.createEvent).not.toHaveBeenCalled();

    await act(async () => {
      await result.current.saveDraft(request);
    });

    expect(travelCalendarApi.createEvent).toHaveBeenCalledWith(1, request, expect.anything());
  });

  test("WhenCalendarRegainsFocusAfterExternalInvite_ReloadsLatestCalendar", async () => {
    const invitedCalendar: TravelCalendar = {
      ...calendar,
      events: [{
        id: "33333333-3333-4333-8333-333333333333",
        version: 1,
        ownerUserId: "44444444-4444-4444-8444-444444444444",
        title: "Invited trip",
        type: "Event",
        startDate: "2026-07-18",
        endDate: "2026-07-18",
        isMustVisit: false,
        queueOrder: 0,
        participants: [],
        myParticipationStatus: "Pending",
        canEdit: false,
        canRespond: true,
        points: [],
        otherCostPln: 0,
        transportCostPln: 0,
        totalCostPln: 0,
      }],
    };
    vi.mocked(travelCalendarApi.get)
      .mockResolvedValueOnce(calendar)
      .mockResolvedValueOnce(invitedCalendar);

    const { result } = renderHook(() => useTravelCalendar(1));
    await waitFor(() => expect(result.current.loading).toBe(false));

    fireEvent.focus(window);

    await waitFor(() => expect(result.current.calendar?.events).toEqual(invitedCalendar.events));
    expect(travelCalendarApi.get).toHaveBeenCalledTimes(2);
  });

  test("Run_WhenFailureCodeIsVersionConflict_ReloadsAndShowsConflictMessage", async () => {
    vi.mocked(travelCalendarApi.createEvent).mockRejectedValueOnce({ status: 409, code: "TRAVEL_CALENDAR_VERSION_CONFLICT" });

    const { result } = renderHook(() => useTravelCalendar(1));
    await waitFor(() => expect(result.current.loading).toBe(false));

    act(() => result.current.createDraft("2026-07-18"));
    await act(async () => {
      await result.current.saveDraft(request);
    });

    await waitFor(() => expect(travelCalendarApi.get).toHaveBeenCalledTimes(2));
    expect(ErrorToast).toHaveBeenCalledWith("travelCalendar:errors.saveFailed");
  });

  test("Run_WhenFailureCodeIsDateOverlap_ShowsOverlapMessageWithoutReloading", async () => {
    vi.mocked(travelCalendarApi.createEvent).mockRejectedValueOnce({ status: 409, code: "TRAVEL_CALENDAR_DATE_OVERLAP" });

    const { result } = renderHook(() => useTravelCalendar(1));
    await waitFor(() => expect(result.current.loading).toBe(false));

    act(() => result.current.createDraft("2026-07-18"));
    await act(async () => {
      await result.current.saveDraft(request);
    });

    expect(ErrorToast).toHaveBeenCalledWith("travelCalendar:errors.dateOverlap");
    expect(travelCalendarApi.get).toHaveBeenCalledTimes(1);
  });

  test("Run_WhenFailureHasNoRecognizedCode_ShowsGenericMessage", async () => {
    vi.mocked(travelCalendarApi.createEvent).mockRejectedValueOnce({ status: 500 });

    const { result } = renderHook(() => useTravelCalendar(1));
    await waitFor(() => expect(result.current.loading).toBe(false));

    act(() => result.current.createDraft("2026-07-18"));
    await act(async () => {
      await result.current.saveDraft(request);
    });

    expect(ErrorToast).toHaveBeenCalledWith("travelCalendar:errors.saveFailedGeneric");
    expect(travelCalendarApi.get).toHaveBeenCalledTimes(1);
  });

  test("Load_WhenInitialRequestFails_ShowsLoadErrorThroughTheGlobalToast", async () => {
    vi.mocked(travelCalendarApi.get).mockRejectedValueOnce(new Error("calendar unavailable"));

    const { result } = renderHook(() => useTravelCalendar(1));
    await waitFor(() => expect(result.current.loading).toBe(false));

    expect(ErrorToast).toHaveBeenCalledWith("travelCalendar:errors.loadFailed");
  });

  test("SearchUsers_WhenRequestFails_ShowsSearchErrorThroughTheGlobalToast", async () => {
    vi.mocked(travelCalendarApi.searchUsers).mockRejectedValueOnce(new Error("search unavailable"));

    const { result } = renderHook(() => useTravelCalendar(1));
    await waitFor(() => expect(result.current.loading).toBe(false));

    let users: unknown;
    await act(async () => {
      users = await result.current.searchUsers("alex");
    });

    expect(users).toEqual([]);
    expect(ErrorToast).toHaveBeenCalledWith("travelCalendar:errors.searchUsersFailed");
  });

  test("Load_OnInitialMount_RequestsTheFullCalendarWithoutADateRange", async () => {
    const { result } = renderHook(() => useTravelCalendar(1));
    await waitFor(() => expect(result.current.loading).toBe(false));

    // Regression guard for the unbounded-fetch fix: calendar.events must stay complete
    // (not scoped to an initial N-month window), so activeEvent/getEventVersion and the
    // Received/Sent invitation feeds work regardless of which months are visible.
    expect(travelCalendarApi.get).toHaveBeenCalledWith(undefined, undefined, expect.anything());
  });

  test("EnsureMonths_AfterFullInitialLoad_KeepsEventsOutsideTheFetchedMonth", async () => {
    const septemberEvent: TravelEvent = {
      id: "55555555-5555-4555-8555-555555555555",
      version: 1,
      ownerUserId: "66666666-6666-4666-8666-666666666666",
      title: "September trip",
      type: "Event",
      startDate: "2026-09-05",
      endDate: "2026-09-05",
      isMustVisit: false,
      queueOrder: 0,
      participants: [],
      myParticipationStatus: "Pending",
      canEdit: false,
      canRespond: true,
      points: [],
      otherCostPln: 0,
      transportCostPln: 0,
      totalCostPln: 0,
    };
    const fullCalendar: TravelCalendar = { ...calendar, events: [septemberEvent] };
    const julyPage: TravelCalendar = { ...calendar, events: [] };
    vi.mocked(travelCalendarApi.get)
      .mockResolvedValueOnce(fullCalendar)
      .mockResolvedValueOnce(julyPage);

    const { result } = renderHook(() => useTravelCalendar(1));
    await waitFor(() => expect(result.current.loading).toBe(false));
    expect(result.current.calendar?.events).toEqual([septemberEvent]);

    await act(async () => {
      await result.current.ensureMonths([{ year: 2026, month: 6 }]);
    });

    expect(travelCalendarApi.get).toHaveBeenNthCalledWith(2, "2026-07-01", "2026-07-31", expect.anything());
    // The September event doesn't overlap the fetched July range, so mergeCalendarPage
    // must not have dropped it from calendar.events.
    expect(result.current.calendar?.events).toEqual([septemberEvent]);
  });

  test("UpdateMyParticipation_ForAnEventOutsideTheVisibleMonths_StillSendsTheRequest", async () => {
    const farEvent: TravelEvent = {
      id: "77777777-7777-4777-8777-777777777777",
      version: 3,
      ownerUserId: "88888888-8888-4888-8888-888888888888",
      title: "Far-future trip",
      type: "Event",
      startDate: "2026-09-25",
      endDate: "2026-09-25",
      isMustVisit: false,
      queueOrder: 0,
      participants: [],
      myParticipationStatus: "Pending",
      canEdit: false,
      canRespond: true,
      points: [],
      otherCostPln: 0,
      transportCostPln: 0,
      totalCostPln: 0,
    };
    const calendarWithFarEvent: TravelCalendar = { ...calendar, events: [farEvent] };
    vi.mocked(travelCalendarApi.get).mockResolvedValue(calendarWithFarEvent);
    vi.mocked(travelCalendarApi.updateMyParticipation).mockResolvedValue({ calendar: calendarWithFarEvent });

    const { result } = renderHook(() => useTravelCalendar(1));
    await waitFor(() => expect(result.current.loading).toBe(false));

    // Before the unbounded-fetch fix, an event dated outside the initially-fetched
    // window would be missing from calendar.events, getEventVersion would return
    // undefined, and this call would silently short-circuit to Promise.resolve(null)
    // instead of reaching the API.
    await act(async () => {
      await result.current.updateMyParticipation(farEvent.id, "Accepted");
    });

    expect(travelCalendarApi.updateMyParticipation).toHaveBeenCalledWith(3, farEvent.id, "Accepted", expect.anything());
  });
});
