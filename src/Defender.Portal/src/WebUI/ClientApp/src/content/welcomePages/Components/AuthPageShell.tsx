import { Box, Stack, Typography } from "@mui/material";
import { ReactNode } from "react";

import useUtils from "src/appUtils";
import HeaderPreferences from "src/components/HeaderPreferences";
import Logo from "src/components/LogoSign";

import {
  AUTH_MOBILE_LOGO_SIZE,
  loginFormContentLayout,
  loginFormPanelLayout,
  loginPageLayout,
  loginStoryPanelLayout,
  mobileLoginBrandLayout,
} from "../Login/loginPageLayout";

type AuthPageShellProps = {
  children: ReactNode;
  title: string;
  description: string;
};

const AuthPageShell = ({ children, title, description }: AuthPageShellProps) => {
  const u = useUtils();

  return (
    <Box sx={loginPageLayout}>
      <Box sx={loginStoryPanelLayout}>
        <Stack direction="row" spacing={1.5} sx={{
          alignItems: "center"
        }}>
          <Logo width="72px" height="72px" />
          <Typography
            variant="h5"
            sx={{
              fontWeight: 800,
              color: "#fff"
            }}>
            Defender Portal
          </Typography>
        </Stack>
        <Box sx={{
          maxWidth: 560
        }}>
          <Typography
            variant="h2"
            sx={{
              fontWeight: 800,
              lineHeight: 1.05,
              color: "#fff",
              fontSize: { md: "2.75rem", lg: "3.5rem" },
              letterSpacing: "-0.035em"
            }}>
            {u.t("welcome:login_story_title")}
          </Typography>
          <Typography
            sx={{
              mt: 2,
              maxWidth: 560,
              color: "rgba(255,255,255,0.76)",
              fontSize: { md: "1rem", lg: "1.125rem" },
            }}
          >
            {u.t("welcome:login_story_description")}
          </Typography>
        </Box>
        <Typography variant="body2" sx={{ color: "rgba(255,255,255,0.56)" }}>
          {u.t("welcome:login_story_footer")}
        </Typography>
      </Box>
      <Box sx={loginFormPanelLayout}>
        <Box
          sx={{
            position: "absolute",
            top: 16,
            right: { xs: 16, sm: 24 },
            "& .MuiIconButton-root": { width: 44, height: 44 },
            "& .MuiInputBase-root": { minHeight: 44 },
          }}
        >
          <HeaderPreferences />
        </Box>
        <Box sx={mobileLoginBrandLayout}>
          <Logo width={AUTH_MOBILE_LOGO_SIZE} height={AUTH_MOBILE_LOGO_SIZE} />
        </Box>
        <Box sx={loginFormContentLayout}>
          <Typography variant="h3" sx={{
            fontWeight: 800
          }}>
            {title}
          </Typography>
          <Typography
            sx={{
              color: "text.secondary",
              mt: 1,
              mb: 3
            }}>
            {description}
          </Typography>
          {children}
        </Box>
      </Box>
    </Box>
  );
};

export default AuthPageShell;
