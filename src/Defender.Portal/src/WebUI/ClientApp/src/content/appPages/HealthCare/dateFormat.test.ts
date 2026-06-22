import { formatEventDateTime, formatEventTime } from "./dateFormat";

describe("health care date formatting", () => {
  test("formatEventTime_WhenLanguageIsRu_UsesTwentyFourHourTimeWithoutAmPm", () => {
    const text = formatEventTime(new Date("2026-06-22T14:30:00"), "ru");

    expect(text).not.toMatch(/AM|PM/i);
    expect(text).toContain("14");
  });

  test("formatEventDateTime_WhenLanguageIsRu_UsesTwentyFourHourTimeWithoutAmPm", () => {
    const text = formatEventDateTime(new Date("2026-06-22T14:30:00"), "ru");

    expect(text).not.toMatch(/AM|PM/i);
    expect(text).toContain("14");
  });
});
