import { TravelEvent } from "src/api/travelCalendar";
import { normalizeClickedRange } from "./calendarMath";

export const createDraftEvent = (date: string): TravelEvent => {
  const { start, end } = normalizeClickedRange(date);
  const isWeekend = start !== end;

  return {
    id: "draft",
    version: 0,
    ownerUserId: "",
    title: isWeekend ? "Weekend trip" : "New event",
    type: isWeekend ? "OvernightTrip" : "DayTrip",
    startDate: start,
    endDate: end,
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
};
