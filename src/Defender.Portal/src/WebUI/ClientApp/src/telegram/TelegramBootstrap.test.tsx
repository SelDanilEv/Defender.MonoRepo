import { render, screen, waitFor } from "@testing-library/react";
import { Provider } from "react-redux";
import { MemoryRouter } from "react-router";

import TelegramBootstrap, { createTelegramSession } from "./TelegramBootstrap";
import type { TelegramWebApp } from "./telegramWebApp";
import type { Session } from "src/models/Session";
import { logoutActionName } from "src/reducers/sessionReducer";
import store from "src/state/store";

const telegramSession: Session = {
  isAuthenticated: true,
  language: "en",
  token: "",
  user: {
    id: "user-1",
    nickname: "Danil",
    email: "danil@example.com",
    phone: "+48123123123",
    isEmailVerified: true,
    isPhoneVerified: true,
    isBlocked: false,
    roles: ["User"],
    createdDate: new Date("2026-07-28T00:00:00Z"),
  },
};

const authenticatedResult = { kind: "authenticated" as const, session: telegramSession };

describe("TelegramBootstrap", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
    vi.restoreAllMocks();
    store.dispatch({ type: logoutActionName });
  });

  test("WhenTelegramRuntimeIsMissing_RendersSafeOpenBotFallback", () => {
    renderBootstrap(<TelegramBootstrap webApp={null} />);

    expect(screen.getByRole("heading", { name: "Open Defender in Telegram" })).not.toBeNull();
    expect(screen.getByText("Open the Defender bot, then select Open app.")).not.toBeNull();
  });

  test("WhenTelegramProvidesInitData_PostsRawValueToPortalSessionEndpoint", async () => {
    const requestSession = vi.fn().mockResolvedValue(authenticatedResult);
    const runtime = createRuntime("query_id=AAEAA&hash=signed-value");

    renderBootstrap(
      <TelegramBootstrap webApp={runtime} requestSession={requestSession} />,
    );

    await waitFor(() =>
      expect(requestSession).toHaveBeenCalledWith("query_id=AAEAA&hash=signed-value"),
    );

    await waitFor(() => expect(requestSession).toHaveBeenCalledTimes(1));
    await waitFor(() => expect(store.getState().session.user.id).toBe("user-1"));
  });

  test("createTelegramSession_WhenPortalAcceptsLaunchData_ReturnsSessionForMemoryOnlyHydration", async () => {
    const fetchSpy = vi.fn().mockResolvedValue(new Response(JSON.stringify(telegramSession), { status: 200 }));
    vi.stubGlobal("fetch", fetchSpy);

    await expect(createTelegramSession("query_id=AAEAA&hash=signed-value")).resolves.toMatchObject({
      kind: "authenticated",
      session: {
        ...telegramSession,
        user: {
          ...telegramSession.user,
          createdDate: "2026-07-28T00:00:00.000Z",
        },
      },
    });

    expect(fetchSpy).toHaveBeenCalledWith("/api/telegram/session", {
      method: "POST",
      credentials: "same-origin",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ initData: "query_id=AAEAA&hash=signed-value" }),
    });
  });

  test("WhenTelegramSessionNeedsAccountLink_ShowsSafeSignInRoute", async () => {
    const requestSession = vi.fn().mockResolvedValue({ kind: "link-required" as const });
    const runtime = createRuntime("query_id=AAEAA&hash=signed-value");

    renderBootstrap(<TelegramBootstrap webApp={runtime} requestSession={requestSession} />);

    await waitFor(() => expect(requestSession).toHaveBeenCalledTimes(1));
    expect(screen.getByRole("link", { name: "Sign in to link" }).getAttribute("href"))
      .toBe("/welcome/login?returnUrl=%2Ftelegram%2Flink");
  });
});

const renderBootstrap = (element: React.ReactNode) =>
  render(
    <Provider store={store}>
      <MemoryRouter>{element}</MemoryRouter>
    </Provider>,
  );

const createRuntime = (initData: string): TelegramWebApp => ({
  initData,
  ready: vi.fn(),
  expand: vi.fn(),
  onEvent: vi.fn(),
  offEvent: vi.fn(),
});
