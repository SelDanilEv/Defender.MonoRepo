import useUtils from "src/appUtils";

import { Link, Typography } from "@mui/material";
import { Link as RouterLink } from "react-router-dom";

import AuthPageShell from "../Components/AuthPageShell";
import ResetPasswordForm from "./Form";

const ResetPassword = () => {
  const u = useUtils();

  return (
    <AuthPageShell
      title={u.t("welcome:reset_password_title")}
      description={u.t("welcome:reset_password_description")}
    >
      <ResetPasswordForm />
      <Typography
        sx={{
          textAlign: "center",
          mt: 3
        }}>
        <Link component={RouterLink} to="/welcome/login" sx={{
          fontWeight: 700
        }}>
          {u.t("welcome:back_button")}
        </Link>
      </Typography>
    </AuthPageShell>
  );
};

export default ResetPassword;
