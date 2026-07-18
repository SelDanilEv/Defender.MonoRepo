import { describe, expect, test } from "vitest";
import { getContrastRatio } from "@mui/material/styles";

import { NebulaFighterLightTheme } from "src/theme/schemes/NebulaFighterLightTheme";
import { NebulaFighterTheme } from "src/theme/schemes/NebulaFighterTheme";

import { getSidebarBackground } from "./index";

describe("getSidebarBackground", () => {
  test("Sidebar_WhenLightThemeSelected_UsesLightSidebarToken", () => {
    expect(getSidebarBackground(NebulaFighterLightTheme))
      .toBe(NebulaFighterLightTheme.sidebar.background);
    expect(getSidebarBackground(NebulaFighterLightTheme))
      .not.toBe(NebulaFighterTheme.sidebar.background);
  });

  test("Sidebar_WhenDarkThemeSelected_KeepsSectionHeadingsReadable", () => {
    expect(
      getContrastRatio(
        NebulaFighterTheme.sidebar.menuItemHeadingColor,
        NebulaFighterTheme.sidebar.background
      )
    ).toBeGreaterThanOrEqual(4.5);
  });
});
