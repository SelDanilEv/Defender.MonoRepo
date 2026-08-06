import React from "react";
import { act } from "react";
import { createRoot, Root } from "react-dom/client";
import { fireEvent, render, screen, waitFor } from "@testing-library/react";

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
  points: [],
  otherCostPln: 0,
  transportCostPln: 0,
  totalCostPln: 0,
};

// DayTrip `event` above hides the hotel field; OvernightTrip is needed so all three
// budget inputs (hotel, transport, other) render together.
const overnightEvent: TravelEvent = {
  ...event,
  id: "44444444-4444-4444-8444-444444444444",
  type: "OvernightTrip",
  hotel: { isBooked: false, costPln: 0 },
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

  test("TransportField_WhenTypeIsTrip_UpdatesLiveTotal", () => {
    const tripEvent: TravelEvent = { ...event, type: "DayTrip", transportCostPln: 0, otherCostPln: 50 };

    act(() => {
      root.render(<EventDrawer {...props} event={tripEvent} />);
    });

    const transportInput = document.querySelectorAll<HTMLInputElement>('input[type="number"]')[0];
    expect(transportInput).not.toBeUndefined();

    act(() => {
      const valueSetter = Object.getOwnPropertyDescriptor(HTMLInputElement.prototype, "value")?.set;
      valueSetter?.call(transportInput, "100");
      transportInput.dispatchEvent(new Event("input", { bubbles: true }));
    });

    expect(document.body.textContent).toContain("150");
  });
});

describe("EventDrawer budget fields", () => {
  const spinbuttons = () => screen.getAllByRole("spinbutton") as HTMLInputElement[];

  test("Budget_WhenEventHasZeroCosts_ShowsBlankInputsInsteadOfZero", () => {
    render(<EventDrawer {...props} event={overnightEvent} />);

    const inputs = spinbuttons();
    expect(inputs.length).toBe(3);
    for (const input of inputs) {
      expect(input.value).toBe("");
    }
  });

  test("Budget_WhenInputsAreBlank_StillRendersZeroTotal", () => {
    render(<EventDrawer {...props} event={overnightEvent} />);

    expect(document.body.textContent).toContain("0 PLN");
  });

  test("Budget_WhenAmountIsTypedThenCleared_SavesZero", async () => {
    const onSave = vi.fn().mockResolvedValue(undefined);
    render(<EventDrawer {...props} event={overnightEvent} onSave={onSave} />);

    const [hotelInput] = spinbuttons();
    fireEvent.change(hotelInput, { target: { value: "250" } });
    fireEvent.change(hotelInput, { target: { value: "" } });

    fireEvent.click(screen.getByRole("button", { name: "Save" }));

    await waitFor(() => expect(onSave).toHaveBeenCalled());
    const payload = onSave.mock.calls[0][0];
    expect(payload.hotelCostPln).toBe(0);
    expect(typeof payload.hotelCostPln).toBe("number");
  });

  test("Budget_WhenAmountsAreTyped_SavesNumericPayload", async () => {
    const onSave = vi.fn().mockResolvedValue(undefined);
    render(<EventDrawer {...props} event={overnightEvent} onSave={onSave} />);

    const [hotelInput, transportInput, otherInput] = spinbuttons();
    fireEvent.change(hotelInput, { target: { value: "300" } });
    fireEvent.change(transportInput, { target: { value: "120.5" } });
    fireEvent.change(otherInput, { target: { value: "40" } });

    fireEvent.click(screen.getByRole("button", { name: "Save" }));

    await waitFor(() => expect(onSave).toHaveBeenCalled());
    const payload = onSave.mock.calls[0][0];
    expect(payload.hotelCostPln).toBe(300);
    expect(payload.transportCostPln).toBe(120.5);
    expect(payload.otherCostPln).toBe(40);
  });

  test("Budget_WhenAmountsAreTyped_UpdatesLiveTotal", () => {
    render(<EventDrawer {...props} event={overnightEvent} />);

    const [hotelInput, transportInput, otherInput] = spinbuttons();
    fireEvent.change(hotelInput, { target: { value: "100" } });
    fireEvent.change(transportInput, { target: { value: "50" } });
    fireEvent.change(otherInput, { target: { value: "25" } });

    expect(document.body.textContent).toContain("175 PLN");
  });
});
