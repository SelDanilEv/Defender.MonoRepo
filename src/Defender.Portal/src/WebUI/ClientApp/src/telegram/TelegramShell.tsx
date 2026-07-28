import type { PropsWithChildren } from "react";
import {
  Box,
  Button,
  CircularProgress,
  Container,
  Paper,
  Stack,
  Typography,
} from "@mui/material";
import { Link as RouterLink } from "react-router-dom";

type TelegramShellProps = PropsWithChildren<{
  title?: string;
}>;

const portalLinks = [
  { label: "Home", to: "/home" },
  { label: "Wallet", to: "/banking" },
  { label: "Budget", to: "/budget-tracker/diagram" },
  { label: "Calendar", to: "/travel-calendar" },
  { label: "Food", to: "/food-advisor" },
  { label: "Health", to: "/health-care" },
  { label: "Lottery", to: "/games/lottery" },
  { label: "Profile", to: "/account/update" },
];

export const TelegramLoading = () => (
  <TelegramShell title="Connecting Defender">
    <Stack spacing={2} sx={{ alignItems: "center", py: 6 }}>
      <CircularProgress aria-label="Connecting to Defender" />
      <Typography>Connecting your Telegram session…</Typography>
    </Stack>
  </TelegramShell>
);

export const TelegramFallback = () => (
  <TelegramShell title="Open Defender in Telegram">
    <Stack spacing={1.5} sx={{ py: 4 }}>
      <Typography variant="h5" component="h1">
        Open Defender in Telegram
      </Typography>
      <Typography color="text.secondary">
        Open the Defender bot, then select Open app.
      </Typography>
    </Stack>
  </TelegramShell>
);

const TelegramShell = ({ children, title = "Defender" }: TelegramShellProps) => (
  <Box
    sx={{
      minHeight: "100dvh",
      bgcolor: "var(--tg-theme-bg-color, background.default)",
      color: "var(--tg-theme-text-color, text.primary)",
      pt: "env(safe-area-inset-top)",
      pb: "env(safe-area-inset-bottom)",
    }}
  >
    <Container maxWidth="sm" sx={{ py: 2 }}>
      <Paper elevation={0} sx={{ p: 2, mb: 2 }}>
        <Typography variant="h6" component="p">
          {title}
        </Typography>
      </Paper>
      {children}
      {title === "Defender" && (
        <Box
          component="nav"
          aria-label="Telegram navigation"
          sx={{ display: "flex", flexWrap: "wrap", gap: 1, pt: 2 }}
        >
          {portalLinks.map((link) => (
            <Button
              key={link.to}
              component={RouterLink}
              to={link.to}
              variant="outlined"
              size="small"
            >
              {link.label}
            </Button>
          ))}
        </Box>
      )}
    </Container>
  </Box>
);

export default TelegramShell;
