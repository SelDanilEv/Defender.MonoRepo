import IUtils from "src/appUtils/interface";
import WarningToast from "src/components/Toast/WarningToast";
import stateLoader from "src/state/StateLoader";

export const logoutPortal = (u: IUtils, logoutAction: () => void) => {
  stateLoader.cleanState();
  WarningToast(u.t("SessionExpired"));
  logoutAction();
  u.react.navigate("/");
};
