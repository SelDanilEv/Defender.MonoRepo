export const DARK_THEME_NAME = "NebulaFighterTheme";
export const LIGHT_THEME_NAME = "NebulaFighterLightTheme";
export const APP_THEME_STORAGE_KEY = "appTheme";

export const normalizeThemeName = (themeName?: string | null) =>
  themeName === LIGHT_THEME_NAME ? LIGHT_THEME_NAME : DARK_THEME_NAME;

export const getNextThemeName = (themeName?: string | null) =>
  normalizeThemeName(themeName) === DARK_THEME_NAME
    ? LIGHT_THEME_NAME
    : DARK_THEME_NAME;

export const isLightTheme = (themeName?: string | null) =>
  normalizeThemeName(themeName) === LIGHT_THEME_NAME;
