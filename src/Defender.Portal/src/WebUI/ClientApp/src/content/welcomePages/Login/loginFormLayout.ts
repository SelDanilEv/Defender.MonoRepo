export const loginFormLayout = {
  display: "flex",
  flexDirection: "column",
  width: "100%",
  gap: 2,
};

export const googleLoginButtonLayout = {
  width: "100%",
  minHeight: 48,
  justifyContent: "center",
  gap: 1.5,
  fontWeight: 700,
  textTransform: "none",
  backgroundColor: (theme: Theme) =>
    theme.palette.mode === "dark" ? "#131314" : "#fff",
  borderColor: (theme: Theme) =>
    theme.palette.mode === "dark" ? "#8E918F" : "#747775",
  color: (theme: Theme) =>
    theme.palette.mode === "dark" ? "#E3E3E3" : "#1F1F1F",
  "&:hover": {
    backgroundColor: (theme: Theme) =>
      theme.palette.mode === "dark" ? "#1f1f20" : "#f7f7f7",
    borderColor: (theme: Theme) =>
      theme.palette.mode === "dark" ? "#A8ABAF" : "#5f6368",
  },
};

export const loginInputAutoComplete = {
  login: "username",
  password: "current-password",
};
import { Theme } from "@mui/material";
