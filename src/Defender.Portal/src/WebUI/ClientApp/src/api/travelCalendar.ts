import APICallWrapper from "src/api/APIWrapper/APICallWrapper";
import type IUtils from "src/appUtils/interface";

export type TravelEventType = "OvernightTrip" | "DayTrip" | "Event" | "Rest" | "Family";
export type CalendarTheme = "Light" | "Dark";
export type TravelParticipantStatus = "Pending" | "Accepted" | "Declined";

export interface PointOfInterest { id: string; text: string; isChecked: boolean; }
export interface HotelDetails { isBooked: boolean; name?: string; address?: string; bookingUrl?: string; costPln: number; }
export interface TravelParticipant { userId: string; displayName: string; avatarUrl?: string; status: TravelParticipantStatus; }
export interface TravelEvent {
  id: string;
  version: number;
  ownerUserId: string;
  ownerDisplayName?: string;
  title: string;
  type: TravelEventType;
  startDate?: string;
  endDate?: string;
  notes?: string;
  isMustVisit: boolean;
  queueOrder: number;
  participants: TravelParticipant[];
  myParticipationStatus?: TravelParticipantStatus;
  canEdit: boolean;
  canRespond: boolean;
  hotel?: HotelDetails;
  distanceKm: number;
  mainPoint?: string;
  points: PointOfInterest[];
  otherCostPln: number;
  transportCostPln: number;
  totalCostPln: number;
}
export interface PackingItem { id: string; text: string; isChecked: boolean; order: number; }
export interface TravelCalendarUserOption { userId: string; displayName: string; email: string; avatarUrl?: string; }
export interface TravelCalendar {
  id: string;
  version: number;
  baseCity: string;
  currency: string;
  seasonStart: string;
  seasonEnd: string;
  theme: CalendarTheme;
  vehicle: { name: string; fuelConsumptionLitersPer100Km: number; fuelPricePlnPerLiter: number; };
  holidays: { date: string; nameKey: string; flag: string; type: string; }[];
  events: TravelEvent[];
  packingItems: PackingItem[];
  summary: { overnightTripCount: number; hotelTotalPln: number; transportTotalPln: number; otherTotalPln: number; grandTotalPln: number; details: { eventId: string; title: string; date?: string; hotelPln: number; transportPln: number; otherPln: number; totalPln: number; }[]; };
  updatedAtUtc: string;
}
export interface MutationResult { calendar: TravelCalendar; affectedEventId?: string; affectedItemId?: string; }
export interface UpdateEventRequest { title: string; type: TravelEventType; startDate: string; endDate: string; notes?: string; hotelBooked: boolean; hotelName?: string; hotelAddress?: string; hotelBookingUrl?: string; hotelCostPln: number; distanceKm: number; mainPoint?: string; otherCostPln: number; }

const base = "/api/travelCalendar";
const call = <T>(path: string, method: string, body: unknown, utils?: IUtils | null): Promise<T> => new Promise((resolve, reject) => APICallWrapper({
  url: `${base}${path}`,
  options: { method, ...(body === undefined ? {} : { body: JSON.stringify(body) }) },
  utils,
  showError: false,
  onSuccess: async (response) => resolve(await response.json()),
  onFailure: async (failure) => reject(failure),
}));

const version = (expectedVersion: number) => ({ expectedVersion });

export const travelCalendarApi = {
  get: (from: string, to: string, utils?: IUtils | null) => call<TravelCalendar>(`?from=${encodeURIComponent(from)}&to=${encodeURIComponent(to)}`, "GET", undefined, utils),
  searchUsers: (query: string, utils?: IUtils | null) => call<TravelCalendarUserOption[]>(`/users?query=${encodeURIComponent(query)}`, "GET", undefined, utils),
  setTheme: (v: number, theme: CalendarTheme, u?: IUtils | null) => call<MutationResult>("/theme", "PATCH", { expectedVersion: v, theme }, u),
  addQueuedTrip: (v: number, title: string, u?: IUtils | null) => call<MutationResult>("/queued-trips", "POST", { expectedVersion: v, title }, u),
  createFromDate: (v: number, date: string, u?: IUtils | null) => call<MutationResult>("/events/from-date", "POST", { expectedVersion: v, date }, u),
  updateEvent: (v: number, id: string, request: UpdateEventRequest, u?: IUtils | null) => call<MutationResult>(`/events/${id}`, "PUT", { expectedVersion: v, ...request }, u),
  removeEvent: (v: number, id: string, u?: IUtils | null) => call<MutationResult>(`/events/${id}`, "DELETE", version(v), u),
  autoSchedule: (v: number, id: string, u?: IUtils | null) => call<MutationResult>(`/events/${id}/auto-schedule`, "POST", version(v), u),
  addPoint: (v: number, id: string, text: string, u?: IUtils | null) => call<MutationResult>(`/events/${id}/points`, "POST", { expectedVersion: v, text }, u),
  updatePoint: (v: number, id: string, pointId: string, patch: { text?: string; isChecked?: boolean }, u?: IUtils | null) => call<MutationResult>(`/events/${id}/points/${pointId}`, "PATCH", { expectedVersion: v, ...patch }, u),
  removePoint: (v: number, id: string, pointId: string, u?: IUtils | null) => call<MutationResult>(`/events/${id}/points/${pointId}`, "DELETE", version(v), u),
  addParticipant: (v: number, id: string, userId: string, displayName: string, avatarUrl?: string, u?: IUtils | null) => call<MutationResult>(`/events/${id}/participants`, "POST", { expectedVersion: v, userId, displayName, avatarUrl }, u),
  removeParticipant: (v: number, id: string, participantUserId: string, u?: IUtils | null) => call<MutationResult>(`/events/${id}/participants/${participantUserId}`, "DELETE", version(v), u),
  updateMyParticipation: (v: number, id: string, status: TravelParticipantStatus, u?: IUtils | null) => call<MutationResult>(`/events/${id}/my-participation`, "PATCH", { expectedVersion: v, status }, u),
  addPacking: (v: number, text: string, u?: IUtils | null) => call<MutationResult>("/packing-items", "POST", { expectedVersion: v, text }, u),
  updatePacking: (v: number, id: string, patch: { text?: string; isChecked?: boolean }, u?: IUtils | null) => call<MutationResult>(`/packing-items/${id}`, "PATCH", { expectedVersion: v, ...patch }, u),
  removePacking: (v: number, id: string, u?: IUtils | null) => call<MutationResult>(`/packing-items/${id}`, "DELETE", version(v), u),
};
