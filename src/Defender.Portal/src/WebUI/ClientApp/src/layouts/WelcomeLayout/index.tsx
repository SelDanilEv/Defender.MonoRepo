import React, { useEffect, useRef } from "react";
import { FC } from "react";
import PropTypes from "prop-types";
import { Outlet, useLocation } from "react-router-dom";
import { connect } from "react-redux";
import { Box, Card, Container, Typography } from "@mui/material";
import { Helmet } from "react-helmet-async";
import { styled } from "@mui/material/styles";

import useUtils from "src/appUtils";
import Logo from "src/components/LogoSign";
import HeaderPreferences from "src/components/HeaderPreferences";
import APICallWrapper from "src/api/APIWrapper/APICallWrapper";
import apiUrls from "src/api/apiUrls";
import { logout } from "src/actions/sessionActions";
import { Session } from "src/models/Session";
import AuthorizationService from "src/services/AuthorizationService";

import { logoutPortal } from "../sharedActions";
import {
  welcomeHeaderLayout,
  welcomeLogoLayout,
  welcomePreferencesLayout,
} from "./welcomeHeaderLayout";
import { modernAuthRoutes } from "./modernAuthRoutes";

const OverviewWrapper = styled(Box)(
  () => `
    overflow: auto;
    flex: 1;
    overflow-x: hidden;
    align-items: center;
`
);

const TypographyH1 = styled(Typography)(
  ({ theme }) => `
    font-size: ${theme.typography.pxToRem(25)};
`
);

const WelcomeLayout: FC = (props: any) => {
  const session = props.session as Session;

  const u = useUtils();
  const sessionRef = useRef(session);
  const utilsRef = useRef(u);
  const logoutRef = useRef(props.logout);
  const location = useLocation();
  const isModernAuthPage = modernAuthRoutes.includes(location.pathname);

  // Session validation is intentionally performed once when the welcome layout mounts.
  useEffect(() => {
    if (sessionRef.current.isAuthenticated) {
      APICallWrapper({
        url: apiUrls.home.authcheck,
        options: {
          method: "GET",
        },
        utils: utilsRef.current,
        onSuccess: async (response) => {
          AuthorizationService.HandleLoginAttempt(utilsRef.current, sessionRef.current);
        },
        onFailure: async (response) => {
          if (response.status == 401) {
            logoutPortal(utilsRef.current, logoutRef.current);
          }
        },
        showError: false,
      });
    }
  }, []);

  if (isModernAuthPage) {
    return (
      <Box sx={{ minHeight: "100dvh", width: "100%" }}>
        <Helmet>
          <title>{u.t("welcome:name_of_app")}</title>
        </Helmet>
        <Outlet />
      </Box>
    );
  }

  return (
    <Box
      sx={{
        flex: 1,
        height: "100%",
      }}
    >
      <OverviewWrapper>
        <Helmet>
          <title>{u.t("welcome:name_of_app")}</title>
        </Helmet>
        <Container maxWidth="lg">
          <Box sx={welcomeHeaderLayout}>
            <Box sx={welcomeLogoLayout}>
              <Logo width="100px" height="100px" />
            </Box>
            <Box sx={welcomePreferencesLayout}>
              <HeaderPreferences />
            </Box>
          </Box>

          <Box
            sx={{
              display: "flex",
              justifyContent: "center",
              alignItems: "center"
            }}>
            <Card
              sx={{
                py: 4,
                mb: 1,
                minWidth: "350px",
                width: "80%",
                borderRadius: 5,
              }}
            >
              <Box
                sx={{
                  maxWidth: "lg",
                  textAlign: "center"
                }}>
                <Box
                  sx={{
                    display: "flex",
                    justifyContent: "center",
                    alignItems: "center",
                    width: "auto",
                  }}
                >
                  <Box
                    sx={{
                      display: "flex",
                      flexDirection: "column",
                      gap: "5px"
                    }}>
                    {u.t("welcome:welcome_to")}
                    <TypographyH1 sx={{ mb: 2 }} variant="h1">
                      {u.t("welcome:name_of_app")}
                    </TypographyH1>
                    <Outlet />
                  </Box>
                </Box>
              </Box>
            </Card>
          </Box>
        </Container>
      </OverviewWrapper>
    </Box>
  );
};

WelcomeLayout.propTypes = {
  children: PropTypes.node,
};

const mapStateToProps = (state: any) => {
  return {
    session: state.session as Session,
  };
};

const mapDispatchToProps = (dispatch: any) => {
  return {
    logout: () => {
      dispatch(logout());
    },
  };
};

export default connect(mapStateToProps, mapDispatchToProps)(WelcomeLayout);
