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
});
