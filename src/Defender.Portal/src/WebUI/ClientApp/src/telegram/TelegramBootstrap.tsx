import { useEffect, useRef, useState } from "react";
import { useNavigate } from "react-router";

import TelegramShell, {
  TelegramFallback,
  TelegramLoading,
} from "./TelegramShell";
import {
  getInitData,
  getTelegramWebApp,
  initializeTelegramWebApp,
  type TelegramWebApp,
} from "./telegramWebApp";
import { login } from "src/actions/sessionActions";
import type { Session } from "src/models/Session";
import { useAppDispatch } from "src/state/hooks";
import { rememberTelegramLaunchData } from "./telegramLaunchContext";

type BootstrapState = "loading" | "ready" | "fallback";

export type TelegramSessionResult =
  | { kind: "authenticated"; session: Session }
  | { kind: "link-required" }
  | { kind: "failed" };

export type TelegramSessionRequester = (initData: string) => Promise<TelegramSessionResult>;

const isTelegramSession = (value: unknown): value is Session =>
  typeof value === "object" &&
  value !== null &&
  "isAuthenticated" in value &&
  (value as Session).isAuthenticated === true &&
  "user" in value &&
  typeof (value as Session).user === "object" &&
  (value as Session).user !== null;

export const createTelegramSession: TelegramSessionRequester = async (initData) => {
  try {
    const response = await fetch("/api/telegram/session", {
      method: "POST",
      credentials: "same-origin",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ initData }),
    });

    if (!response.ok) {
      return response.status === 401 ? { kind: "link-required" } : { kind: "failed" };
    }

    const session = (await response.json()) as unknown;
    return isTelegramSession(session)
      ? { kind: "authenticated", session }
      : { kind: "failed" };
  } catch {
    return { kind: "failed" };
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
  const dispatch = useAppDispatch();
  const navigate = useNavigate();
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

    rememberTelegramLaunchData(initData);
    let active = true;
    void requestSession(initData).then((result) => {
      if (active) {
        if (result.kind === "link-required") {
          navigate("/telegram/link", { replace: true });
          return;
        }

        if (result.kind !== "authenticated") {
          setState("fallback");
          return;
        }

        dispatch(login(result.session));
        setState("ready");
        navigate("/home", { replace: true });
      }
    });

    return () => {
      active = false;
    };
  }, [dispatch, navigate, requestSession, resolvedWebApp]);

  if (state === "loading") {
    return <TelegramLoading />;
  }

  if (state === "fallback") {
    return <TelegramFallback />;
  }

  return (
    <TelegramShell title="Opening Defender">
      <TelegramLoading />
    </TelegramShell>
  );
};

export default TelegramBootstrap;
