import {
  Box,
  Drawer,
  styled,
  Divider,
  Typography,
  useTheme,
  type Theme,
} from "@mui/material";
import { useContext } from "react";

import SidebarMenu from "./SidebarMenu";

import Logo from "src/components/LogoSign";
import config from "src/config.json";
import Scrollbar from "src/components/Scrollbar";
import { SidebarContext } from "src/contexts/SidebarContext";

export const getSidebarBackground = (theme: Theme) => theme.sidebar.background;

const SidebarWrapper = styled(Box)(
  ({ theme }) => `
        width: ${theme.sidebar.width};
        min-width: ${theme.sidebar.width};
        color: ${theme.sidebar.textColor};
        position: relative;
        z-index: 7;
        height: 100%;
`
);

const SidebarLogo = () => {
  return (
    <Box
      sx={{
        mx: 1.5,
        width: "calc(100% - 24px)",
        minHeight: 52,
        display: "flex",
        alignItems: "center",
        gap: 1.8
      }}>
      <Logo compact height={46} width={46} />
      <Typography
        variant="body2"
        sx={{
          fontSize: 14,
          fontWeight: 700,
          lineHeight: 1.15,
          maxWidth: 108,
          whiteSpace: "normal",
          wordBreak: "break-word",
        }}
      >
        {config.NAME_OF_APP}
      </Typography>
    </Box>
  );
};

const SideScrollbar = () => {
  const theme = useTheme();

  return (
    <Scrollbar>
      <Box sx={{
        mt: 2
      }}>
        <SidebarLogo />
      </Box>
      <Divider
        sx={{
          mt: theme.spacing(2),
          mx: theme.spacing(2),
          overflowX: "hidden",
          background: theme.sidebar.dividerBg,
        }}
      />
      <SidebarMenu />
    </Scrollbar>
  );
};

function Sidebar() {
  const { sidebarToggle, toggleSidebar } = useContext(SidebarContext);
  const closeSidebar = () => toggleSidebar();
  const theme = useTheme();

  return (
    <>
      <SidebarWrapper
        sx={{
          display: {
            xs: "none",
            lg: "inline-block",
          },
          position: "fixed",
          left: 0,
          top: 0,
          background: getSidebarBackground(theme),
          boxShadow: theme.sidebar.boxShadow,
        }}
      >
        <SideScrollbar />
      </SidebarWrapper>
      <Drawer
        sx={{
          boxShadow: `${theme.sidebar.boxShadow}`,
        }}
        anchor={theme.direction === "rtl" ? "right" : "left"}
        open={sidebarToggle}
        onClose={closeSidebar}
        variant="temporary"
        elevation={9}
      >
        <SidebarWrapper
          sx={{
            background: getSidebarBackground(theme),
          }}
        >
          <SideScrollbar />
        </SidebarWrapper>
      </Drawer>
    </>
  );
}

export default Sidebar;
