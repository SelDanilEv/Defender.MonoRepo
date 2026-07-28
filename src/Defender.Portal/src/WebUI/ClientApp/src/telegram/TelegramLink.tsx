import { useEffect, useRef, useState } from "react";
import { useNavigate } from "react-router";
import { Stack, Typography } from "@mui/material";

import TelegramShell, { TelegramFallback, TelegramLinkRequired, TelegramLoading } from "./TelegramShell";
import { createTelegramSession, type TelegramSessionRequester } from "./TelegramBootstrap";
import { clearTelegramLaunchData, getTelegramLaunchData } from "./telegramLaunchContext";
import { login } from "src/actions/sessionActions";
import { useAppDispatch, useAppSelector } from "src/state/hooks";

type TelegramAccountLinkResult = "linked" | "unauthorized" | "failed";

export type TelegramAccountLinkRequester = (initData: string) => Promise<TelegramAccountLinkResult>;

export const createTelegramAccountLink: TelegramAccountLinkRequester = async (initData) => {
  try {
    const response = await fetch("/api/telegram/link", {
      method: "POST",
      credentials: "same-origin",
      headers: { "Content-Type": "application/json" },
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
    void requestLink(initData).then(async (linkResult) => {
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
  }, [dispatch, isAuthenticated, navigate, requestLink, requestSession]);

  if (state === "fallback") {
    return <TelegramFallback />;
  }

  if (state === "link-required") {
    return <TelegramLinkRequired />;
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
