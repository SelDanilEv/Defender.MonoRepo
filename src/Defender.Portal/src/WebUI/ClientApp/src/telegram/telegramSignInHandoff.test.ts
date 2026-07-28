import {
  createTelegramSignInHandoff,
  getHandoffSession,
} from "./telegramSignInHandoff";

describe("Telegram top-level Google sign-in handoff", () => {
  test("createTelegramSignInHandoff_UsesSameOriginLoginAndOpaqueHandoff", () => {
    const handoff = createTelegramSignInHandoff("https://portal.coded-by-danil.dev", "handoff-123");

    expect(handoff.id).toBe("handoff-123");
    expect(handoff.url).toBe(
      "https://portal.coded-by-danil.dev/welcome/login?SsoUrl=https%3A%2F%2Fportal.coded-by-danil.dev&TelegramHandoff=handoff-123",
    );
  });

  test("getHandoffSession_RejectsMessageFromAnotherOriginOrHandoff", () => {
    const expectedOrigin = "https://portal.coded-by-danil.dev";
    const session = {
      isAuthenticated: true,
      token: "portal-token",
      language: "en",
      user: { id: "user-1" },
    };

    expect(getHandoffSession({ origin: "https://attacker.test", data: { telegramHandoff: "handoff-123", session } }, expectedOrigin, "handoff-123")).toBeNull();
    expect(getHandoffSession({ origin: expectedOrigin, data: { telegramHandoff: "other", session } }, expectedOrigin, "handoff-123")).toBeNull();
    expect(getHandoffSession({ origin: expectedOrigin, data: { telegramHandoff: "handoff-123", session } }, expectedOrigin, "handoff-123")).toEqual(session);
  });
});
