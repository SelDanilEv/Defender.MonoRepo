import {
  googleLoginButtonLayout,
  loginFormLayout,
  loginInputAutoComplete,
  passwordVisibilityButtonLayout,
  resetPasswordLinkLayout,
} from "./loginFormLayout";

describe("loginFormLayout", () => {
  test("LoginForm_WhenRendered_UsesAccessibleFullWidthControls", () => {
    expect(loginFormLayout.width).toBe("100%");
    expect(googleLoginButtonLayout.width).toBe("100%");
    expect(googleLoginButtonLayout.minHeight).toBe(48);
    expect(loginInputAutoComplete).toEqual({
      login: "username",
      password: "current-password",
    });
  });

  test("LoginForm_WhenRendered_UsesCompactAccessiblePasswordControls", () => {
    expect(passwordVisibilityButtonLayout).toMatchObject({
      width: 32,
      height: 32,
    });
    expect(resetPasswordLinkLayout).toMatchObject({
      fontSize: "0.75rem",
      minHeight: 32,
    });
    expect(resetPasswordLinkLayout["&:focus-visible"]).toBeDefined();
  });
});
