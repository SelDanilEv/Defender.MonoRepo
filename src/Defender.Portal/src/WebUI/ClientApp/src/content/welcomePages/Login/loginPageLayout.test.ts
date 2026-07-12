import {
  loginFormPanelLayout,
  loginPageLayout,
  loginStoryPanelLayout,
  mobileLoginBrandLayout,
} from "./loginPageLayout";

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
  });
});
