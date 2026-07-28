import { useEffect, useRef, useState } from "react";
import { useNavigate } from "react-router";
import { Stack, Typography } from "@mui/material";

import TelegramShell, { TelegramFallback, TelegramLinkRequired, TelegramLoading } from "./TelegramShell";
import { createTelegramSession, type TelegramSessionRequester } from "./TelegramBootstrap";
import { clearTelegramLaunchData, getTelegramLaunchData } from "./telegramLaunchContext";
import { login } from "src/actions/sessionActions";
import { useAppDispatch } from "src/state/hooks";
import { openTelegramLink } from "./telegramWebApp";

export type TelegramLinkHandoffRequester = (initData: string) => Promise<string | null>;

export const createTelegramLinkHandoff: TelegramLinkHandoffRequester = async (initData) => {
  try {
    const response = await fetch("/api/telegram/link-handoff", {
      method: "POST",
      credentials: "same-origin",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ initData }),
    });

    if (!response.ok) {
      return null;
    }

    const handoff = (await response.json()) as { loginUrl?: unknown };
    return typeof handoff.loginUrl === "string" ? handoff.loginUrl : null;
  } catch {
    return null;
  }
};

interface TelegramLinkProps {
  requestHandoff?: TelegramLinkHandoffRequester;
  requestSession?: TelegramSessionRequester;
}

const TelegramLink = ({
  requestHandoff = createTelegramLinkHandoff,
  requestSession = createTelegramSession,
}: TelegramLinkProps) => {
  const [state, setState] = useState<"loading" | "fallback" | "link-required" | "waiting" | "failed">("loading");
  const dispatch = useAppDispatch();
  const navigate = useNavigate();
  const pollingStarted = useRef(false);

  const startNativeSignIn = async () => {
    const initData = getTelegramLaunchData();
    if (!initData) {
      setState("fallback");
      return;
    }

    setState("loading");
    const loginUrl = await requestHandoff(initData);
    if (!loginUrl) {
      setState("failed");
      return;
    }

    openTelegramLink(loginUrl);
    setState("waiting");
  };

  useEffect(() => {
    const initData = getTelegramLaunchData();
    if (!initData) {
      setState("fallback");
      return;
    }

    setState("link-required");
  }, []);

  useEffect(() => {
    if (state !== "waiting" || pollingStarted.current) {
      return;
    }

    const initData = getTelegramLaunchData();
    if (!initData) {
      setState("fallback");
      return;
    }

    pollingStarted.current = true;
    let active = true;
    const poll = async () => {
      const result = await requestSession(initData);
      if (!active || result.kind === "link-required") {
        return;
      }

      if (result.kind !== "authenticated") {
        setState("failed");
        return;
      }

      clearTelegramLaunchData();
      dispatch(login(result.session));
      navigate("/home", { replace: true });
    };

    void poll();
    const interval = window.setInterval(() => void poll(), 2000);
    return () => {
      active = false;
      window.clearInterval(interval);
    };
  }, [dispatch, navigate, requestSession, state]);

  if (state === "fallback") {
    return <TelegramFallback />;
  }

  if (state === "link-required") {
    return <TelegramLinkRequired onSignIn={() => void startNativeSignIn()} />;
  }

  if (state === "waiting") {
    return (
      <TelegramShell title="Finish signing in" showNavigation={false}>
        <Stack spacing={1.5} sx={{ py: 4 }}>
          <Typography>Finish signing in in the Portal tab. This app will continue automatically.</Typography>
          <TelegramLoading />
        </Stack>
      </TelegramShell>
    );
  }

  if (state === "failed") {
    return (
      <TelegramShell title="Unable to link Telegram" showNavigation={false}>
        <Stack spacing={1.5} sx={{ py: 4 }}>
          <Typography>Try opening the Defender bot again, then select Open app.</Typography>
        </Stack>
      </TelegramShell>
    );
  }

  return (
    <TelegramShell title="Linking Defender account" showNavigation={false}>
      <TelegramLoading />
    </TelegramShell>
  );
};

export default TelegramLink;
