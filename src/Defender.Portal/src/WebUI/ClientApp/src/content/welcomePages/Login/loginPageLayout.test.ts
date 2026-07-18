import {
  AUTH_MOBILE_LOGO_SIZE,
  loginFormPanelLayout,
  loginPageLayout,
  loginStoryPanelLayout,
  mobileLoginBrandLayout,
} from "./loginPageLayout";
import { NebulaFighterTheme } from "src/theme/schemes/NebulaFighterTheme";
import { NebulaFighterLightTheme } from "src/theme/schemes/NebulaFighterLightTheme";

describe("loginPageLayout", () => {
  test("LoginPage_WhenResponsive_UsesStorySplitOnlyOnLaptop", () => {
    expect(loginPageLayout.gridTemplateColumns).toEqual({
      xs: "1fr",
      md: "52% 48%",
    });
    expect(loginStoryPanelLayout.display).toEqual({ xs: "none", md: "flex" });
    expect(mobileLoginBrandLayout.display).toEqual({ xs: "flex", md: "none" });
    expect(loginFormPanelLayout.minHeight).toEqual({ xs: "100dvh", md: "100vh" });
    expect(loginFormPanelLayout.px).toEqual({ xs: 2.5, sm: 5, lg: 10 });
    expect(AUTH_MOBILE_LOGO_SIZE).toBe("80px");
  });

  test("LoginPage_WhenThemeChanges_UsesDistinctStoryPanelTokens", () => {
    expect(NebulaFighterTheme.auth.storyPanelBackground).not.toBe(
      NebulaFighterLightTheme.auth.storyPanelBackground
    );
    expect(NebulaFighterTheme.auth.formPanelBackground).toBe(
      NebulaFighterTheme.palette.background.default
    );
    expect(NebulaFighterLightTheme.auth.formPanelBackground).toBe(
      NebulaFighterLightTheme.palette.background.default
    );
  });

  test("LoginPage_WhenLightTheme_UsesAccessibleHeadingAndLinkColors", () => {
    expect(NebulaFighterLightTheme.typography.h3.color).toBe(
      NebulaFighterLightTheme.palette.text.primary
    );
    expect(NebulaFighterLightTheme.palette.primary.main).toBe("#6B5ACA");
  });

  test("LoginPage_WhenDarkTheme_UsesAccessiblePrimaryAndHelperTextColors", () => {
    expect(NebulaFighterTheme.palette.primary.contrastText).toBe("#ffffff");
    expect(
      (
        NebulaFighterTheme.components?.MuiFormHelperText?.styleOverrides
          ?.root as { color?: string }
      )?.color
    ).toBe("rgba(255, 255, 255, 0.7)");
    expect(NebulaFighterTheme.palette.primary.main).toBe("#8C7CF0");
    expect(NebulaFighterTheme.typography.subtitle2.color).toBe(
      "rgba(203, 204, 210, 0.7)"
    );
  });
});
