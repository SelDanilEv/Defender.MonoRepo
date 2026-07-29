import {
  consumeTelegramLinkHandoff,
  getSafeReturnUrl,
  getSafeSsoUrl,
  getTelegramHandoffCode,
  getTelegramHandoffCodeFromHash,
} from "./AuthorizationService";

describe("AuthorizationService return URL policy", () => {
  test("getSafeReturnUrl_WhenTelegramLinkRouteIsRequested_AllowsExactLocalRoute", () => {
    expect(getSafeReturnUrl("/telegram/link")).toBe("/telegram/link");
  });

  test("getSafeReturnUrl_WhenExternalOrDifferentTelegramRouteIsRequested_RejectsIt", () => {
    expect(getSafeReturnUrl("https://example.test")).toBeNull();
    expect(getSafeReturnUrl("/telegram/link/other")).toBeNull();
  });

  test("getSafeSsoUrl_WhenSameOriginIsRequested_AllowsOnlyThatOrigin", () => {
    expect(getSafeSsoUrl("https://portal.coded-by-danil.dev", "https://portal.coded-by-danil.dev")).toBe("https://portal.coded-by-danil.dev");
    expect(getSafeSsoUrl("https://attacker.test", "https://portal.coded-by-danil.dev")).toBeNull();
  });

  test("getTelegramHandoffCode_WhenCodeIsOpaque64Hex_AllowsOnlyTheExpectedFormat", () => {
    expect(getTelegramHandoffCode("a".repeat(64))).toBe("a".repeat(64));
    expect(getTelegramHandoffCode("handoff-123")).toBeNull();
    expect(getTelegramHandoffCode("https://attacker.test")).toBeNull();
  });

  test("getTelegramHandoffCodeFromHash_WhenLoginUsesAFragment_ReadsOnlyTheOpaqueCode", () => {
    expect(getTelegramHandoffCodeFromHash("#TelegramHandoff=" + "a".repeat(64))).toBe("a".repeat(64));
    expect(getTelegramHandoffCodeFromHash("#other=value")).toBeNull();
  });

  test("consumeTelegramLinkHandoff_WhenPortalSessionIsAuthenticated_UsesSameOriginPost", async () => {
    const fetchSpy = vi.fn().mockResolvedValue(new Response(null, { status: 204 }));
    vi.stubGlobal("fetch", fetchSpy);

    await expect(consumeTelegramLinkHandoff("a".repeat(64))).resolves.toBe(true);

    expect(fetchSpy).toHaveBeenCalledWith("/api/telegram/link-handoff/consume", {
      method: "POST",
      credentials: "same-origin",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ code: "a".repeat(64) }),
    });
  });
});
