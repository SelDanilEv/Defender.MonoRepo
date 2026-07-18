import { expect, test, type Page } from "@playwright/test";
import axe from "axe-core";

import {
  APP_THEME_STORAGE_KEY,
  DARK_THEME_NAME,
  LIGHT_THEME_NAME,
} from "../src/theme/themeMode";

const session = { isAuthenticated: true, language: "en", token: "", user: {
  id: "user-1", nickname: "Test user", email: "test@example.com", roles: ["User"],
  isEmailVerified: true, isPhoneVerified: false, createdDate: new Date(0).toISOString(),
} };

const mockApi = async (page: Page, authenticated = false) => {
  await page.route("**/api/**", async (route) => {
    const url = route.request().url();
    if (url.includes("/api/home/authorization/check")) {
      await route.fulfill({
        status: authenticated ? 200 : 401,
        contentType: "application/problem+json",
        body: authenticated
          ? JSON.stringify(session)
          : JSON.stringify({ title: "Unauthorized", detail: "Session expired" }),
      });
      return;
    }
    if (url.includes("/api/banking/wallet/info")) {
      await route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify({ walletNumber: 1, defaultCurrency: "USD", currencyAccounts: [] }) });
      return;
    }
    if (url.includes("/api/budgetTracker/positions") || url.includes("/api/lottery/draw/active")) {
      await route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify({ items: [], totalItemsCount: 0 }) });
      return;
    }
    await route.fulfill({ status: 200, contentType: "application/json", body: "[]" });
  });
};

const seedSession = async (page: Page) => {
  await page.addInitScript((value) => localStorage.setItem("defender_apps:state", JSON.stringify(value)), session);
};

const setTheme = async (page: Page, themeName: string) => {
  await page.addInitScript(
    ([storageKey, value]) => localStorage.setItem(storageKey, value),
    [APP_THEME_STORAGE_KEY, themeName]
  );
};

const expectNoSeriousA11yViolations = async (page: Page) => {
  await page.addScriptTag({ content: axe.source });
  const violations = await page.evaluate(async () => {
    const result = await (window as any).axe.run(document, { runOnly: { type: "tag", values: ["wcag2a", "wcag2aa"] } });
    return result.violations.filter((item: any) => item.impact === "serious" || item.impact === "critical");
  });
  expect(violations, JSON.stringify(violations, null, 2)).toEqual([]);
};

const expectBasicAccessibility = async (page: Page) => {
  await expect(page.locator("main, [role=main], h1, h2, h3").first()).toBeVisible();
  const unnamedButtons = page.locator("button:not([aria-label])").filter({ hasText: /^\s*$/ });
  await expect(unnamedButtons).toHaveCount(0);
};

test("welcome login exposes credential and Google login shells", async ({ page }) => {
  await mockApi(page);
  await page.goto("/welcome/login");
  await expect(page.getByRole("textbox").first()).toBeVisible();
  await expect(page.getByRole("button", { name: /google/i })).toBeVisible();
  await expectBasicAccessibility(page);
  await expectNoSeriousA11yViolations(page);
});

for (const themeName of [DARK_THEME_NAME, LIGHT_THEME_NAME]) {
  test.describe(`${themeName} welcome accessibility`, () => {
    test.beforeEach(async ({ page }) => {
      await setTheme(page, themeName);
      await mockApi(page);
    });

    for (const route of [
      "/welcome/login",
      "/welcome/create",
      "/welcome/password/reset",
      "/welcome/verification",
    ]) {
      test(`${route} passes Section 508 checks`, async ({ page }) => {
        await page.goto(route);
        await expect(page.locator("h1, h2, h3").first()).toBeVisible();
        await expectNoSeriousA11yViolations(page);
      });
    }

    test("login keeps password visibility control and reset link keyboard reachable", async ({ page }) => {
      await page.goto("/welcome/login");
      await page.getByRole("textbox", { name: "Login" }).focus();
      await page.keyboard.press("Tab");
      await expect(page.getByRole("textbox", { name: "Password" })).toBeFocused();
      await page.keyboard.press("Tab");
      await expect(page.getByRole("button", { name: /show password/i })).toBeFocused();
      await page.keyboard.press("Tab");
      await expect(page.getByRole("link", { name: /forgot password/i })).toBeFocused();
    });
  });
}

test("expired session cannot remain on protected banking journey", async ({ page }) => {
  await mockApi(page);
  await page.goto("/banking");
  await expect(page).toHaveURL(/\/welcome\/login|\/$/);
});

test.describe("authenticated journey shells", () => {
  test.beforeEach(async ({ page }) => {
    await seedSession(page);
    await mockApi(page, true);
  });

  for (const journey of [
    ["home", "/home", "/home"],
    ["banking", "/banking", "/banking"],
    ["budget diagram", "/budget-tracker/diagram", "/budget-tracker/diagram"],
    ["budget positions", "/budget-tracker/positions", "/budget-tracker/positions"],
    ["budget reviews", "/budget-tracker/reviews", "/budget-tracker/reviews"],
    ["lottery", "/games/lottery", "/games/lottery"],
    ["lottery tickets", "/games/lottery/tickets", "/games/lottery/tickets"],
    ["health care", "/health-care", "/health-care"],
    ["travel calendar", "/travel-calendar", "/travel-calendar"],
    ["configuration", "/configuration", "/configuration"],
    ["account update", "/account/update", "/account/update"],
    ["food advisor", "/food-advisor", "/food-advisor"],
    ["food advisor new session", "/food-advisor/session/new", "/food-advisor/session/new"],
    ["food advisor sessions", "/food-advisor/sessions", "/food-advisor/sessions"],
  ] as const) {
    test(`${journey[0]} shell loads without uncaught page error`, async ({ page }) => {
      const errors: Error[] = [];
      const consoleErrors: string[] = [];
      page.on("pageerror", (error) => errors.push(error));
      page.on("console", (message) => {
        if (message.type() === "error") consoleErrors.push(message.text());
      });
      await page.goto(journey[1]);
      await expect(page).toHaveURL(new RegExp(`${journey[1]}$`));
      await expect(page.locator("#root")).toBeVisible();
      await expect(page.getByText("The page hit an unexpected client-side error.", { exact: true })).toHaveCount(0);
      expect(errors).toEqual([]);
      expect(consoleErrors).toEqual([]);
      await expectNoSeriousA11yViolations(page);
    });
  }
});
