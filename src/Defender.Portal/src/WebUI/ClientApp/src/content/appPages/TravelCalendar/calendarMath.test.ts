import { buildMonthDays, eventForDate, normalizeClickedRange } from "./calendarMath";

describe("travel calendar math", () => {
  it("builds a Monday-first July 2026 grid", () => {
    const days = buildMonthDays(2026, 6);
    expect(days[0]).toBeNull();
    expect(days[2]).toBe("2026-07-01");
    expect(days.filter(Boolean)).toHaveLength(31);
  });

  it("normalizes Sunday to a weekend range", () => {
    expect(normalizeClickedRange("2026-07-12")).toEqual({ start: "2026-07-11", end: "2026-07-12" });
  });

  it("finds an event covering a date", () => {
    expect(eventForDate([{ id: "a", startDate: "2026-07-04", endDate: "2026-07-05" }], "2026-07-05")?.id).toBe("a");
  });
});
