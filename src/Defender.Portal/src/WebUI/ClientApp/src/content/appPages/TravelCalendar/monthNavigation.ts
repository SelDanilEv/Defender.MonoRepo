export type CalendarMonth = { year: number; month: number };

export const visibleCalendarMonthCount = (desktop: boolean) => desktop ? 4 : 3;

export const currentCalendarMonth = (): CalendarMonth => {
  const now = new Date();
  return { year: now.getFullYear(), month: now.getMonth() };
};

export const addCalendarMonths = ({ year, month }: CalendarMonth, amount: number): CalendarMonth => {
  const value = new Date(year, month + amount, 1);
  return { year: value.getFullYear(), month: value.getMonth() };
};

export const calendarMonths = (first: CalendarMonth, count: number): CalendarMonth[] =>
  Array.from({ length: count }, (_, index) => addCalendarMonths(first, index));

export const monthKey = ({ year, month }: CalendarMonth) => `${year}-${String(month + 1).padStart(2, "0")}`;

export const monthRange = (value: CalendarMonth) => {
  const from = new Date(value.year, value.month, 1);
  const to = new Date(value.year, value.month + 1, 0);
  const asIsoDate = (date: Date) => `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, "0")}-${String(date.getDate()).padStart(2, "0")}`;
  return { from: asIsoDate(from), to: asIsoDate(to) };
};

export const calendarMonthFromDate = (date: string): CalendarMonth => {
  const [year, month] = date.split("-").map(Number);
  return { year, month: month - 1 };
};

// Clamps a visible-months window into [seasonStart, seasonEnd] (both CalendarMonths), only
// when the window doesn't already overlap the season at all. Used so the calendar never
// opens on a blank grid when "today" falls entirely outside the (currently hardcoded,
// server-side) season range - see TravelCalendarPage.tsx's post-load clamp effect.
export const clampMonthToRange = (
  first: CalendarMonth,
  seasonStart: CalendarMonth,
  seasonEnd: CalendarMonth,
  visibleCount: number,
): CalendarMonth => {
  const lastVisible = addCalendarMonths(first, visibleCount - 1);
  if (monthKey(lastVisible) < monthKey(seasonStart)) {
    // Entirely before the season: jump forward so the season's first month is visible.
    return seasonStart;
  }

  if (monthKey(first) > monthKey(seasonEnd)) {
    // Entirely after the season: jump back so the season's last month is the LAST
    // visible month (its tail end), not the first.
    return addCalendarMonths(seasonEnd, -(visibleCount - 1));
  }

  // Already overlaps the season somewhere - leave it alone.
  return first;
};
