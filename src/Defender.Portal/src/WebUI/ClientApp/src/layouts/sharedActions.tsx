import IUtils from "src/appUtils/interface";
import WarningToast from "src/components/Toast/WarningToast";
import stateLoader from "src/state/StateLoader";
import { beginSessionExpiryHandling } from "src/services/SessionExpiryService";

export const logoutPortal = (u: IUtils, logoutAction: () => void) => {
  if (!beginSessionExpiryHandling()) {
    return;
  }

  stateLoader.cleanState();
  WarningToast(u.t("SessionExpired"));
  logoutAction();
  u.react.navigate("/");
};
