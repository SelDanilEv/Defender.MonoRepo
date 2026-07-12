import { Link, Typography } from "@mui/material";
import { Link as RouterLink } from "react-router-dom";

import useUtils from "src/appUtils";

import LineWithText from "../Components/LineWithText";
import AuthPageShell from "../Components/AuthPageShell";
import CreateForm from "./Form";
import LoginByGoogle from "../Components/LoginByGoogle";

const CreateAccount = () => {
  const u = useUtils();

  return (
    <AuthPageShell
      title={u.t("welcome:create_account_title")}
      description={u.t("welcome:create_account_description")}
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
      <CreateForm />
      <Typography
        sx={{
          color: "text.secondary",
          textAlign: "center",
          mt: 3
        }}>
        {u.t("welcome:already_have_account")} {" "}
        <Link component={RouterLink} to="/welcome/login" sx={{
          fontWeight: 700
        }}>
          {u.t("welcome:sign_in")}
        </Link>
      </Typography>
    </AuthPageShell>
  );
};

export default CreateAccount;
