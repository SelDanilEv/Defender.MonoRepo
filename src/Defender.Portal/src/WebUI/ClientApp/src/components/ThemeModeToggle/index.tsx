import { useContext } from "react";
import DarkModeTwoToneIcon from "@mui/icons-material/DarkModeTwoTone";
import LightModeTwoToneIcon from "@mui/icons-material/LightModeTwoTone";
import { IconButton, Tooltip } from "@mui/material";

import { ThemeContext } from "src/theme/ThemeProvider";
import { isLightTheme } from "src/theme/themeMode";

const ThemeModeToggle = () => {
  const { themeName, toggleTheme } = useContext(ThemeContext);
  const lightThemeActive = isLightTheme(themeName);

  return (
    <Tooltip
      arrow
      title={lightThemeActive ? "Switch to dark theme" : "Switch to light theme"}
    >
      <IconButton color="primary" onClick={toggleTheme}>
        {lightThemeActive ? (
          <DarkModeTwoToneIcon fontSize="small" />
        ) : (
          <LightModeTwoToneIcon fontSize="small" />
        )}
      </IconButton>
    </Tooltip>
  );
};

export default ThemeModeToggle;
