import { alpha, createTheme, darken } from "@mui/material";

import { NebulaFighterTheme } from "./NebulaFighterTheme";

const primary = "#6B5ACA";
const textPrimary = "#1A2440";
const textSecondary = "#66708F";
const backgroundDefault = "#F4F5FF";
const backgroundPaper = "#FFFFFF";
const headerBackground = "#F8F8FF";
const sidebarBackground = "#EEF0FF";

export const NebulaFighterLightTheme = createTheme(NebulaFighterTheme, {
  sidebar: {
    ...NebulaFighterTheme.sidebar,
    background: sidebarBackground,
    textColor: textPrimary,
    dividerBg: alpha(textPrimary, 0.08),
    menuItemColor: alpha(textPrimary, 0.78),
    menuItemColorActive: primary,
    menuItemBg: sidebarBackground,
    menuItemBgActive: alpha(primary, 0.1),
    menuItemIconColor: alpha(textPrimary, 0.42),
    menuItemIconColorActive: primary,
    menuItemHeadingColor: alpha(textPrimary, 0.52),
    boxShadow: `1px 0 0 ${alpha(textPrimary, 0.08)}`,
  },
  header: {
    ...NebulaFighterTheme.header,
    background: headerBackground,
    boxShadow: `0px 1px 0px ${alpha(textPrimary, 0.08)}`,
    textColor: alpha(textPrimary, 0.72),
  },
  auth: {
    storyPanelBackground:
      "radial-gradient(circle at 24% 28%, rgba(140, 124, 240, 0.32), transparent 34%), linear-gradient(145deg, #EEF0FF 0%, #D7E0FF 68%)",
    storyTextPrimary: textPrimary,
    storyTextSecondary: alpha(textPrimary, 0.78),
    formPanelBackground: backgroundDefault,
  },
  colors: {
    ...NebulaFighterTheme.colors,
    secondary: {
      lighter: alpha(textSecondary, 0.1),
      light: alpha(textSecondary, 0.3),
      main: textSecondary,
      dark: darken(textSecondary, 0.2),
    },
    primary: {
      lighter: alpha(primary, 0.1),
      light: alpha(primary, 0.25),
      main: primary,
      dark: darken(primary, 0.2),
    },
    success: {
      lighter: alpha("#2EAF6B", 0.1),
      light: alpha("#2EAF6B", 0.3),
      main: "#2EAF6B",
      dark: darken("#2EAF6B", 0.2),
    },
    warning: {
      lighter: alpha("#E8A93E", 0.1),
      light: alpha("#E8A93E", 0.3),
      main: "#E8A93E",
      dark: darken("#E8A93E", 0.2),
    },
    error: {
      lighter: alpha("#E2557A", 0.1),
      light: alpha("#E2557A", 0.3),
      main: "#E2557A",
      dark: darken("#E2557A", 0.2),
    },
    info: {
      lighter: alpha("#3E9AF6", 0.1),
      light: alpha("#3E9AF6", 0.3),
      main: "#3E9AF6",
      dark: darken("#3E9AF6", 0.2),
    },
    alpha: {
      white: {
        5: alpha(backgroundPaper, 0.02),
        10: alpha(backgroundPaper, 0.1),
        30: alpha(backgroundPaper, 0.3),
        50: alpha(backgroundPaper, 0.5),
        70: alpha(backgroundPaper, 0.7),
        100: backgroundPaper,
      },
      trueWhite: NebulaFighterTheme.colors.alpha.trueWhite,
      black: {
        5: alpha(textPrimary, 0.02),
        10: alpha(textPrimary, 0.1),
        30: alpha(textPrimary, 0.3),
        50: alpha(textPrimary, 0.5),
        70: alpha(textPrimary, 0.7),
        100: textPrimary,
      },
    },
  },
  palette: {
    ...NebulaFighterTheme.palette,
    mode: "light",
    primary: {
      light: alpha(primary, 0.25),
      main: primary,
      dark: darken(primary, 0.2),
      contrastText: backgroundPaper,
    },
    secondary: {
      light: alpha(textSecondary, 0.3),
      main: textSecondary,
      dark: darken(textSecondary, 0.2),
    },
    success: {
      light: alpha("#2EAF6B", 0.3),
      main: "#2EAF6B",
      dark: darken("#2EAF6B", 0.2),
      contrastText: backgroundPaper,
    },
    warning: {
      light: alpha("#E8A93E", 0.3),
      main: "#E8A93E",
      dark: darken("#E8A93E", 0.2),
      contrastText: backgroundPaper,
    },
    error: {
      light: alpha("#E2557A", 0.3),
      main: "#E2557A",
      dark: darken("#E2557A", 0.2),
      contrastText: backgroundPaper,
    },
    info: {
      light: alpha("#3E9AF6", 0.3),
      main: "#3E9AF6",
      dark: darken("#3E9AF6", 0.2),
      contrastText: backgroundPaper,
    },
    text: {
      primary: textPrimary,
      secondary: alpha(textPrimary, 0.7),
      disabled: alpha(textPrimary, 0.45),
    },
    background: {
      paper: backgroundPaper,
      default: backgroundDefault,
    },
    action: {
      ...NebulaFighterTheme.palette.action,
      active: textPrimary,
      selected: alpha(textPrimary, 0.08),
      disabled: alpha(textPrimary, 0.45),
      disabledBackground: alpha(textPrimary, 0.05),
      focus: alpha(textPrimary, 0.08),
    },
  },
  components: {
    ...NebulaFighterTheme.components,
    MuiBackdrop: {
      styleOverrides: {
        root: {
          backgroundColor: alpha(darken(backgroundDefault, 0.6), 0.28),
          backdropFilter: "blur(2px)",
          "&.MuiBackdrop-invisible": {
            backgroundColor: "transparent",
            backdropFilter: "blur(2px)",
          },
        },
      },
    },
    MuiDialog: {
      styleOverrides: {
        paper: {
          backgroundColor: backgroundPaper,
        },
      },
    },
    MuiCssBaseline: {
      styleOverrides: {
        body: {
          backgroundColor: backgroundDefault,
        },
        ":root": {
          "--swiper-theme-color": primary,
          colorScheme: "light",
        },
        "*::-webkit-scrollbar-thumb": {
          backgroundColor: alpha(primary, 0.25),
          borderRadius: 8,
          border: "2px solid transparent",
          backgroundClip: "content-box",
        },
        "*::-webkit-scrollbar-thumb:hover": {
          backgroundColor: primary,
        },
      },
    },
    MuiPaper: {
      styleOverrides: {
        root: {
          backgroundImage: "none",
        },
        elevation: {
          boxShadow: "0px 10px 30px rgba(87, 103, 160, 0.10)",
        },
        elevation2: {
          boxShadow: "0px 6px 18px rgba(87, 103, 160, 0.08)",
        },
        elevation24: {
          boxShadow:
            "0 0rem 10rem 0 rgb(93 115 247 / 8%), 0 0.8rem 2.3rem rgb(111 130 156 / 9%), 0 0.2rem 0.7rem rgb(17 29 57 / 10%)",
        },
      },
    },
    MuiDivider: {
      styleOverrides: {
        root: {
          background: alpha(textPrimary, 0.1),
          border: 0,
          height: 1,
        },
        wrapper: {
          background: backgroundPaper,
          fontWeight: "bold",
          height: 24,
          lineHeight: "24px",
          marginTop: -12,
          color: "inherit",
          textTransform: "uppercase",
        },
      },
    },
    MuiTooltip: {
      styleOverrides: {
        tooltip: {
          backgroundColor: alpha(textPrimary, 0.92),
          padding: "6px 12px",
          fontSize: 12,
        },
        arrow: {
          color: alpha(textPrimary, 0.92),
        },
      },
    },
    MuiTableRow: {
      styleOverrides: {
        head: {
          background: alpha(textPrimary, 0.04),
        },
        root: {
          transition: "background-color .2s",
          "&.MuiTableRow-hover:hover": {
            backgroundColor: alpha(primary, 0.04),
          },
        },
      },
    },
    MuiListItem: {
      styleOverrides: {
        root: {
          "&.MuiButtonBase-root": {
            color: textSecondary,
            "&:hover, &:active, &.active, &.Mui-selected": {
              color: textPrimary,
              background: alpha(primary, 0.12),
            },
          },
        },
      },
    },
    MuiMenu: {
      styleOverrides: {
        paper: {
          padding: 8,
        },
        list: {
          padding: 8,
          "& .MuiMenuItem-root.MuiButtonBase-root": {
            fontSize: 13,
            marginTop: 1,
            marginBottom: 1,
            transition: "all .2s",
            color: alpha(textPrimary, 0.7),
            "&:hover, &:active, &.active, &.Mui-selected": {
              color: textPrimary,
              background: alpha(primary, 0.12),
            },
          },
        },
      },
    },
    MuiMenuItem: {
      styleOverrides: {
        root: {
          background: "transparent",
          transition: "all .2s",
          "&:hover, &:active, &.active, &.Mui-selected": {
            color: textPrimary,
            background: alpha(primary, 0.12),
          },
        },
      },
    },
    MuiChip: {
      ...NebulaFighterTheme.components?.MuiChip,
      styleOverrides: {
        ...NebulaFighterTheme.components?.MuiChip?.styleOverrides,
        colorSecondary: {
          background: alpha(textPrimary, 0.05),
          color: textPrimary,
          "&:hover": {
            background: alpha(textPrimary, 0.08),
          },
        },
        deleteIcon: {
          color: alpha(textPrimary, 0.5),
          "&:hover": {
            color: alpha(textPrimary, 0.7),
          },
        },
      },
    },
    MuiSwitch: {
      styleOverrides: {
        root: {
          height: 30,
          overflow: "visible",
          "& .MuiButtonBase-root": {
            position: "absolute",
            padding: 5,
          },
          "& .MuiIconButton-root": {
            borderRadius: 100,
          },
          "& .MuiSwitch-switchBase.Mui-checked + .MuiSwitch-track": {
            opacity: 0.3,
          },
        },
        thumb: {
          border: `1px solid ${alpha(textPrimary, 0.18)}`,
          boxShadow: `0px 9px 14px ${alpha(textPrimary, 0.08)}, 0px 2px 2px ${alpha(textPrimary, 0.08)}`,
        },
        track: {
          backgroundColor: alpha(textPrimary, 0.06),
          border: `1px solid ${alpha(textPrimary, 0.1)}`,
          boxShadow: `inset 0px 1px 1px ${alpha(textPrimary, 0.08)}`,
          opacity: 1,
        },
        colorPrimary: {
          "& .MuiSwitch-thumb": {
            backgroundColor: backgroundPaper,
          },
          "&.Mui-checked .MuiSwitch-thumb": {
            backgroundColor: primary,
          },
        },
      },
    },
  },
  typography: {
    ...NebulaFighterTheme.typography,
    h3: {
      ...NebulaFighterTheme.typography.h3,
      color: textPrimary,
    },
    subtitle2: {
      ...NebulaFighterTheme.typography.subtitle2,
      color: alpha(textPrimary, 0.7),
    },
  },
});
