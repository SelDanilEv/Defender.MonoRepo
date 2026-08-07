import type { HealthChartShare } from "src/api/healthCare";
import {
  clampHealthDateRangeSelection,
  getHealthShareAllowedBounds,
  getInitialHealthShareSelection,
  getNextDisplayedShare,
} from "./ShareState";

const createShare = (token: string): HealthChartShare => ({
  token,
  publicUrl: `https://example.com/share/${token}`,
  events: [],
  isEnabled: true,
  createdAtUtc: "2026-06-22T00:00:00Z",
});

describe("getNextDisplayedShare", () => {
  test("WhenBackgroundRefreshFails_PreservesCurrentShare", () => {
    const currentShare = createShare("current");

    const nextShare = getNextDisplayedShare(currentShare, null, false);

    expect(nextShare).toBe(currentShare);
  });

  test("WhenAbsoluteShareLoads_StartsWithTheExactServerInterval", () => {
    const share = {
      ...createShare("absolute"),
      from: "2026-06-18T08:30:00Z",
      to: "2026-06-19T17:00:00Z",
      rangeMode: "Absolute" as const,
    };

    expect(getInitialHealthShareSelection(share)).toEqual({
      kind: "custom",
      from: new Date("2026-06-18T08:30:00Z"),
      to: new Date("2026-06-19T17:00:00Z"),
    });
  });

  test("WhenRollingShareRefreshes_UsesTheEffectiveEndAsPresetAnchor", () => {
    const share = {
      ...createShare("rolling"),
      from: "2026-08-01T12:00:00Z",
      to: "2026-08-07T12:00:00Z",
      rangeMode: "Rolling" as const,
    };

    expect(getInitialHealthShareSelection(share)).toEqual({
      kind: "preset",
      preset: "week",
    });
    expect(getHealthShareAllowedBounds(share)).toEqual({
      from: new Date("2026-08-01T12:00:00Z"),
      to: new Date("2026-08-07T12:00:00Z"),
    });
  });

  test("WhenPollingMovesShareBounds_ClampsCustomSelectionWithoutResettingIt", () => {
    const selection = {
      kind: "custom" as const,
      from: new Date("2026-08-01T00:00:00Z"),
      to: new Date("2026-08-08T00:00:00Z"),
    };

    expect(
      clampHealthDateRangeSelection(selection, {
        from: new Date("2026-08-02T00:00:00Z"),
        to: new Date("2026-08-07T00:00:00Z"),
      })
    ).toEqual({
      kind: "custom",
      from: new Date("2026-08-02T00:00:00Z"),
      to: new Date("2026-08-07T00:00:00Z"),
    });
  });
});
