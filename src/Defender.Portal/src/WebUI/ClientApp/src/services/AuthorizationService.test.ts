import { getSafeReturnUrl } from "./AuthorizationService";

describe("AuthorizationService return URL policy", () => {
  test("getSafeReturnUrl_WhenTelegramLinkRouteIsRequested_AllowsExactLocalRoute", () => {
    expect(getSafeReturnUrl("/telegram/link")).toBe("/telegram/link");
  });

  test("getSafeReturnUrl_WhenExternalOrDifferentTelegramRouteIsRequested_RejectsIt", () => {
    expect(getSafeReturnUrl("https://example.test")).toBeNull();
    expect(getSafeReturnUrl("/telegram/link/other")).toBeNull();
  });
});
