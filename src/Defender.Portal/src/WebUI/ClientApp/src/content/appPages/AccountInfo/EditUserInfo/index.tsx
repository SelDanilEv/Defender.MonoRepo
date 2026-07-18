import {
  Typography,
  Grid,
  CardContent,
  TextField,
  Divider,
  Button,
} from "@mui/material";
import { useEffect, useState } from "react";
import { connect } from "react-redux";
import APICallWrapper from "src/api/APIWrapper/APICallWrapper";
import APICallProps from "src/api/APIWrapper/interfaces/APICallProps";
import useUtils from "src/appUtils";
import SaveIcon from "@mui/icons-material/Save";

import Text from "src/components/Text";
import { UserAccountInfo } from "src/models/UserAccountInfo";
import { updateUserInfo } from "src/actions/sessionActions";
import apiUrls from "src/api/apiUrls";
import UserService from "src/services/UserService";

const EditUserInfo = (props: any) => {
  const u = useUtils();

  let sessionUser = props.currentUser;

  const [user, setUser] = useState<UserAccountInfo>({ ...props.currentUser });
  const [isSaveActionDisabled, setSaveActionDisabled] = useState<boolean>(true);

  useEffect(() => {
    setSaveActionDisabled(sessionUser?.nickname == user?.nickname);
  }, [sessionUser, user]);

  const handleUpdateUserInfo = () => {
    setSaveActionDisabled(true);

    const requestBody = {
      nickname: user.nickname,
    };

    APICallWrapper({
      url: apiUrls.account.updateInfo,
      options: {
        method: "PUT",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify(requestBody),
      },
      utils: u,
      showSuccess: true,
      successMessage: u.t("personal_info_page__account_updated_message"),
      onSuccess: async (response) => {
        props.updateUserInfo(user);
        sessionUser = user;
      },
    } as APICallProps);
  };

  const UpdateRequest = (event) => {
    user[event.target.id] = event.target.value;
    setUser(user);
    DisableButtonIfDataNotChanged();
  };

  const DisableButtonIfDataNotChanged = () => {
    setSaveActionDisabled(sessionUser?.nickname == user?.nickname);
  };

  return (
    <>
      <CardContent sx={{ p: 4 }}>
        <Typography variant="subtitle2">
          <Grid container spacing={2} sx={{ fontSize: "1.2em" }}>
            <Grid
              container
              sx={{
                alignContent: "center",
                justifyContent: { xs: "left", sm: "center" }
              }}
              size={{
                xs: 12,
                sm: 4,
                md: 3
              }}>
              <Grid>{u.t("personal_info_page__name_field")}:</Grid>
            </Grid>
            <Grid
              size={{
                xs: 12,
                sm: 6,
                md: 7
              }}>
              <TextField
                id="nickname"
                sx={{ padding: 0 }}
                defaultValue={user.nickname}
                onChange={UpdateRequest}
                variant="standard"
                fullWidth
                slotProps={{
                  input: { style: { fontSize: "1.1em" } },
                  htmlInput: { "aria-label": u.t("personal_info_page__name_field") },
                }}
              />
            </Grid>
            <Grid
              container
              sx={{
                alignContent: "center",
                justifyContent: { xs: "left", sm: "center" }
              }}
              size={{
                xs: 12,
                sm: 4,
                md: 3
              }}>
              <Grid>{u.t("personal_info_page__created_date_field")}:</Grid>
            </Grid>
            <Grid
              size={{
                xs: 12,
                sm: 8,
                md: 9
              }}>
              <Text color="black">
                {UserService.GetAccountCreatedUTCDate(sessionUser)}
              </Text>
            </Grid>
            <Grid
              sx={{
                pt: 1,
                pb: 1
              }}
              size={12}>
              <Divider />
            </Grid>
            <Grid container sx={{
              justifyContent: "flex-end"
            }}>
              <Button
                disabled={isSaveActionDisabled}
                onClick={() => handleUpdateUserInfo()}
                variant="outlined"
                startIcon={<SaveIcon />}
              >
                {u.t("personal_info_page__button_save")}
              </Button>
            </Grid>
          </Grid>
        </Typography>
      </CardContent>
    </>
  );
};

const mapDispatchToProps = (dispatch: any) => {
  return {
    updateUserInfo: (newUser) => {
      dispatch(updateUserInfo(newUser));
    },
  };
};

const mapStateToProps = (state: any) => {
  return {
    currentUser: state.session.user,
  };
};

export default connect(mapStateToProps, mapDispatchToProps)(EditUserInfo);
