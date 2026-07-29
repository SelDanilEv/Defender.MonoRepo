export type TelegramSignInHandoff = {
  id: string;
  url: string;
};

type TelegramHandoffMessage = {
  telegramHandoff?: unknown;
  session?: unknown;
};

const createHandoffId = (): string => crypto.randomUUID();

export const createTelegramSignInHandoff = (
  origin: string,
  id = createHandoffId(),
): TelegramSignInHandoff => {
  const parameters = new URLSearchParams({
    SsoUrl: origin,
    TelegramHandoff: id,
  });

  return {
    id,
    url: `${origin}/welcome/login?${parameters.toString()}`,
  };
};

const isSession = (value: unknown): value is Session =>
  typeof value === "object" &&
  value !== null &&
  "isAuthenticated" in value &&
  "token" in value &&
  "user" in value;

export const getHandoffSession = (
  event: Pick<MessageEvent, "origin" | "data">,
  expectedOrigin: string,
  expectedHandoff: string,
): Session | null => {
  if (event.origin !== expectedOrigin || typeof event.data !== "object" || event.data === null) {
    return null;
  }

  const data = event.data as TelegramHandoffMessage;
  return data.telegramHandoff === expectedHandoff && isSession(data.session) ? data.session : null;
};
import type { Session } from "src/models/Session";
