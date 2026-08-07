import { expect, test, type Page } from "@playwright/test";

const session = {
  isAuthenticated: true,
  language: "en",
  token: "",
  user: {
    id: "user-1",
    nickname: "Test user",
    email: "test@example.com",
    roles: ["User"],
    isEmailVerified: true,
    isPhoneVerified: false,
    createdDate: new Date(0).toISOString(),
  },
};

const ownerEvents = [
  {
    id: "health-inside",
    type: "Wellbeing",
    startedAt: "2026-06-18T09:00:00.000Z",
    wellbeingScore: 4,
    notes: "custom-range-event",
  },
  {
    id: "health-outside",
    type: "Temperature",
    startedAt: "2026-06-22T09:00:00.000Z",
    temperatureCelsius: 37.2,
    notes: "outside-custom-range",
  },
];

const seedSession = async (page: Page) => {
  await page.addInitScript((value) => {
    localStorage.setItem("defender_apps:state", JSON.stringify(value));
  }, session);
};

const fillDateTimeGroup = async (
  page: Page,
  name: string,
  values: { month: string; day: string; year: string; hours: string; minutes: string; meridiem: string }
) => {
  const group = page.getByRole("group", { name });
  await group.getByRole("spinbutton", { name: "Month" }).fill(values.month);
  await group.getByRole("spinbutton", { name: "Day" }).fill(values.day);
  await group.getByRole("spinbutton", { name: "Year" }).fill(values.year);
  await group.getByRole("spinbutton", { name: "Hours" }).fill(values.hours);
  await group.getByRole("spinbutton", { name: "Minutes" }).fill(values.minutes);
  await group.getByRole("spinbutton", { name: "Meridiem" }).fill(values.meridiem);
  await group.press("Tab");
};

const shareResponse = (overrides: Record<string, unknown> = {}) => ({
  token: "health-share-token",
  publicUrl: "/health-care/share/health-share-token",
  events: ownerEvents,
  from: "2026-06-18T08:30:00.000Z",
  to: "2026-06-19T17:00:00.000Z",
  rangeMode: "Absolute",
  isEnabled: true,
  createdAtUtc: "2026-06-18T08:30:00.000Z",
  ...overrides,
});

const mockHealthCareApi = async (page: Page, currentShare: unknown = null) => {
  let activeShare = currentShare;
  await page.route("**/api/**", async (route) => {
    const request = route.request();
    const url = new URL(request.url());

    if (url.pathname === "/api/home/authorization/check") {
      await route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify(session) });
      return;
    }

    if (url.pathname === "/api/healthCare/events/medication-options") {
      await route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify({ names: [], amounts: [], units: [] }) });
      return;
    }

    if (url.pathname === "/api/healthCare/events" && request.method() === "GET") {
      await route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify(ownerEvents) });
      return;
    }

    if (url.pathname === "/api/healthCare/chart-shares/current") {
      if (!activeShare) {
        await route.fulfill({ status: 404, contentType: "application/problem+json", body: "{}" });
        return;
      }

      await route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify(activeShare) });
      return;
    }

    if (url.pathname === "/api/healthCare/chart-shares" && request.method() === "POST") {
      const payload = JSON.parse(request.postData() ?? "{}");
      activeShare = shareResponse({
        from: payload.from,
        to: payload.to,
        rangeMode: payload.rangeMode,
      });
      await route.fulfill({ status: 201, contentType: "application/json", body: JSON.stringify(activeShare) });
      return;
    }

    if (url.pathname.startsWith("/api/healthCare/public/chart-shares/")) {
      await route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify(shareResponse()) });
      return;
    }

    await route.fulfill({ status: 200, contentType: "application/json", body: "[]" });
  });

  return () => activeShare;
};

test.describe("Health Care date-time ranges", () => {
  test("owner can choose a custom interval and update an existing share", async ({ page }) => {
    await seedSession(page);
    let lastSharePayload: Record<string, unknown> | undefined;
    await mockHealthCareApi(page);
    await page.route("**/api/healthCare/chart-shares", async (route) => {
      if (route.request().method() !== "POST") {
        await route.continue();
        return;
      }

      lastSharePayload = JSON.parse(route.request().postData() ?? "{}");
      await route.fulfill({ status: 201, contentType: "application/json", body: JSON.stringify(shareResponse(lastSharePayload)) });
    });

    await page.goto("/health-care");
    await expect(page.getByText("Health Care", { exact: true }).first()).toBeVisible();

    await page.getByRole("combobox", { name: "Chart period" }).click();
    await page.getByRole("option", { name: "Custom range" }).click();
    await expect(page.getByRole("group", { name: "From" })).toBeVisible();
    await expect(page.getByRole("group", { name: "To" })).toBeVisible();

    await fillDateTimeGroup(page, "From", {
      month: "06",
      day: "18",
      year: "2026",
      hours: "08",
      minutes: "30",
      meridiem: "AM",
    });
    await fillDateTimeGroup(page, "To", {
      month: "06",
      day: "19",
      year: "2026",
      hours: "05",
      minutes: "00",
      meridiem: "PM",
    });

    const shareButton = page.getByRole("button", { name: "Share chart" });
    await expect(shareButton).toBeEnabled();
    await shareButton.click();
    await expect.poll(() => lastSharePayload?.rangeMode).toBe("Absolute");
    expect(lastSharePayload?.from).toBeTruthy();
    expect(lastSharePayload?.to).toBeTruthy();

    await page.getByRole("combobox", { name: "Chart period" }).click();
    await page.getByRole("option", { name: "All time" }).click();
    await page.getByRole("button", { name: "Update shared range" }).click();
    await expect.poll(() => lastSharePayload?.rangeMode).toBe("All");
    expect(lastSharePayload?.from).toBeUndefined();
    expect(lastSharePayload?.to).toBeUndefined();
  });

  test("public absolute share keeps the same picker and never shows events outside server bounds", async ({ page }) => {
    await seedSession(page);
    await mockHealthCareApi(page, shareResponse());

    await page.goto("/health-care/share/health-share-token");
    await expect(page.getByText("Shared Health Care chart", { exact: true })).toBeVisible();
    await expect(page.getByRole("combobox", { name: "Chart period" })).toBeVisible();
    await expect(page.getByRole("group", { name: "From" }).locator('input[aria-hidden="true"]')).toHaveValue("06/18/2026 10:30 AM");
    await expect(page.getByRole("group", { name: "To" }).locator('input[aria-hidden="true"]')).toHaveValue("06/19/2026 07:00 PM");
    await expect(page.getByText("custom-range-event")).toBeVisible();
    await expect(page.getByText("outside-custom-range")).toHaveCount(0);
  });
});
