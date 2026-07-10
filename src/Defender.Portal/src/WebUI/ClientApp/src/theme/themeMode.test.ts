import {
  DARK_THEME_NAME,
  LIGHT_THEME_NAME,
  getNextThemeName,
  normalizeThemeName,
} from "./themeMode";

describe("themeMode", () => {
  test("normalizeThemeName_WhenThemeUnknown_ReturnsDarkTheme", () => {
    expect(normalizeThemeName("unknown")).toBe(DARK_THEME_NAME);
  });

  test("getNextThemeName_WhenDarkThemeActive_ReturnsLightTheme", () => {
    expect(getNextThemeName(DARK_THEME_NAME)).toBe(LIGHT_THEME_NAME);
  });

  test("getNextThemeName_WhenLightThemeActive_ReturnsDarkTheme", () => {
    expect(getNextThemeName(LIGHT_THEME_NAME)).toBe(DARK_THEME_NAME);
  });
});
