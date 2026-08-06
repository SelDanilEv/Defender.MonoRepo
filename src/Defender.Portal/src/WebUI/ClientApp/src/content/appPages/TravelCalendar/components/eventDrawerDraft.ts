import { TravelEvent, TravelEventType, UpdateEventRequest } from "src/api/travelCalendar";

export interface EventDraft {
  title: string;
  type: TravelEventType;
  startDate: string;
  endDate: string;
  notes?: string;
  hotelBooked: boolean;
  hotelName?: string;
  hotelAddress?: string;
  hotelBookingUrl?: string;
  hotelCostPln: string;
  transportCostPln: string;
  mainPoint?: string;
  otherCostPln: string;
}

// Truthiness intentional: 0/undefined/null all render blank ("unset or zero shows blank").
// An event previously saved with a genuine 0 re-opens blank; it still saves back as 0
// via toAmount below, so don't "fix" this to `value ?? ""`.
export const toAmountInput = (value?: number): string => (value ? String(value) : "");

export const toAmount = (value: string): number => {
  const parsed = Number(value);
  return Number.isFinite(parsed) ? parsed : 0;
};

export const createEventDraft = (event: TravelEvent): EventDraft => ({
  title: event.title,
  type: event.type,
  startDate: event.startDate || "",
  endDate: event.endDate || "",
  notes: event.notes || "",
  hotelBooked: event.hotel?.isBooked || false,
  hotelName: event.hotel?.name || "",
  hotelAddress: event.hotel?.address || "",
  hotelBookingUrl: event.hotel?.bookingUrl || "",
  hotelCostPln: toAmountInput(event.hotel?.costPln),
  transportCostPln: toAmountInput(event.transportCostPln),
  mainPoint: event.mainPoint || "",
  otherCostPln: toAmountInput(event.otherCostPln),
});

export const toUpdateEventRequest = (draft: EventDraft): UpdateEventRequest => ({
  ...draft,
  hotelCostPln: toAmount(draft.hotelCostPln),
  transportCostPln: toAmount(draft.transportCostPln),
  otherCostPln: toAmount(draft.otherCostPln),
});
