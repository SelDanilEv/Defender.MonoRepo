import { useEffect, useRef, useState } from "react";
import { useNavigate } from "react-router";
import { Stack, Typography } from "@mui/material";

import TelegramShell, { TelegramFallback, TelegramLinkRequired, TelegramLoading } from "./TelegramShell";
import { createTelegramSession, type TelegramSessionRequester } from "./TelegramBootstrap";
import { clearTelegramLaunchData, getTelegramLaunchData } from "./telegramLaunchContext";
import { login } from "src/actions/sessionActions";
import { useAppDispatch, useAppSelector } from "src/state/hooks";
import type { Session } from "src/models/Session";
import { createTelegramSignInHandoff, getHandoffSession } from "./telegramSignInHandoff";

type TelegramAccountLinkResult = "linked" | "unauthorized" | "failed";

export type TelegramAccountLinkRequester = (initData: string, token: string) => Promise<TelegramAccountLinkResult>;

export const createTelegramAccountLink: TelegramAccountLinkRequester = async (initData, token) => {
  try {
    const response = await fetch("/api/telegram/link", {
      method: "POST",
      credentials: "same-origin",
      headers: { "Content-Type": "application/json", Authorization: `Bearer ${token}` },
      body: JSON.stringify({ initData }),
    });

    if (response.ok) {
      return "linked";
    }

    return response.status === 401 ? "unauthorized" : "failed";
  } catch {
    return "failed";
  }
};

interface TelegramLinkProps {
  requestLink?: TelegramAccountLinkRequester;
  requestSession?: TelegramSessionRequester;
}

const TelegramLink = ({
  requestLink = createTelegramAccountLink,
  requestSession = createTelegramSession,
}: TelegramLinkProps) => {
  const [state, setState] = useState<"loading" | "fallback" | "link-required" | "failed">("loading");
  const started = useRef(false);
  const isAuthenticated = useAppSelector((store) => store.session.isAuthenticated);
  const dispatch = useAppDispatch();
  const navigate = useNavigate();
  const session = useAppSelector((store) => store.session as Session);
  const handoffId = useRef<string | null>(null);

  const startTopLevelSignIn = () => {
    const handoff = createTelegramSignInHandoff(window.location.origin);
    handoffId.current = handoff.id;
    window.open(handoff.url, "_blank");
  };

  useEffect(() => {
    const receiveSession = (event: MessageEvent) => {
      if (!handoffId.current) {
        return;
      }

      const receivedSession = getHandoffSession(event, window.location.origin, handoffId.current);
      if (receivedSession) {
        handoffId.current = null;
        dispatch(login(receivedSession));
      }
    };

    window.addEventListener("message", receiveSession);
    return () => window.removeEventListener("message", receiveSession);
  }, [dispatch]);

  useEffect(() => {
    const initData = getTelegramLaunchData();
    if (!initData) {
      setState("fallback");
      return;
    }

    if (!isAuthenticated) {
      setState("link-required");
      return;
    }

    if (started.current) {
      return;
    }

    started.current = true;
    let active = true;
    void requestLink(initData, session.token).then(async (linkResult) => {
      if (!active) {
        return;
      }

      if (linkResult === "unauthorized") {
        setState("link-required");
        return;
      }

      if (linkResult !== "linked") {
        setState("failed");
        return;
      }

      const sessionResult = await requestSession(initData);
      if (!active) {
        return;
      }

      if (sessionResult.kind !== "authenticated") {
        setState("failed");
        return;
      }

      clearTelegramLaunchData();
      dispatch(login(sessionResult.session));
      navigate("/home", { replace: true });
    });

    return () => {
      active = false;
    };
  }, [dispatch, isAuthenticated, navigate, requestLink, requestSession, session.token]);

  if (state === "fallback") {
    return <TelegramFallback />;
  }

  if (state === "link-required") {
    return <TelegramLinkRequired onSignIn={startTopLevelSignIn} />;
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
