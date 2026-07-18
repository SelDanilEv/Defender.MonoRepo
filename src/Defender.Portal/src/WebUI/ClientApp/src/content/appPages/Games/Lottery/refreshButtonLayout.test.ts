import { describe, expect, test } from "vitest";

import { compactRefreshButtonLayout } from "./refreshButtonLayout";

describe("compactRefreshButtonLayout", () => {
  test("RefreshButton_WhenUsedInLotteryHeader_UsesCompactAccessibleTarget", () => {
    expect(compactRefreshButtonLayout).toMatchObject({
      width: 32,
      height: 32,
      minWidth: 32,
      p: 0,
    });
    expect(compactRefreshButtonLayout["& .MuiSvgIcon-root"]).toEqual({ fontSize: 20 });
    expect(compactRefreshButtonLayout["&:focus-visible"]).toBeDefined();
  });
});
