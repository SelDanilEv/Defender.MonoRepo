import {
  getInitData,
  getTelegramWebApp,
  initializeTelegramWebApp,
  isTelegramMiniApp,
  type TelegramWebApp,
} from "./telegramWebApp";

describe("Telegram Web App runtime", () => {
  afterEach(() => {
    delete (window as Window & { Telegram?: unknown }).Telegram;
    vi.restoreAllMocks();
  });

  test("getTelegramWebApp_WhenRuntimeIsMissing_ReturnsNull", () => {
    expect(getTelegramWebApp()).toBeNull();
    expect(isTelegramMiniApp()).toBe(false);
  });

  test("isTelegramMiniApp_WhenTelegramRuntimeIsPresent_ReturnsTrue", () => {
    setTelegramRuntime(createRuntime("query_id=AAEAA&hash=signed-value"));

    expect(isTelegramMiniApp()).toBe(true);
  });

  test("isTelegramMiniApp_WhenBridgeHasNoLaunchData_ReturnsFalse", () => {
    setTelegramRuntime(createRuntime(""));

    expect(isTelegramMiniApp()).toBe(false);
  });

  test("getInitData_WhenTelegramProvidesSignedLaunchData_ReturnsRawValueForServerHandoff", () => {
    const runtime = createRuntime("query_id=AAEAA&hash=signed-value");
    setTelegramRuntime(runtime);

    expect(getInitData()).toBe("query_id=AAEAA&hash=signed-value");
  });

  test("initializeTelegramWebApp_WhenCalledMoreThanOnce_CallsReadyOnlyOnce", () => {
    const runtime = createRuntime("query_id=AAEAA&hash=signed-value");

    initializeTelegramWebApp(runtime);
    initializeTelegramWebApp(runtime);

    expect(runtime.ready).toHaveBeenCalledTimes(1);
    expect(runtime.expand).toHaveBeenCalledTimes(2);
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
