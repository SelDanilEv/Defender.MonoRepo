import { Box, Link, Stack, Typography } from "@mui/material";
import { Link as RouterLink } from "react-router-dom";
import React from "react";

import APICallWrapper from "src/api/APIWrapper/APICallWrapper";
import apiUrls from "src/api/apiUrls";
import useUtils from "src/appUtils";

import AuthPageShell from "../Components/AuthPageShell";
import LockedButton from "src/components/LockedComponents/LockedButton/LockedButton";
import SuccessToast from "src/components/Toast/DefaultSuccessToast";
import { logout } from "src/actions/sessionActions";
import { connect } from "react-redux";

const Verification = (props: any) => {
  const u = useUtils();
  const intervalRef = React.useRef<ReturnType<typeof setInterval> | undefined>(undefined);

  const stopPolling = () => {
    if (intervalRef.current !== undefined) {
      clearInterval(intervalRef.current);
      intervalRef.current = undefined;
    }
  };

  const checkVerification = () => {
    APICallWrapper({
      url: `${apiUrls.verification.check}`,
      options: {
        method: "GET",
        headers: {
          "Content-Type": "application/json",
        },
        cache: "default",
      },
      utils: u,
      onSuccess: async (response) => {
        const verificationResponse = await response.json();
        if (!verificationResponse.isVerified) {
          return;
        }
        stopPolling();
        u.react.navigate("/home");
      },
      onFailure: async (response) => {
        stopPolling();
        props.logout();
        u.react.navigate("/");
      },
      showError: true,
      doLock: false,
    });
  };
  const checkVerificationRef = React.useRef(checkVerification);
  checkVerificationRef.current = checkVerification;

  React.useEffect(() => {
    checkVerificationRef.current();

    intervalRef.current = setInterval(
      () => checkVerificationRef.current(),
      10 * 1000,
    );

    return () => stopPolling();
  }, []);

  const resendVerification = () => {
    APICallWrapper({
      url: `${apiUrls.verification.resendEmail}`,
      options: {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        cache: "default",
      },
      utils: u,
      onSuccess: async (response) => {
        SuccessToast(u.t("welcome:verification_email_sent"));
      },
      showError: true,
      doLock: true,
    });
  };

  return (
    <AuthPageShell
      title={u.t("welcome:verification_title")}
      description={u.t("welcome:verification_description")}
    >
      <Stack spacing={3}>
      <Typography sx={{
        color: "text.secondary"
      }}>
        {u.t("welcome:email_verification_description")}
      </Typography>
      <LockedButton
        sx={{ minHeight: 48, fontSize: "1rem", fontWeight: 700 }}
        variant="contained"
        fullWidth
        onClick={resendVerification}
      >
        {u.t("welcome:resend_verification_email")}
      </LockedButton>
      <Box sx={{
        textAlign: "center"
      }}>
        <Link component={RouterLink} to="/welcome/login" onClick={stopPolling} sx={{
          fontWeight: 700
        }}>
          {u.t("welcome:back_to_login_page")}
        </Link>
      </Box>
      </Stack>
    </AuthPageShell>
  );
};

const mapDispatchToProps = (dispatch: any) => {
  return {
    logout: () => {
      dispatch(logout());
    },
  };
};

export default connect(null, mapDispatchToProps)(Verification);
