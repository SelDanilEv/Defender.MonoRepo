import { shouldShowHeaderNewSessionButton } from "./index";

describe("shouldShowHeaderNewSessionButton", () => {
  test("WhenLoadingAndNoSessions_ReturnsTrue", () => {
    expect(shouldShowHeaderNewSessionButton(true, 0)).toBe(true);
  });

  test("WhenLoadedAndNoSessions_ReturnsFalse", () => {
    expect(shouldShowHeaderNewSessionButton(false, 0)).toBe(false);
  });

  test("WhenLoadedAndHasSessions_ReturnsTrue", () => {
    expect(shouldShowHeaderNewSessionButton(false, 1)).toBe(true);
  });
});
