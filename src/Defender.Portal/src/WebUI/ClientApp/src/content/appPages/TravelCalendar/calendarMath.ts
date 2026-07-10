export const toLocalDate = (date: Date) => `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, "0")}-${String(date.getDate()).padStart(2, "0")}`;

export const buildMonthDays = (year: number, month: number): (string | null)[] => {
  const first = new Date(year, month, 1);
  const mondayOffset = (first.getDay() + 6) % 7;
  const count = new Date(year, month + 1, 0).getDate();
  return [...Array(mondayOffset).fill(null), ...Array.from({ length: count }, (_, index) => toLocalDate(new Date(year, month, index + 1)))];
};

export const eventForDate = <T extends { startDate?: string | null; endDate?: string | null }>(events: T[], date: string) => events.find((item) => item.startDate && item.endDate && date >= item.startDate && date <= item.endDate);

export const normalizeClickedRange = (value: string) => {
  const date = new Date(`${value}T12:00:00`); const day = date.getDay();
  if (day === 0) { const start = new Date(date); start.setDate(start.getDate() - 1); return { start: toLocalDate(start), end: value }; }
  if (day === 6) { const end = new Date(date); end.setDate(end.getDate() + 1); return { start: value, end: toLocalDate(end) }; }
  return { start: value, end: value };
};
