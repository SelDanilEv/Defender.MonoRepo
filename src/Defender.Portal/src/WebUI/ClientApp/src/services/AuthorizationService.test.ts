import { getSafeReturnUrl, getSafeSsoUrl } from "./AuthorizationService";

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
});
