import { act, fireEvent, renderHook, waitFor } from "@testing-library/react";

import { TravelCalendar, UpdateEventRequest, travelCalendarApi } from "src/api/travelCalendar";
import { useTravelCalendar } from "./useTravelCalendar";

vi.mock("src/appUtils", () => ({
  default: () => ({ t: (key: string) => key }),
}));

vi.mock("src/api/travelCalendar", () => ({
  travelCalendarApi: {
    get: vi.fn(),
    createEvent: vi.fn(),
    createFromDate: vi.fn(),
  },
}));

const calendar = {
  id: "11111111-1111-4111-8111-111111111111",
  version: 1,
  baseCity: "Warsaw",
  currency: "PLN",
  seasonStart: "2026-07-01",
  seasonEnd: "2026-09-30",
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
  distanceKm: 0,
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
        distanceKm: 0,
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
});
