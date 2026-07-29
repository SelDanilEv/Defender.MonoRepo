import { describe, expect, test } from "vitest";

import { NebulaFighterLightTheme } from "./NebulaFighterLightTheme";

describe("NebulaFighterLightTheme", () => {
  test("Tooltip_WhenLightThemeSelected_UsesReadableLightSurface", () => {
    const tooltip =
      NebulaFighterLightTheme.components?.MuiTooltip?.styleOverrides?.tooltip;
    const arrow =
      NebulaFighterLightTheme.components?.MuiTooltip?.styleOverrides?.arrow;

    expect(tooltip).toMatchObject({
      backgroundColor: NebulaFighterLightTheme.palette.background.paper,
      color: NebulaFighterLightTheme.palette.text.primary,
    });
    expect(arrow).toMatchObject({
      color: NebulaFighterLightTheme.palette.background.paper,
    });
  });
});
