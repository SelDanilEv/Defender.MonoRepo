import { Session } from "src/models/Session";

const isBoolean = (value: unknown): value is boolean => {
  return typeof value === "boolean";
};

export const getProtectedRedirectPath = (
  session?: Partial<Session> | null
): string | null => {
  if (!session?.isAuthenticated) {
    return "/welcome/login";
  }

  if (!session.user) {
    return "/welcome/login";
  }

  const { isEmailVerified, isPhoneVerified } = session.user;
  if (!isBoolean(isEmailVerified) || !isBoolean(isPhoneVerified)) {
    return "/welcome/login";
  }

  if (!isEmailVerified && !isPhoneVerified) {
    return "/welcome/verification";
  }

  return null;
};
