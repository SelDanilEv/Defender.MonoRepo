import { refreshTelegramSession } from "./telegramSessionRecovery";
import type { TelegramWebApp } from "./telegramWebApp";

describe("Telegram session recovery", () => {
  afterEach(() => {
    delete (window as Window & { Telegram?: unknown }).Telegram;
    vi.unstubAllGlobals();
  });

  test("WhenFreshTelegramLaunchDataIsAvailable_ReissuesCookieSession", async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response("{}", { status: 200 }));
    vi.stubGlobal("fetch", fetchMock);
    setTelegramRuntime(createRuntime("query_id=AAEAA&hash=signed-value"));

    await expect(refreshTelegramSession()).resolves.toBe(true);

    expect(fetchMock).toHaveBeenCalledWith("/api/telegram/session", {
      method: "POST",
      credentials: "same-origin",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ initData: "query_id=AAEAA&hash=signed-value" }),
    });
  });

  test("WhenTelegramLaunchDataIsUnavailable_DoesNotAttemptCookieSessionRecovery", async () => {
    const fetchMock = vi.fn();
    vi.stubGlobal("fetch", fetchMock);

    await expect(refreshTelegramSession()).resolves.toBe(false);

    expect(fetchMock).not.toHaveBeenCalled();
  });
});

const createRuntime = (initData: string): TelegramWebApp => ({
  initData,
  ready: vi.fn(),
  expand: vi.fn(),
  onEvent: vi.fn(),
  offEvent: vi.fn(),
});

const setTelegramRuntime = (runtime: TelegramWebApp): void => {
  (window as Window & { Telegram?: { WebApp?: TelegramWebApp } }).Telegram = {
    WebApp: runtime,
  };
};
