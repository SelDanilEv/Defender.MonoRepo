import { describe, expect, test } from "vitest";

import { NebulaFighterLightTheme } from "src/theme/schemes/NebulaFighterLightTheme";
import { NebulaFighterTheme } from "src/theme/schemes/NebulaFighterTheme";

import { getEventDrawerPaperSx } from "./eventDrawerStyles";

describe("getEventDrawerPaperSx", () => {
  test("EventDrawer_WhenThemeChanges_UsesOpaqueThemePaper", () => {
    for (const theme of [NebulaFighterTheme, NebulaFighterLightTheme]) {
      expect(getEventDrawerPaperSx(theme)).toMatchObject({
        backgroundColor: theme.palette.background.paper,
        color: theme.palette.text.primary,
        backgroundImage: "none",
      });
    }
  });
});
