import { useState, type PropsWithChildren } from "react";
import {
  BottomNavigation,
  BottomNavigationAction,
  Box,
  Button,
  CircularProgress,
  Drawer,
  List,
  ListItemButton,
  ListItemText,
  Stack,
  Typography,
} from "@mui/material";
import AccountBalanceOutlinedIcon from "@mui/icons-material/AccountBalanceOutlined";
import CalendarMonthOutlinedIcon from "@mui/icons-material/CalendarMonthOutlined";
import HomeOutlinedIcon from "@mui/icons-material/HomeOutlined";
import MoreHorizIcon from "@mui/icons-material/MoreHoriz";
import SavingsOutlinedIcon from "@mui/icons-material/SavingsOutlined";
import { Link as RouterLink, useLocation } from "react-router";

type TelegramShellProps = PropsWithChildren<{
  title?: string;
  showNavigation?: boolean;
}>;

type PortalLink = {
  label: string;
  to: string;
  matches: (pathname: string) => boolean;
};

const primaryLinks: PortalLink[] = [
  { label: "Home", to: "/home", matches: (pathname) => pathname === "/home" },
  { label: "Wallet", to: "/banking", matches: (pathname) => pathname.startsWith("/banking") },
  { label: "Budget", to: "/budget-tracker/diagram", matches: (pathname) => pathname.startsWith("/budget-tracker") },
  { label: "Calendar", to: "/travel-calendar", matches: (pathname) => pathname.startsWith("/travel-calendar") },
];

const moreLinks: PortalLink[] = [
  { label: "Food Advisor", to: "/food-advisor", matches: (pathname) => pathname.startsWith("/food-advisor") },
  { label: "Health Care", to: "/health-care", matches: (pathname) => pathname.startsWith("/health-care") },
  { label: "Lottery", to: "/games/lottery", matches: (pathname) => pathname.startsWith("/games") },
  { label: "Profile", to: "/account/update", matches: (pathname) => pathname.startsWith("/account") },
];

const navigationIcons = [
  <HomeOutlinedIcon key="home" />,
  <AccountBalanceOutlinedIcon key="wallet" />,
  <SavingsOutlinedIcon key="budget" />,
  <CalendarMonthOutlinedIcon key="calendar" />,
];

const TelegramNavigation = () => {
  const [isMoreOpen, setMoreOpen] = useState(false);
  const { pathname } = useLocation();
  const selectedPrimaryLink = primaryLinks.findIndex((link) => link.matches(pathname));
  const isMoreSelected = moreLinks.some((link) => link.matches(pathname));

  return (
    <>
      <BottomNavigation
        component="nav"
        aria-label="Telegram navigation"
        value={selectedPrimaryLink >= 0 ? selectedPrimaryLink : isMoreSelected ? primaryLinks.length : false}
        showLabels
        sx={{
          position: "fixed",
          zIndex: (theme) => theme.zIndex.appBar,
          right: 0,
          bottom: 0,
          left: 0,
          minHeight: "calc(64px + env(safe-area-inset-bottom))",
          pb: "env(safe-area-inset-bottom)",
          borderTop: 1,
          borderColor: "divider",
          bgcolor: (theme) => `var(--tg-theme-secondary-bg-color, ${theme.palette.background.paper})`,
        }}
      >
        {primaryLinks.map((link, index) => (
          <BottomNavigationAction
            key={link.to}
            component={RouterLink}
            to={link.to}
            label={link.label}
            icon={navigationIcons[index]}
          />
        ))}
        <BottomNavigationAction
          label="More"
          icon={<MoreHorizIcon />}
          onClick={() => setMoreOpen(true)}
        />
      </BottomNavigation>
      <Drawer
        anchor="bottom"
        open={isMoreOpen}
        onClose={() => setMoreOpen(false)}
        slotProps={{
          paper: {
            sx: {
              pb: "env(safe-area-inset-bottom)",
              borderTopLeftRadius: 16,
              borderTopRightRadius: 16,
            },
          },
        }}
      >
        <List aria-label="More Defender destinations">
          {moreLinks.map((link) => (
            <ListItemButton
              key={link.to}
              component={RouterLink}
              to={link.to}
              selected={link.matches(pathname)}
              onClick={() => setMoreOpen(false)}
            >
              <ListItemText primary={link.label} />
            </ListItemButton>
          ))}
        </List>
      </Drawer>
    </>
  );
};

export const TelegramLoading = () => (
  <Stack spacing={2} sx={{ alignItems: "center", py: 6 }}>
    <CircularProgress aria-label="Connecting to Defender" />
    <Typography>Connecting your Telegram session...</Typography>
  </Stack>
);

export const TelegramFallback = () => (
  <TelegramShell title="Open Defender in Telegram" showNavigation={false}>
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

interface TelegramLinkRequiredProps {
  onSignIn: () => void;
}

export const TelegramLinkRequired = ({ onSignIn }: TelegramLinkRequiredProps) => (
  <TelegramShell title="Link your Defender account" showNavigation={false}>
    <Stack spacing={2} sx={{ py: 4 }}>
      <Typography>
        Sign in to your Defender account before linking it to Telegram.
      </Typography>
      <Button variant="contained" onClick={onSignIn}>
        Sign in to link
      </Button>
    </Stack>
  </TelegramShell>
);

const TelegramShell = ({
  children,
  title = "Defender",
  showNavigation = title === "Defender",
}: TelegramShellProps) => (
  <Box
    sx={{
      minHeight: "100dvh",
      bgcolor: (theme) => `var(--tg-theme-bg-color, ${theme.palette.background.default})`,
      color: (theme) => `var(--tg-theme-text-color, ${theme.palette.text.primary})`,
      pt: "env(safe-area-inset-top)",
      pb: showNavigation ? "calc(64px + env(safe-area-inset-bottom))" : "env(safe-area-inset-bottom)",
    }}
  >
    {title !== "Defender" && (
      <Box sx={{ px: 2, pt: 2 }}>
        <Typography variant="h6" component="p">
          {title}
        </Typography>
      </Box>
    )}
    <Box component="main" sx={{ px: 1.5, py: 2 }}>
      {children}
    </Box>
    {showNavigation && <TelegramNavigation />}
  </Box>
);

export default TelegramShell;
