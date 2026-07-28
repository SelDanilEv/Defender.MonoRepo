export type TelegramWebAppEvent = "themeChanged" | "viewportChanged";

export interface TelegramWebApp {
  initData: string;
  ready: () => void;
  expand: () => void;
  onEvent: (event: TelegramWebAppEvent, listener: () => void) => void;
  offEvent: (event: TelegramWebAppEvent, listener: () => void) => void;
}

type TelegramWindow = Window & {
  Telegram?: {
    WebApp?: TelegramWebApp;
  };
};

const readyRuntimeInstances = new WeakSet<TelegramWebApp>();

export const getTelegramWebApp = (): TelegramWebApp | null =>
  (window as TelegramWindow).Telegram?.WebApp ?? null;

export const getInitData = (webApp = getTelegramWebApp()): string | null => {
  const initData = webApp?.initData.trim();
  return initData || null;
};

export const isTelegramMiniApp = (): boolean => getInitData() !== null;

export const ready = (webApp = getTelegramWebApp()): void => {
  if (!webApp || readyRuntimeInstances.has(webApp)) {
    return;
  }

  webApp.ready();
  readyRuntimeInstances.add(webApp);
};

export const expand = (webApp = getTelegramWebApp()): void => {
  webApp?.expand();
};

export const initializeTelegramWebApp = (webApp = getTelegramWebApp()): void => {
  ready(webApp);
  expand(webApp);
};

const subscribe = (
  event: TelegramWebAppEvent,
  listener: () => void,
  webApp = getTelegramWebApp(),
): (() => void) => {
  if (!webApp) {
    return () => {};
  }

  webApp.onEvent(event, listener);
  return () => webApp.offEvent(event, listener);
};

export const onThemeChanged = (
  listener: () => void,
  webApp = getTelegramWebApp(),
): (() => void) => subscribe("themeChanged", listener, webApp);

export const onViewportChanged = (
  listener: () => void,
  webApp = getTelegramWebApp(),
): (() => void) => subscribe("viewportChanged", listener, webApp);
