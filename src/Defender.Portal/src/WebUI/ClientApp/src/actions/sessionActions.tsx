import type { AppDispatch } from "src/state/store";
import type { Session } from "src/models/Session";
import { UserAccountInfo } from "src/models/UserAccountInfo";
import apiUrls from "src/api/apiUrls";
import {
  loginActionName,
  logoutActionName,
  updateLanguageActionName,
  updateUserInfoActionName,
} from "src/reducers/sessionReducer";
import { cleanWalletInfoActionName } from "src/reducers/walletReducer";

export function login(session: Session) {
  if (!session.isAuthenticated) {
    return;
  }

  return (dispatch: AppDispatch) => {
    dispatch({
      type: loginActionName,
      payload: session,
    });
  };
}

export function logout() {
  return async (dispatch: AppDispatch) => {
    try {
      await fetch(apiUrls.authorization.logout, {
        method: "POST",
        credentials: "same-origin",
      });
    } catch {}

    dispatch({
      type: logoutActionName,
      payload: "",
    });
    dispatch({
      type: cleanWalletInfoActionName,
      payload: "",
    });
  };
}

export function updateLanguage(language: string) {
  return (dispatch: AppDispatch) => {
    dispatch({
      type: updateLanguageActionName,
      payload: language,
    });
  };
}

export function updateUserInfo(updatedUser: UserAccountInfo) {
  return (dispatch: AppDispatch) => {
    dispatch({
      type: updateUserInfoActionName,
      payload: updatedUser,
    });
  };
}
