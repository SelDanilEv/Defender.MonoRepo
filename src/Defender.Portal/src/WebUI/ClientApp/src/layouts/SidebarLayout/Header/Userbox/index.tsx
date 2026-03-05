import {
  Avatar,
  Box,
  Button,
  Divider,
  Hidden,
  lighten,
  List,
  ListItemButton,
  ListItemText,
  Popover,
  Typography,
} from "@mui/material";
import { useEffect, useLayoutEffect, useRef, useState } from "react";
import { connect } from "react-redux";
import { NavLink } from "react-router-dom";
import { styled } from "@mui/material/styles";
import ExpandMoreTwoToneIcon from "@mui/icons-material/ExpandMoreTwoTone";
import AccountBoxTwoToneIcon from "@mui/icons-material/AccountBoxTwoTone";
import LockOpenTwoToneIcon from "@mui/icons-material/LockOpenTwoTone";

import { logout } from "src/actions/sessionActions";
import useUtils from "src/appUtils";
import UserService from "src/services/UserService";

const UserBoxButton = styled(Button)(
  ({ theme }) => `
        padding-left: ${theme.spacing(1)};
        padding-right: ${theme.spacing(1)};
`
);

const MenuUserBox = styled(Box)(
  ({ theme }) => `
        background: ${theme.colors.alpha.black[5]};
        padding: ${theme.spacing(1, 1.25)};
`
);

const UserBoxText = styled(Box)(
  ({ theme }) => `
        text-align: left;
        padding-left: ${theme.spacing(0.75)};
`
);

const UserBoxLabel = styled(Typography)(
  ({ theme }) => `
        font-weight: ${theme.typography.fontWeightBold};
        color: ${theme.palette.secondary.main};
        display: block;
        line-height: 1.2;
`
);

const UserBoxDescription = styled(Typography)(
  ({ theme }) => `
        color: ${lighten(theme.palette.secondary.main, 0.5)};
        line-height: 1.2;
`
);

const HeaderUserbox = (props: any) => {
  let u = useUtils();

  const ref = useRef<any>(null);
  const [isOpen, setOpen] = useState<boolean>(false);

  const [user, setUser] = useState<any>();
  const [roleToDisplay, setRoleToDisplay] = useState<string>();

  const handleOpen = (): void => {
    setOpen(true);
  };

  const handleClose = (): void => {
    setOpen(false);
  };

  useEffect(() => {
    if (props.session.isAuthenticated) {
      const userFromSession = UserService.GetUserInfoFromSession(props.session);

      setUser(userFromSession);
      setRoleToDisplay(UserService.RoleToDisplay(u, userFromSession.Role));
    }
  }, [props.session]);

  const Logout = () => {
    props.logout();
    u.react.navigate("/");
  };

  return (
    <>
      <UserBoxButton color="secondary" ref={ref} onClick={handleOpen}>
        <Avatar variant="rounded" alt={user?.Nickname} src={user?.Avatar} />
        <Hidden mdDown>
          <UserBoxText>
            <UserBoxLabel variant="body1">{user?.Nickname}</UserBoxLabel>
            <UserBoxDescription variant="body2">
              {roleToDisplay}
            </UserBoxDescription>
          </UserBoxText>
        </Hidden>
        <Hidden smDown>
          <ExpandMoreTwoToneIcon sx={{ ml: 1 }} />
        </Hidden>
      </UserBoxButton>
      <Popover
        anchorEl={ref.current}
        onClose={handleClose}
        open={isOpen}
        anchorOrigin={{
          vertical: "top",
          horizontal: "right",
        }}
        transformOrigin={{
          vertical: "top",
          horizontal: "right",
        }}
        PaperProps={{
          sx: {
            minWidth: 194,
          },
        }}
      >
        <MenuUserBox sx={{ minWidth: 194 }} display="flex">
          <Avatar
            variant="rounded"
            alt={user?.Nickname}
            src={user?.Avatar}
            sx={{ width: 28, height: 28, fontSize: "0.85rem" }}
          />
          <UserBoxText>
            <UserBoxLabel variant="body2">{user?.Nickname}</UserBoxLabel>
            <UserBoxDescription variant="caption">
              {roleToDisplay}
            </UserBoxDescription>
          </UserBoxText>
        </MenuUserBox>
        <Divider sx={{ mb: 0 }} />
        <List sx={{ p: 0.5 }} component="nav">
          <ListItemButton
            to="/account/update"
            component={NavLink}
            onClick={handleClose}
            sx={{ px: 1, py: 0.5, minHeight: 34, borderRadius: 1 }}
          >
            <AccountBoxTwoToneIcon fontSize="small" />
            <ListItemText
              primaryTypographyProps={{ ml: "8px", fontSize: "0.95rem", lineHeight: 1.2 }}
              primary={u.t("sidebar_header__menu_profile")}
            />
          </ListItemButton>
          <Divider sx={{ my: 0.25 }} />
          <ListItemButton
            onClick={() => Logout()}
            sx={{ px: 1, py: 0.5, minHeight: 34, borderRadius: 1 }}
          >
            <LockOpenTwoToneIcon fontSize="small" />
            <ListItemText
              primaryTypographyProps={{ ml: "8px", fontSize: "0.95rem", lineHeight: 1.2 }}
              primary={u.t("sidebar_header__menu_logout")}
            />
          </ListItemButton>
        </List>
      </Popover>
    </>
  );
};

const mapStateToProps = (state: any) => {
  return {
    session: state.session,
  };
};

const mapDispatchToProps = (dispatch: any) => {
  return {
    logout: () => {
      dispatch(logout());
    },
  };
};

export default connect(mapStateToProps, mapDispatchToProps)(HeaderUserbox);
