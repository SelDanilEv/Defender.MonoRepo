import {
  googleLoginButtonLayout,
  loginFormLayout,
  loginInputAutoComplete,
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
});
