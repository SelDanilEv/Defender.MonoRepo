import { getInitData } from "./telegramWebApp";

export const refreshTelegramSession = async (): Promise<boolean> => {
  const initData = getInitData();
  if (!initData) {
    return false;
  }

  try {
    const response = await fetch("/api/telegram/session", {
      method: "POST",
      credentials: "same-origin",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ initData }),
    });
    return response.ok;
  } catch {
    return false;
  }
};
