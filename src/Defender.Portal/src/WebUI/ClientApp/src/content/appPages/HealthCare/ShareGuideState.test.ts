import {
  healthCareShareGuideStorageKey,
  shouldShowHealthCareShareGuide,
  dismissHealthCareShareGuide,
} from "./ShareGuideState";

describe("health care share guide state", () => {
  beforeEach(() => {
    localStorage.clear();
    vi.useFakeTimers();
    vi.setSystemTime(new Date("2026-06-22T12:00:00Z"));
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  test("shouldShowHealthCareShareGuide_WhenNeverDismissed_ReturnsTrue", () => {
    expect(shouldShowHealthCareShareGuide()).toBe(true);
  });

  test("shouldShowHealthCareShareGuide_WhenDismissedRecently_ReturnsFalse", () => {
    dismissHealthCareShareGuide();

    expect(shouldShowHealthCareShareGuide()).toBe(false);
  });

  test("shouldShowHealthCareShareGuide_WhenDismissedMoreThanThirtyDaysAgo_ReturnsTrue", () => {
    localStorage.setItem(
      healthCareShareGuideStorageKey,
      new Date("2026-05-22T11:59:59Z").toISOString()
    );

    expect(shouldShowHealthCareShareGuide()).toBe(true);
  });
});
