export type CalendarMonth = { year: number; month: number };

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
