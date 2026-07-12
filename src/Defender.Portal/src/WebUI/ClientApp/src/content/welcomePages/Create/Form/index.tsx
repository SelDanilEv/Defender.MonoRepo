import {
  Box,
  FormControl,
  IconButton,
  InputAdornment,
  InputLabel,
  OutlinedInput,
  TextField,
} from "@mui/material";
import { connect } from "react-redux";
import { useState } from "react";
import { Visibility, VisibilityOff } from "@mui/icons-material";

import LockedButton from "src/components/LockedComponents/LockedButton/LockedButton";
import LoadingStateService from "src/services/LoadingStateService";
import APICallWrapper from "src/api/APIWrapper/APICallWrapper";
import { login } from "src/actions/sessionActions";
import apiUrls from "src/api/apiUrls";
import useUtils from "src/appUtils";
import APIRequestValidator from "src/validators/APIRequestValidator";
import AuthorizationService from "src/services/AuthorizationService";
import { loginFormLayout } from "../../Login/loginFormLayout";

const CreateForm = (props: any) => {
  const [createRequest, setCreateRequest]: any = useState({
    email: "",
    phoneNumber: "",
    nickname: "",
    password: "",
  });

  const [showPassword, setShowPassword] = useState(false);

  const handleClickShowPassword = () => setShowPassword((show) => !show);

  const handleMouseDownPassword = (
    event: React.MouseEvent<HTMLButtonElement>
  ) => {
    event.preventDefault();
  };

  const UpdateLoginRequest = (event) => {
    setCreateRequest((current) => ({
      ...current,
      [event.target.id]: event.target.value,
    }));
  };

  const u = useUtils();

  const Create = () => {
    LoadingStateService.StartLoading();
    CreateAccount();
    LoadingStateService.FinishLoading();
  };

  const CreateAccount = async () => {
    if (
      !(await APIRequestValidator.ValidateCreateUserRequest(u, createRequest))
    )
      return;

    APICallWrapper({
      url: apiUrls.authorization.create,
      options: {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify(createRequest),
        cache: "default",
      },
      utils: u,
      onSuccess: async (response) => {
        const newSession = await response.json();

        props.login(newSession);

        AuthorizationService.HandleLoginAttempt(u, newSession);
      },
      onFailure: async (response) => {
        if (response.status == 401) {
          props.logout();
          u.react.navigate("/");
        }
      },
      showError: true,
    });
  };

  return (
    <Box sx={loginFormLayout}>
      <FormControl sx={{ gap: 2 }} variant="outlined">
        <TextField
          id="email"
          type="email"
          autoComplete="email"
          onChange={UpdateLoginRequest}
          label={u.t("welcome:email_label")}
          fullWidth
        />
        <TextField
          id="nickname"
          type="text"
          autoComplete="nickname"
          onChange={UpdateLoginRequest}
          label={u.t("welcome:nickname_label")}
          fullWidth
        />
        <TextField
          id="phoneNumber"
          type="tel"
          autoComplete="tel"
          placeholder="+(48)726101290"
          onChange={UpdateLoginRequest}
          label={u.t("welcome:phone_label")}
          fullWidth
        />
        <FormControl variant="outlined">
          <InputLabel htmlFor="password">
            {u.t("welcome:password_label")}
          </InputLabel>
          <OutlinedInput
            id="password"
            type={showPassword ? "text" : "password"}
            autoComplete="new-password"
            onChange={UpdateLoginRequest}
            endAdornment={
              <InputAdornment position="end">
                <IconButton
                  aria-label={u.t(
                    showPassword
                      ? "welcome:hide_password"
                      : "welcome:show_password"
                  )}
                  onClick={handleClickShowPassword}
                  onMouseDown={handleMouseDownPassword}
                  edge="end"
                  sx={{ width: 44, height: 44 }}
                >
                  {showPassword ? <VisibilityOff /> : <Visibility />}
                </IconButton>
              </InputAdornment>
            }
            label={u.t("welcome:password_label")}
          />
        </FormControl>
        <LockedButton
          sx={{ minHeight: 48, fontSize: "1rem", fontWeight: 700 }}
          variant="contained"
          fullWidth
          onClick={() => Create()}
        >
          {u.t("welcome:create")}
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

export default connect(mapStateToProps, mapDispatchToProps)(CreateForm);
