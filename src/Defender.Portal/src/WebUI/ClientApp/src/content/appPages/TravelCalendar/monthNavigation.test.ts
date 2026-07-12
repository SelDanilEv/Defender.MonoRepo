import { addCalendarMonths, calendarMonths, monthRange, visibleCalendarMonthCount } from "./monthNavigation";

describe("month navigation", () => {
  it("crosses a year boundary in both directions", () => {
    expect(addCalendarMonths({ year: 2026, month: 11 }, 1)).toEqual({ year: 2027, month: 0 });
    expect(addCalendarMonths({ year: 2026, month: 0 }, -1)).toEqual({ year: 2025, month: 11 });
  });

  it("builds a continuous visible range and a complete month request", () => {
    expect(calendarMonths({ year: 2026, month: 10 }, 3)).toEqual([{ year: 2026, month: 10 }, { year: 2026, month: 11 }, { year: 2027, month: 0 }]);
    expect(monthRange({ year: 2024, month: 1 })).toEqual({ from: "2024-02-01", to: "2024-02-29" });
  });

  it("shows exactly four months on desktop and preserves mobile count", () => {
    expect(visibleCalendarMonthCount(true)).toBe(4);
    expect(visibleCalendarMonthCount(false)).toBe(3);
  });
});
