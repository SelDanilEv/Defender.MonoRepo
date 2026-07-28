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

export const getTelegramHandoffCode = (handoff: string | null): string | null =>
  handoff && /^[a-f0-9]{64}$/i.test(handoff) ? handoff.toLowerCase() : null;

export const getTelegramHandoffCodeFromHash = (hash: string): string | null =>
  getTelegramHandoffCode(new URLSearchParams(hash.replace(/^#/, "")).get("TelegramHandoff"));

export const consumeTelegramLinkHandoff = async (code: string): Promise<boolean> => {
  try {
    const response = await fetch("/api/telegram/link-handoff/consume", {
      method: "POST",
      credentials: "same-origin",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ code }),
    });

    return response.ok;
  } catch {
    return false;
  }
};

const AuthorizationService = {
  HandleLoginAttempt: async (u: IUtils, session: Session) => {
    if (!session.isAuthenticated) {
      u.e("AuthorizationFailed");
      return;
    }

    const telegramHandoff = getTelegramHandoffCodeFromHash(window.location.hash);
    if (telegramHandoff && !(await consumeTelegramLinkHandoff(telegramHandoff))) {
      u.e("AuthorizationFailed");
      return;
    }

    if (telegramHandoff) {
      window.history.replaceState(null, "", `${window.location.pathname}${window.location.search}`);
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
        const legacyTelegramHandoff = u.searchParams.get("TelegramHandoff");
        const message = legacyTelegramHandoff
          ? { token: session.token, session, telegramHandoff: legacyTelegramHandoff }
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
