import { render, screen, waitFor } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";

import TelegramBootstrap, { createTelegramSession } from "./TelegramBootstrap";
import type { TelegramWebApp } from "./telegramWebApp";

describe("TelegramBootstrap", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
    vi.restoreAllMocks();
  });

  test("WhenTelegramRuntimeIsMissing_RendersSafeOpenBotFallback", () => {
    renderBootstrap(<TelegramBootstrap webApp={null} />);

    expect(screen.getByRole("heading", { name: "Open Defender in Telegram" })).not.toBeNull();
    expect(screen.getByText("Open the Defender bot, then select Open app.")).not.toBeNull();
  });

  test("WhenTelegramProvidesInitData_PostsRawValueToPortalSessionEndpoint", async () => {
    const requestSession = vi.fn().mockResolvedValue(true);
    const runtime = createRuntime("query_id=AAEAA&hash=signed-value");

    renderBootstrap(
      <TelegramBootstrap webApp={runtime} requestSession={requestSession} />,
    );

    await waitFor(() =>
      expect(requestSession).toHaveBeenCalledWith("query_id=AAEAA&hash=signed-value"),
    );

    expect(screen.getByRole("link", { name: "Home" }).getAttribute("href")).toBe("/home");
  });

  test("createTelegramSession_WhenPortalAcceptsLaunchData_UsesSameOriginCookieRequest", async () => {
    const fetchSpy = vi.fn().mockResolvedValue(new Response(null, { status: 204 }));
    vi.stubGlobal("fetch", fetchSpy);

    await expect(createTelegramSession("query_id=AAEAA&hash=signed-value")).resolves.toBe(true);

    expect(fetchSpy).toHaveBeenCalledWith("/api/telegram/session", {
      method: "POST",
      credentials: "same-origin",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ initData: "query_id=AAEAA&hash=signed-value" }),
    });
  });
});

const renderBootstrap = (element: React.ReactNode) =>
  render(<MemoryRouter>{element}</MemoryRouter>);

const createRuntime = (initData: string): TelegramWebApp => ({
  initData,
  ready: vi.fn(),
  expand: vi.fn(),
  onEvent: vi.fn(),
  offEvent: vi.fn(),
});
