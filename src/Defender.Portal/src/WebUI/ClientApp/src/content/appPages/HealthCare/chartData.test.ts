import {
  filterEventsByTimeRange,
  getLatestWellbeingEvent,
  paginateHealthEvents,
  wellbeingScoreToEmoji,
} from "./chartData";
import { HealthEvent } from "src/api/healthCare";

const event = (
  id: string,
  type: HealthEvent["type"],
  startedAt: string,
  extra: Partial<HealthEvent> = {}
): HealthEvent => ({
  id,
  type,
  startedAt,
  ...extra,
});

describe("health care chart helpers", () => {
  test("wellbeingScoreToEmoji_WhenScoreIsInRange_ReturnsExpectedEmoji", () => {
    expect(wellbeingScoreToEmoji(1)).toBe("😢");
    expect(wellbeingScoreToEmoji(3)).toBe("😐");
    expect(wellbeingScoreToEmoji(5)).toBe("😄");
  });

  test("getLatestWellbeingEvent_WhenEventsContainWellbeing_ReturnsNewestInRange", () => {
    const events = [
      event("old", "Wellbeing", "2026-06-20T08:00:00.000Z", {
        wellbeingScore: 2,
      }),
      event("temperature", "Temperature", "2026-06-21T08:00:00.000Z", {
        temperatureCelsius: 37,
      }),
      event("new", "Wellbeing", "2026-06-21T09:00:00.000Z", {
        wellbeingScore: 5,
      }),
    ];

    expect(getLatestWellbeingEvent(events, "all")?.id).toBe("new");
  });

  test("filterEventsByTimeRange_WhenWellbeingIsOld_ExcludesItFromWeek", () => {
    const events = [
      event("old", "Wellbeing", "2026-06-01T08:00:00.000Z", {
        wellbeingScore: 1,
      }),
      event("recent", "Wellbeing", "2026-06-20T08:00:00.000Z", {
        wellbeingScore: 4,
      }),
    ];

    const filtered = filterEventsByTimeRange(
      events,
      "week",
      new Date("2026-06-21T08:00:00.000Z")
    );

    expect(filtered.map((item) => item.id)).toEqual(["recent"]);
  });

  test("paginateHealthEvents_WhenPageWouldOverflow_ReturnsAvailableItemsOnly", () => {
    const events = [
      event("1", "Temperature", "2026-06-21T08:00:00.000Z"),
      event("2", "Medication", "2026-06-21T09:00:00.000Z"),
      event("3", "Sleep", "2026-06-21T10:00:00.000Z"),
    ];

    expect(paginateHealthEvents(events, 1, 2).map((item) => item.id)).toEqual([
      "3",
    ]);
  });
});
