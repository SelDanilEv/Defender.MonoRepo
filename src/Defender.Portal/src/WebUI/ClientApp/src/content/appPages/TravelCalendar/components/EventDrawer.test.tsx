import React from "react";
import ReactDOM from "react-dom";
import { act } from "react-dom/test-utils";

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
  onClose: jest.fn(),
  onSave: jest.fn(),
  onRemove: jest.fn(),
  onAddPoint: jest.fn(),
  onUpdatePoint: jest.fn(),
  onRemovePoint: jest.fn(),
  onSearchUsers: jest.fn().mockResolvedValue([]),
  onAddParticipant: jest.fn(),
  onRemoveParticipant: jest.fn(),
  onRespond: jest.fn(),
};

describe("EventDrawer", () => {
  let container: HTMLDivElement;

  beforeEach(() => {
    container = document.createElement("div");
    document.body.appendChild(container);
  });

  afterEach(() => {
    act(() => {
      ReactDOM.unmountComponentAtNode(container);
    });
    container.remove();
    jest.clearAllMocks();
  });

  test("Draft_WhenSameEventRefreshes_PreservesUnsavedFields", () => {
    act(() => {
      ReactDOM.render(<EventDrawer {...props} event={event} />, container);
    });

    const titleInput = document.querySelector<HTMLInputElement>('input[value="Original title"]');
    expect(titleInput).not.toBeNull();

    act(() => {
      const valueSetter = Object.getOwnPropertyDescriptor(HTMLInputElement.prototype, "value")?.set;
      valueSetter?.call(titleInput, "Unsaved title");
      titleInput?.dispatchEvent(new Event("input", { bubbles: true }));
    });

    act(() => {
      ReactDOM.render(
        <EventDrawer
          {...props}
          event={{ ...event, version: 2, participants: [{ userId: "33333333-3333-4333-8333-333333333333", displayName: "Guest", status: "Pending" }] }}
        />,
        container
      );
    });

    expect(document.querySelector<HTMLInputElement>('input[aria-label="Title"]')?.value ?? titleInput?.value).toBe("Unsaved title");
  });
});
