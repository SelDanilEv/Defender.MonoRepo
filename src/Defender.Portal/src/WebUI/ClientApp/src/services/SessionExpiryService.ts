import type IUtils from "src/appUtils/interface";
import WarningToast from "src/components/Toast/WarningToast";
import { logoutActionName } from "src/reducers/sessionReducer";
import { cleanWalletInfoActionName } from "src/reducers/walletReducer";
import stateLoader from "src/state/StateLoader";

let handlingSessionExpiry = false;

export const beginSessionExpiryHandling = () => {
  if (handlingSessionExpiry) {
    return false;
  }

  handlingSessionExpiry = true;
  return true;
};

export const resetSessionExpiryHandling = () => {
  handlingSessionExpiry = false;
};

export const expireSession = async (utils?: IUtils | null) => {
  if (!beginSessionExpiryHandling()) return false;

  stateLoader.cleanState();
  const { default: store } = await import("src/state/store");
  store.dispatch({ type: logoutActionName, payload: "" });
  store.dispatch({ type: cleanWalletInfoActionName, payload: "" });
  if (utils) {
    WarningToast(utils.t("SessionExpired"));
    utils.react.navigate("/");
  } else if (typeof window !== "undefined") {
    window.location.assign("/");
  }
  return true;
};
