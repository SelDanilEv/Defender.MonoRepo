import { useEffect, useRef, useState } from "react";

import TelegramShell, { TelegramFallback, TelegramLoading } from "./TelegramShell";
import {
  getInitData,
  getTelegramWebApp,
  initializeTelegramWebApp,
  type TelegramWebApp,
} from "./telegramWebApp";

type BootstrapState = "loading" | "ready" | "fallback";

export type TelegramSessionRequester = (initData: string) => Promise<boolean>;

export const createTelegramSession: TelegramSessionRequester = async (initData) => {
  try {
    const response = await fetch("/api/telegram/session", {
      method: "POST",
      credentials: "same-origin",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ initData }),
    });

    return response.ok;
  } catch {
    return false;
  }
};

interface TelegramBootstrapProps {
  webApp?: TelegramWebApp | null;
  requestSession?: TelegramSessionRequester;
}

const TelegramBootstrap = ({
  webApp,
  requestSession = createTelegramSession,
}: TelegramBootstrapProps) => {
  const [state, setState] = useState<BootstrapState>("loading");
  const started = useRef(false);
  const resolvedWebApp = webApp ?? getTelegramWebApp();

  useEffect(() => {
    if (!resolvedWebApp || started.current) {
      if (!resolvedWebApp) {
        setState("fallback");
      }

      return;
    }

    started.current = true;
    initializeTelegramWebApp(resolvedWebApp);
    const initData = getInitData(resolvedWebApp);

    if (!initData) {
      setState("fallback");
      return;
    }

    let active = true;
    void requestSession(initData).then((isAuthenticated) => {
      if (active) {
        setState(isAuthenticated ? "ready" : "fallback");
      }
    });

    return () => {
      active = false;
    };
  }, [requestSession, resolvedWebApp]);

  if (state === "loading") {
    return <TelegramLoading />;
  }

  if (state === "fallback") {
    return <TelegramFallback />;
  }

  return <TelegramShell />;
};

export default TelegramBootstrap;
