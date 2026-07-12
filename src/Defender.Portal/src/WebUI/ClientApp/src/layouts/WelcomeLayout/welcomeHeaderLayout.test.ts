import {
  welcomeHeaderLayout,
  welcomeLogoLayout,
  welcomePreferencesLayout,
} from "./welcomeHeaderLayout";

describe("welcomeHeaderLayout", () => {
  test("WelcomeHeader_WhenResponsive_KeepsPreferencesClearOfCenteredLogo", () => {
    expect(welcomeHeaderLayout.gridTemplateColumns).toEqual({
      xs: "1fr",
      sm: "1fr auto 1fr",
    });
    expect(welcomeLogoLayout.gridColumn).toEqual({ xs: "1", sm: "2" });
    expect(welcomePreferencesLayout.gridColumn).toEqual({
      xs: "1",
      sm: "3",
    });
    expect(welcomePreferencesLayout.justifySelf).toEqual({
      xs: "center",
      sm: "end",
    });
    expect(welcomePreferencesLayout.mt).toEqual({ xs: 1, sm: 0 });
  });
});
