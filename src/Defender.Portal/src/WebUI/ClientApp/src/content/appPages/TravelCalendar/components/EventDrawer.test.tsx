import React from "react";
import { act } from "react";
import { createRoot, Root } from "react-dom/client";

import "src/localization/i18n";
import { TravelCalendar, TravelEvent } from "src/api/travelCalendar";
import { EventDrawer } from "./EventDrawer";

const event: TravelEvent = {
  id: "11111111-1111-4111-8111-111111111111",
  version: 1,
  ownerUserId: "22222222-2222-4222-8222-222222222222",
  title: "Original title",
  type: "DayTrip",
  startDate: "2026-07-18",
  endDate: "2026-07-18",
  isMustVisit: false,
  queueOrder: 0,
  participants: [],
  canEdit: true,
  canRespond: false,
  distanceKm: 0,
  points: [],
  otherCostPln: 0,
  transportCostPln: 0,
  totalCostPln: 0,
};

const calendar = {
  vehicle: {
    fuelConsumptionLitersPer100Km: 7,
    fuelPricePlnPerLiter: 6,
  },
} as TravelCalendar;

const props = {
  calendar,
  open: true,
  busy: false,
  isDraft: false,
  onClose: vi.fn(),
  onSave: vi.fn(),
  onRemove: vi.fn(),
  onAddPoint: vi.fn(),
  onUpdatePoint: vi.fn(),
  onRemovePoint: vi.fn(),
  onSearchUsers: vi.fn().mockResolvedValue([]),
  onAddParticipant: vi.fn(),
  onRemoveParticipant: vi.fn(),
  onRespond: vi.fn(),
};

describe("EventDrawer", () => {
  let container: HTMLDivElement;
  let root: Root;

  beforeEach(() => {
    container = document.createElement("div");
    document.body.appendChild(container);
    root = createRoot(container);
  });

  afterEach(() => {
    act(() => {
      root.unmount();
    });
    container.remove();
    vi.clearAllMocks();
  });

  test("Draft_WhenSameEventRefreshes_PreservesUnsavedFields", () => {
    act(() => {
      root.render(<EventDrawer {...props} event={event} />);
    });

    const titleInput = document.querySelector<HTMLInputElement>('input[value="Original title"]');
    expect(titleInput).not.toBeNull();

    act(() => {
      const valueSetter = Object.getOwnPropertyDescriptor(HTMLInputElement.prototype, "value")?.set;
      valueSetter?.call(titleInput, "Unsaved title");
      titleInput?.dispatchEvent(new Event("input", { bubbles: true }));
    });

    act(() => {
      root.render(
        <EventDrawer
          {...props}
          event={{ ...event, version: 2, participants: [{ userId: "33333333-3333-4333-8333-333333333333", displayName: "Guest", status: "Pending" }] }}
        />
      );
    });

    expect(document.querySelector<HTMLInputElement>('input[aria-label="Title"]')?.value ?? titleInput?.value).toBe("Unsaved title");
  });
});
