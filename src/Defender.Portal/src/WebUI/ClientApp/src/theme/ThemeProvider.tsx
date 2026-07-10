import React, { useState } from "react";
import { ThemeProvider } from "@mui/material";
import { StylesProvider } from "@mui/styles";

import { themeCreator } from "./base";
import {
  APP_THEME_STORAGE_KEY,
  DARK_THEME_NAME,
  getNextThemeName,
  normalizeThemeName,
} from "./themeMode";

export const ThemeContext = React.createContext({
  themeName: DARK_THEME_NAME,
  setThemeName: (themeName: string): void => {},
  toggleTheme: (): void => {},
});

const ThemeProviderWrapper: React.FC = (props) => {
  const curThemeName = normalizeThemeName(
    localStorage.getItem(APP_THEME_STORAGE_KEY)
  );
  const [themeName, _setThemeName] = useState(curThemeName);
  const theme = themeCreator(themeName);
  const setThemeName = (nextThemeName: string): void => {
    const normalizedThemeName = normalizeThemeName(nextThemeName);
    localStorage.setItem(APP_THEME_STORAGE_KEY, normalizedThemeName);
    _setThemeName(normalizedThemeName);
  };
  const toggleTheme = (): void => {
    setThemeName(getNextThemeName(themeName));
  };

  return (
    <StylesProvider injectFirst>
      <ThemeContext.Provider value={{ themeName, setThemeName, toggleTheme }}>
        <ThemeProvider theme={theme}>{props.children}</ThemeProvider>
      </ThemeContext.Provider>
    </StylesProvider>
  );
};

export default ThemeProviderWrapper;
