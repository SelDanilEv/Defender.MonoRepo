import { Link, Typography } from "@mui/material";
import { Link as RouterLink } from "react-router-dom";

import useUtils from "src/appUtils";

import AuthPageShell from "../Components/AuthPageShell";
import LineWithText from "../Components/LineWithText";
import LoginForm from "./Form";
import LoginByGoogle from "../Components/LoginByGoogle";

const Login = () => {
  const u = useUtils();

  return (
    <AuthPageShell
      title={u.t("welcome:login_welcome_back")}
      description={u.t("welcome:login_continue_description")}
    >
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
    </AuthPageShell>
  );
};

export default Login;
