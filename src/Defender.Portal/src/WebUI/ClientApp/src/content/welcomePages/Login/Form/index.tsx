import {
  Box,
  FormControl,
  IconButton,
  InputAdornment,
  Link,
  TextField,
} from "@mui/material";
import VisibilityTwoToneIcon from "@mui/icons-material/VisibilityTwoTone";
import VisibilityOffTwoToneIcon from "@mui/icons-material/VisibilityOffTwoTone";
import { Link as RouterLink } from "react-router-dom";
import { connect } from "react-redux";
import { useState } from "react";

import LockedButton from "src/components/LockedComponents/LockedButton/LockedButton";
import LoadingStateService from "src/services/LoadingStateService";
import APICallWrapper from "src/api/APIWrapper/APICallWrapper";
import { login } from "src/actions/sessionActions";
import apiUrls from "src/api/apiUrls";
import useUtils from "src/appUtils";
import { Session } from "src/models/Session";
import AuthorizationService from "src/services/AuthorizationService";
import {
  loginFormLayout,
  loginInputAutoComplete,
} from "../loginFormLayout";

const LoginForm = (props: any) => {
  const [loginRequest, setLoginRequest]: any = useState({
    login: "",
    password: "",
  });
  const [showPassword, setShowPassword] = useState(false);

  const UpdateLoginRequest = (event) => {
    setLoginRequest((current) => ({
      ...current,
      [event.target.id]: event.target.value,
    }));
  };

  const u = useUtils();

  const login = () => {
    LoadingStateService.StartLoading();
    loginWithPassword();
    LoadingStateService.FinishLoading();
  };

  const validateRequest = () => {
    if (!loginRequest.login) {
      u.e("EmptyLogin");
      return false;
    }

    if (!loginRequest.password) {
      u.e("EmptyPassword");
      return false;
    }

    return true;
  };

  const loginWithPassword = async () => {
    if (!validateRequest()) return;

    APICallWrapper({
      url: apiUrls.authorization.login,
      options: {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify(loginRequest),
        cache: "default",
      },
      utils: u,
      onSuccess: async (response) => {
        const newSession = (await response.json()) as Session;

        props.login(newSession);

        AuthorizationService.HandleLoginAttempt(u, newSession);
      },
      showError: true,
    });
  };

  return (
    <Box sx={loginFormLayout}>
      <FormControl sx={{ gap: 2 }} variant="outlined">
        <TextField
          id="login"
          type="text"
          autoComplete={loginInputAutoComplete.login}
          onChange={UpdateLoginRequest}
          label={u.t("welcome:login_label")}
          fullWidth
        />
        <Box sx={{ display: "flex", flexDirection: "column" }}>
          <TextField
            id="password"
            type={showPassword ? "text" : "password"}
            autoComplete={loginInputAutoComplete.password}
            onChange={UpdateLoginRequest}
            label={u.t("welcome:password_label")}
            fullWidth
            InputProps={{
              endAdornment: (
                <InputAdornment position="end">
                  <IconButton
                    aria-label={u.t(
                      showPassword
                        ? "welcome:hide_password"
                        : "welcome:show_password"
                    )}
                    edge="end"
                    sx={{ width: 44, height: 44 }}
                    onClick={() => setShowPassword((current) => !current)}
                  >
                    {showPassword ? (
                      <VisibilityOffTwoToneIcon />
                    ) : (
                      <VisibilityTwoToneIcon />
                    )}
                  </IconButton>
                </InputAdornment>
              ),
            }}
          />
          <Link
            component={RouterLink}
            to="/welcome/password/reset"
            sx={{ ml: "auto", mt: 0.75, fontSize: "0.875rem" }}
          >
            {u.t("welcome:reset_password_link")}
          </Link>
        </Box>

        <LockedButton
          sx={{ minHeight: 48, fontSize: "1rem", fontWeight: 700 }}
          variant="contained"
          fullWidth
          onClick={() => login()}
        >
          {u.t("welcome:sign_in")}
        </LockedButton>
      </FormControl>
    </Box>
  );
};

const mapStateToProps = (state: any) => {
  return {
    isAuthenticated: state.session.isAuthenticated,
  };
};

const mapDispatchToProps = (dispatch: any) => {
  return {
    login: (payload: any) => {
      dispatch(login(payload));
    },
  };
};

export default connect(mapStateToProps, mapDispatchToProps)(LoginForm);
