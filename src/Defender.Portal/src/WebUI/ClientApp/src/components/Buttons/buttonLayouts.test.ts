import { describe, expect, test } from "vitest";

import { compactIconButtonLayout } from "./buttonLayouts";

describe("compactIconButtonLayout", () => {
  test("IconButton_WhenUsedAsCompactAction_UsesCompactAccessibleTarget", () => {
    expect(compactIconButtonLayout).toMatchObject({
      width: 30,
      height: 30,
      minWidth: 30,
      p: 0,
    });
    expect(compactIconButtonLayout["& .MuiSvgIcon-root"]).toEqual({ fontSize: 20 });
    expect(compactIconButtonLayout["&:focus-visible"]).toBeDefined();
  });
});
