import { isToday } from "./calendarMath";

describe("travel calendar today", () => {
  it("identifies local current date", () => {
    const now = new Date(2026, 6, 18, 23, 59);

    expect(isToday("2026-07-18", now)).toBe(true);
    expect(isToday("2026-07-19", now)).toBe(false);
  });
});
