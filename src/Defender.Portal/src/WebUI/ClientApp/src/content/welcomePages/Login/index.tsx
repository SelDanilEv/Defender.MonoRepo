import { Box, Link, Stack, Typography } from "@mui/material";
import { Link as RouterLink } from "react-router-dom";

import useUtils from "src/appUtils";
import HeaderPreferences from "src/components/HeaderPreferences";
import Logo from "src/components/LogoSign";

import LineWithText from "../Components/LineWithText";
import LoginForm from "./Form";
import LoginByGoogle from "../Components/LoginByGoogle";
import {
  loginFormContentLayout,
  loginFormPanelLayout,
  loginPageLayout,
  loginStoryPanelLayout,
  mobileLoginBrandLayout,
} from "./loginPageLayout";

const Login = () => {
  const u = useUtils();

  return (
    <Box sx={loginPageLayout}>
      <Box sx={loginStoryPanelLayout}>
        <Stack direction="row" alignItems="center" spacing={1.5}>
          <Logo width="72px" height="72px" />
          <Typography variant="h5" fontWeight={800} sx={{ color: "#fff" }}>
            Defender Portal
          </Typography>
        </Stack>
        <Box maxWidth={560}>
          <Typography
            variant="h2"
            fontWeight={800}
            lineHeight={1.05}
            sx={{
              color: "#fff",
              fontSize: { md: "2.75rem", lg: "3.5rem" },
              letterSpacing: "-0.035em",
            }}
          >
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
          <Stack direction="row" alignItems="center" spacing={1}>
            <Logo width="64px" height="64px" />
            <Typography variant="h6" fontWeight={800}>
              Defender
            </Typography>
          </Stack>
        </Box>

        <Box sx={loginFormContentLayout}>
          <Typography variant="h3" fontWeight={800}>
            {u.t("welcome:login_welcome_back")}
          </Typography>
          <Typography color="text.secondary" sx={{ mt: 1, mb: 3 }}>
            {u.t("welcome:login_continue_description")}
          </Typography>
          <LoginByGoogle fullWidth />
          <LineWithText
            margin_x="18px"
            height="1px"
            width_lg="100%"
            width_md="100%"
            width_xs="100%"
            text={u.t("welcome:or")}
            gap="12px"
          />
          <LoginForm />
          <Typography color="text.secondary" textAlign="center" sx={{ mt: 3 }}>
            {u.t("welcome:login_new_here")} {" "}
            <Link
              component={RouterLink}
              to="/welcome/create"
              fontWeight={700}
              underline="hover"
            >
              {u.t("welcome:login_create_account")}
            </Link>
          </Typography>
        </Box>
      </Box>
    </Box>
  );
};

export default Login;
