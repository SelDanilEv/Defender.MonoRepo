import type { HealthChartShare } from "src/api/healthCare";
import { getNextDisplayedShare } from "./ShareState";

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
});
