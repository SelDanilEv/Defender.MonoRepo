import { expect, test, type Page } from "@playwright/test";
import axe from "axe-core";

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
    ["banking", "/banking", "/banking"],
    ["budget editing", "/budget-tracker/positions", "/budget-tracker/positions"],
    ["lottery", "/games/lottery", "/games/lottery"],
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
      await expect(page.locator(`a[href='${journey[2]}']`).first()).toBeVisible();
      await expect(page.getByText("The page hit an unexpected client-side error.", { exact: true })).toHaveCount(0);
      expect(errors).toEqual([]);
      expect(consoleErrors).toEqual([]);
      await expectNoSeriousA11yViolations(page);
    });
  }
});
