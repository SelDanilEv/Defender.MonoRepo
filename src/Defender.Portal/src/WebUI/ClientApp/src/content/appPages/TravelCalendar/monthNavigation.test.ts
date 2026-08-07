import { addCalendarMonths, calendarMonthFromDate, calendarMonths, clampMonthToRange, monthRange, visibleCalendarMonthCount } from "./monthNavigation";

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

  it("parses an ISO date string into its calendar month", () => {
    expect(calendarMonthFromDate("2026-07-01")).toEqual({ year: 2026, month: 6 });
    expect(calendarMonthFromDate("2026-09-30")).toEqual({ year: 2026, month: 8 });
  });
});

describe("clampMonthToRange", () => {
  const seasonStart = { year: 2026, month: 6 }; // July 2026
  const seasonEnd = { year: 2026, month: 8 }; // September 2026

  it("leaves the window alone when it already overlaps the season", () => {
    expect(clampMonthToRange({ year: 2026, month: 7 }, seasonStart, seasonEnd, 4)).toEqual({ year: 2026, month: 7 });
  });

  it("jumps forward to the season start when the whole window is before the season", () => {
    expect(clampMonthToRange({ year: 2026, month: 1 }, seasonStart, seasonEnd, 3)).toEqual(seasonStart);
  });

  it("jumps back so the season end is the LAST visible month when the whole window is after the season (the reported season-drift bug)", () => {
    // October 2026, 4 visible months (desktop) -> window would be Oct 2026-Jan 2027,
    // entirely outside a Jul-Sep 2026 season. Expected: first visible month becomes
    // June 2026, so June/July/August/September are shown and September (season end)
    // lands last, not first.
    expect(clampMonthToRange({ year: 2026, month: 9 }, seasonStart, seasonEnd, 4)).toEqual({ year: 2026, month: 5 });
  });

  it("does not clamp when only part of the window is outside the season", () => {
    // First visible month is the season's last month - window extends past the season
    // but still overlaps it, so no jump should happen.
    expect(clampMonthToRange(seasonEnd, seasonStart, seasonEnd, 4)).toEqual(seasonEnd);
  });
});
