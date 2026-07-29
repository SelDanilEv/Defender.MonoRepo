import { type PropsWithChildren, type ReactNode } from "react";
import {
  BottomNavigation,
  BottomNavigationAction,
  Box,
  Button,
  CircularProgress,
  Stack,
  Typography,
} from "@mui/material";
import AccountBalanceOutlinedIcon from "@mui/icons-material/AccountBalanceOutlined";
import CalendarMonthOutlinedIcon from "@mui/icons-material/CalendarMonthOutlined";
import ConfirmationNumberOutlinedIcon from "@mui/icons-material/ConfirmationNumberOutlined";
import HomeOutlinedIcon from "@mui/icons-material/HomeOutlined";
import MedicalServicesOutlinedIcon from "@mui/icons-material/MedicalServicesOutlined";
import PersonOutlineOutlinedIcon from "@mui/icons-material/PersonOutlineOutlined";
import RestaurantOutlinedIcon from "@mui/icons-material/RestaurantOutlined";
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
  icon?: ReactNode;
};

const navigationLinks: PortalLink[] = [
  {
    label: "Home",
    to: "/home",
    matches: (pathname) => pathname === "/home",
    icon: <HomeOutlinedIcon />,
  },
  {
    label: "Wallet",
    to: "/banking",
    matches: (pathname) => pathname.startsWith("/banking"),
    icon: <AccountBalanceOutlinedIcon />,
  },
  {
    label: "Budget",
    to: "/budget-tracker/diagram",
    matches: (pathname) => pathname.startsWith("/budget-tracker"),
    icon: <SavingsOutlinedIcon />,
  },
  {
    label: "Calendar",
    to: "/travel-calendar",
    matches: (pathname) => pathname.startsWith("/travel-calendar"),
    icon: <CalendarMonthOutlinedIcon />,
  },
  {
    label: "Food Advisor",
    to: "/food-advisor",
    matches: (pathname) => pathname.startsWith("/food-advisor"),
    icon: <RestaurantOutlinedIcon />,
  },
  {
    label: "Health Care",
    to: "/health-care",
    matches: (pathname) => pathname.startsWith("/health-care"),
    icon: <MedicalServicesOutlinedIcon />,
  },
  {
    label: "Lottery",
    to: "/games/lottery",
    matches: (pathname) => pathname.startsWith("/games"),
    icon: <ConfirmationNumberOutlinedIcon />,
  },
  {
    label: "Profile",
    to: "/account/update",
    matches: (pathname) => pathname.startsWith("/account"),
    icon: <PersonOutlineOutlinedIcon />,
  },
];

const TelegramNavigation = () => {
  const { pathname } = useLocation();
  const selectedLink = navigationLinks.findIndex((link) => link.matches(pathname));

  return (
    <BottomNavigation
        component="nav"
        aria-label="Telegram navigation"
        value={selectedLink >= 0 ? selectedLink : false}
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
          overflowX: "auto",
          overflowY: "hidden",
          justifyContent: "flex-start",
          WebkitOverflowScrolling: "touch",
          boxShadow: (theme) => `0 -1px 8px ${theme.palette.action.disabledBackground}`,
          "& .MuiBottomNavigationAction-root": {
            flex: "0 0 72px",
            minWidth: 72,
            maxWidth: 104,
            minHeight: 58,
            px: 1,
            color: "text.secondary",
            borderRadius: 1.5,
          },
          "& .MuiBottomNavigationAction-root.Mui-selected": {
            color: "primary.main",
            bgcolor: (theme) => theme.palette.action.selected,
          },
          "& .MuiBottomNavigationAction-label": {
            mt: 0.25,
            fontSize: "0.66rem",
          },
          "& .MuiBottomNavigationAction-label.Mui-selected": {
            fontSize: "0.66rem",
            fontWeight: 700,
          },
        }}
      >
        {navigationLinks.map((link, index) => (
          <BottomNavigationAction
            key={link.to}
            component={RouterLink}
            to={link.to}
            value={index}
            label={link.label}
            icon={link.icon}
            aria-current={link.matches(pathname) ? "page" : undefined}
          />
        ))}
      </BottomNavigation>
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
