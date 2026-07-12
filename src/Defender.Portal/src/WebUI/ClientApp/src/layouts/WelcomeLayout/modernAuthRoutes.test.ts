import { modernAuthRoutes } from "./modernAuthRoutes";

describe("modernAuthRoutes", () => {
  test("ModernAuthRoutes_ContainsEveryWelcomeAuthenticationFlow", () => {
    expect(modernAuthRoutes).toEqual([
      "/welcome/login",
      "/welcome/create",
      "/welcome/password/reset",
      "/welcome/verification",
      "/welcome/verify-email",
    ]);
  });
});
