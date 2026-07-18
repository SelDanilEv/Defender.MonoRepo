import { alpha } from "@mui/material/styles";
import type { Theme } from "@mui/material/styles";

export const getEventDrawerPaperSx = (theme: Theme) => ({
  width: 480,
  maxWidth: "100vw",
  p: { xs: 2, sm: 3 },
  backgroundColor: theme.palette.background.paper,
  color: theme.palette.text.primary,
  backgroundImage: "none",
  "--tc-drawer": theme.palette.background.paper,
  "--tc-text": theme.palette.text.primary,
  "--tc-muted": theme.palette.text.secondary,
  "--tc-border": alpha(theme.palette.text.primary, 0.12),
  "--tc-accent": theme.palette.primary.main,
  "--tc-accent-soft": alpha(theme.palette.primary.main, 0.16),
});
