import IUtils from "src/appUtils/interface";
import { Session } from "src/models/Session";
import LoadingStateService from "./LoadingStateService";

export const getSafeReturnUrl = (returnUrl: string | null): string | null => {
  if (returnUrl === "/telegram/link") {
    return returnUrl;
  }

  if (returnUrl === "/oauth/authorize" || returnUrl?.startsWith("/oauth/authorize?")) {
    return returnUrl;
  }

  return null;
};

export const getSafeSsoUrl = (ssoUrl: string | null, origin = window.location.origin): string | null => {
  if (!ssoUrl) {
    return null;
  }

  try {
    const url = new URL(ssoUrl);
    return url.origin === origin ? url.origin : null;
  } catch {
    return null;
  }
};

const AuthorizationService = {
  HandleLoginAttempt: (u: IUtils, session: Session) => {
    if (!session.isAuthenticated) {
      u.e("AuthorizationFailed");
      return;
    }

    if (
      !session.user.isEmailVerified &&
      !session.user.isPhoneVerified &&
      window.location.pathname !== "/welcome/verify-email"
    ) {
      u.react.navigate("/welcome/verification");
      return;
    }

    const ssoUrl = getSafeSsoUrl(u.searchParams.get("SsoUrl"));
    if (ssoUrl) {
      LoadingStateService.StartLoading();

      if (window.opener) {
        const telegramHandoff = u.searchParams.get("TelegramHandoff");
        const message = telegramHandoff
          ? { token: session.token, session, telegramHandoff }
          : { token: session.token };
        window.opener.postMessage(message, ssoUrl);
        window.close();
      } else {
        console.error("No parent window found. Cannot send token.");
      }
    } else {
      const returnUrl = getSafeReturnUrl(u.searchParams.get("returnUrl"));
      if (returnUrl) {
        if (returnUrl === "/telegram/link") {
          u.react.navigate(returnUrl);
          return;
        }

        window.location.assign(returnUrl);
        return;
      }

      u.react.navigate("/home");
    }
  },
};

export default AuthorizationService;
