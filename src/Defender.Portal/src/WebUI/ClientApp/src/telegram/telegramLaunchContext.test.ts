import {
  clearTelegramLaunchData,
  getTelegramLaunchData,
  rememberTelegramLaunchData,
} from "./telegramLaunchContext";

describe("Telegram launch context", () => {
  afterEach(() => clearTelegramLaunchData());

  test("WhenLaunchDataIsRemembered_KeepsItOnlyInRuntimeMemory", () => {
    rememberTelegramLaunchData("query_id=AAEAA&hash=signed-value");

    expect(getTelegramLaunchData()).toBe("query_id=AAEAA&hash=signed-value");
    expect(localStorage.length).toBe(0);
  });
});
