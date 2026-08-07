import { HealthEvent } from "src/api/healthCare";
import {
  filterEventsByDateRange,
  resolveHealthDateRange,
  validateHealthDateRange,
} from "./dateRange";

const event = (id: string, startedAt: string): HealthEvent => ({
  id,
  type: "Wellbeing",
  startedAt,
  wellbeingScore: 3,
});

describe("health care date range helpers", () => {
  const anchor = new Date("2026-06-21T12:00:00.000Z");

  test("resolveHealthDateRange_WhenPresetSelected_ReturnsInclusiveBounds", () => {
    expect(resolveHealthDateRange({ kind: "preset", preset: "week" }, anchor)).toEqual({
      from: new Date("2026-06-14T12:00:00.000Z"),
      to: anchor,
    });
  });

  test("resolveHealthDateRange_WhenAllSelected_ReturnsUnboundedRange", () => {
    expect(resolveHealthDateRange({ kind: "preset", preset: "all" }, anchor)).toEqual({
      from: undefined,
      to: undefined,
    });
  });

  test("resolveHealthDateRange_WhenCustomSelected_PreservesExactDates", () => {
    const from = new Date("2026-06-18T08:30:00.000Z");
    const to = new Date("2026-06-19T17:00:00.000Z");

    expect(resolveHealthDateRange({ kind: "custom", from, to }, anchor)).toEqual({
      from,
      to,
    });
  });

  test("filterEventsByDateRange_WhenEventIsOnBoundary_IncludesIt", () => {
    const from = new Date("2026-06-18T08:30:00.000Z");
    const to = new Date("2026-06-19T17:00:00.000Z");
    const events = [
      event("from", from.toISOString()),
      event("inside", "2026-06-19T12:00:00.000Z"),
      event("to", to.toISOString()),
      event("before", "2026-06-18T08:29:00.000Z"),
      event("after", "2026-06-19T17:01:00.000Z"),
    ];

    expect(
      filterEventsByDateRange(events, { kind: "custom", from, to }).map(
        (item) => item.id
      )
    ).toEqual(["from", "inside", "to"]);
  });

  test("filterEventsByDateRange_WhenAllowedBoundsExist_ClampsSelection", () => {
    const events = [
      event("before-share", "2026-06-17T12:00:00.000Z"),
      event("inside-share", "2026-06-18T12:00:00.000Z"),
      event("after-share", "2026-06-20T00:00:00.000Z"),
    ];

    expect(
      filterEventsByDateRange(
        events,
        {
          kind: "custom",
          from: new Date("2026-06-17T00:00:00.000Z"),
          to: new Date("2026-06-21T00:00:00.000Z"),
        },
        anchor,
        {
          from: new Date("2026-06-18T00:00:00.000Z"),
          to: new Date("2026-06-20T00:00:00.000Z"),
        }
      ).map((item) => item.id)
    ).toEqual(["inside-share", "after-share"]);
  });

  test("validateHealthDateRange_WhenDatesAreIncomplete_ReturnsError", () => {
    expect(validateHealthDateRange(new Date("2026-06-18T08:30:00.000Z"), null)).toBe(
      "Both dates are required."
    );
  });

  test("validateHealthDateRange_WhenFromIsNotBeforeTo_ReturnsError", () => {
    const date = new Date("2026-06-18T08:30:00.000Z");

    expect(validateHealthDateRange(date, date)).toBe(
      "The start must be before the end."
    );
  });
});
