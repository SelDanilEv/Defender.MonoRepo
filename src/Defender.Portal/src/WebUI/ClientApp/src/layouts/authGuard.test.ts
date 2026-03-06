import { getProtectedRedirectPath } from "src/layouts/authGuard";

describe("getProtectedRedirectPath", () => {
  test("WhenSessionIsMissing_ReturnsLoginRedirect", () => {
    expect(getProtectedRedirectPath(undefined)).toBe("/welcome/login");
    expect(getProtectedRedirectPath(null)).toBe("/welcome/login");
  });

  test("WhenSessionIsNotAuthenticated_ReturnsLoginRedirect", () => {
    const session = {
      isAuthenticated: false,
      user: {
        isEmailVerified: true,
        isPhoneVerified: true,
      },
    };

    expect(getProtectedRedirectPath(session as any)).toBe("/welcome/login");
  });

  test("WhenAuthenticatedButUserMissing_ReturnsLoginRedirect", () => {
    const session = {
      isAuthenticated: true,
    };

    expect(getProtectedRedirectPath(session as any)).toBe("/welcome/login");
  });

  test("WhenAuthenticatedButUserNotVerified_ReturnsVerificationRedirect", () => {
    const session = {
      isAuthenticated: true,
      user: {
        isEmailVerified: false,
        isPhoneVerified: false,
      },
    };

    expect(getProtectedRedirectPath(session as any)).toBe(
      "/welcome/verification"
    );
  });

  test("WhenAuthenticatedAndAtLeastOneContactVerified_ReturnsNoRedirect", () => {
    const emailVerifiedSession = {
      isAuthenticated: true,
      user: {
        isEmailVerified: true,
        isPhoneVerified: false,
      },
    };
    const phoneVerifiedSession = {
      isAuthenticated: true,
      user: {
        isEmailVerified: false,
        isPhoneVerified: true,
      },
    };

    expect(getProtectedRedirectPath(emailVerifiedSession as any)).toBeNull();
    expect(getProtectedRedirectPath(phoneVerifiedSession as any)).toBeNull();
  });

  test("WhenVerificationFlagsAreMissing_ReturnsLoginRedirect", () => {
    const session = {
      isAuthenticated: true,
      user: {},
    };

    expect(getProtectedRedirectPath(session as any)).toBe("/welcome/login");
  });
});
