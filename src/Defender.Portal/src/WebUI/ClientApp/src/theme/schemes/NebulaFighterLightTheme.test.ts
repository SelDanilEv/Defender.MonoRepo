import { describe, expect, test } from "vitest";

import { NebulaFighterLightTheme } from "./NebulaFighterLightTheme";
import { NebulaFighterTheme } from "./NebulaFighterTheme";

describe("NebulaFighterLightTheme", () => {
  // Pins the "one fix in NebulaFighterTheme covers both themes" premise: NebulaFighterLightTheme.ts
  // never defines its own MuiToggleButton/MuiToggleButtonGroup block, only
  // `...NebulaFighterTheme.components`. Asserting deep equality (not `toBe`/reference identity) is
  // deliberate: MUI's own createTheme(base, overrides) always runs component blocks through
  // @mui/utils' deepmerge, which deep-clones every plain object it visits (see
  // node_modules/@mui/utils/deepmerge/deepmerge.js) even when target and source are the same
  // object - so the built light theme's block is never `===` the dark theme's, only structurally
  // identical to it. Verified empirically: `... === ...` is false, `toEqual` is true.
  test("MuiToggleButtonStyling_WhenLightThemeBuilt_StaysIdenticalToDarkTheme", () => {
    expect(NebulaFighterLightTheme.components?.MuiToggleButton).toEqual(
      NebulaFighterTheme.components?.MuiToggleButton,
    );
    expect(NebulaFighterLightTheme.components?.MuiToggleButtonGroup).toEqual(
      NebulaFighterTheme.components?.MuiToggleButtonGroup,
    );
  });

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
