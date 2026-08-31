import { expect, test, type Locator, type Page, type Route } from "@playwright/test";

import {
  APP_THEME_STORAGE_KEY,
  DARK_THEME_NAME,
} from "../src/theme/themeMode";

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

const draws = [
  {
    drawNumber: 4,
    publicNames: { en: "Family lottery" },
    startDate: "2099-01-01T00:00:00.000Z",
    endDate: "2099-01-01T12:00:00.000Z",
    coefficients: [3],
    allowedBets: [100],
    allowedCurrencies: ["USD", "EUR", "PLN", "RUB"],
    minBetValue: 100,
    maxBetValue: 300,
    isCustomBetAllowed: false,
    minTicketNumber: 1,
    maxTicketNumber: 100,
    isActive: true,
  },
  {
    drawNumber: 2,
    publicNames: { en: "Good Luck" },
    startDate: "2099-01-01T00:00:00.000Z",
    endDate: "2099-01-02T12:00:00.000Z",
    coefficients: [500],
    allowedBets: [50],
    allowedCurrencies: ["USD", "EUR"],
    minBetValue: 50,
    maxBetValue: 500,
    isCustomBetAllowed: false,
    minTicketNumber: 1,
    maxTicketNumber: 100,
    isActive: true,
  },
];

const tickets = [
  {
    drawNumber: 3,
    ticketNumber: 26,
    amount: 100,
    currency: "PLN",
    userId: "user-1",
    paymentTransactionId: "payment-1",
    prizeTransactionId: "",
    prizePaidAmount: 0,
    status: "Won",
  },
];

const fulfillJson = async (route: Route, body: unknown) => {
  await route.fulfill({
    status: 200,
    contentType: "application/json",
    body: JSON.stringify(body),
  });
};

const mockLotteryApi = async (page: Page) => {
  await page.route("**/api/**", async (route) => {
    const pathname = new URL(route.request().url()).pathname;

    if (pathname.endsWith("/api/home/authorization/check")) {
      await fulfillJson(route, session);
      return;
    }

    if (pathname.endsWith("/api/banking/wallet/info")) {
      await fulfillJson(route, {
        walletNumber: 1,
        defaultCurrency: "USD",
        currencyAccounts: [],
      });
      return;
    }

    if (pathname.endsWith("/api/lottery/draw/active")) {
      await fulfillJson(route, {
        items: draws,
        totalItemsCount: draws.length,
        currentPage: 0,
        pageSize: 1000,
        totalPagesCount: 1,
      });
      return;
    }

    if (pathname.endsWith("/api/lottery/tickets")) {
      await fulfillJson(route, {
        items: tickets,
        totalItemsCount: tickets.length,
        currentPage: 0,
        pageSize: 10,
        totalPagesCount: 1,
      });
      return;
    }

    await fulfillJson(route, []);
  });
};

const seedSessionAndTheme = async (page: Page) => {
  await page.addInitScript(
    ({ sessionValue, themeStorageKey, themeName }) => {
      localStorage.setItem("defender_apps:state", JSON.stringify(sessionValue));
      localStorage.setItem(themeStorageKey, themeName);
    },
    {
      sessionValue: session,
      themeStorageKey: APP_THEME_STORAGE_KEY,
      themeName: DARK_THEME_NAME,
    },
  );
};

const getRects = async (locator: Locator) =>
  locator.evaluateAll((elements) =>
    elements.map((element) => {
      const rect = element.getBoundingClientRect();
      return {
        x: Math.round(rect.x),
        y: Math.round(rect.y),
        width: Math.round(rect.width),
        height: Math.round(rect.height),
        right: Math.round(rect.right),
      };
    }),
  );

test("lottery arena keeps cards, currencies, and latest ticket text readable", async ({ page }) => {
  await page.setViewportSize({ width: 1440, height: 1100 });
  await seedSessionAndTheme(page);
  await mockLotteryApi(page);
  await page.goto("/games/lottery");

  await expect(page.getByText("Family lottery", { exact: true })).toBeVisible();

  const drawCards = page.getByTestId("lottery-active-draw-card");
  await expect(drawCards).toHaveCount(2);

  const cardRects = await getRects(drawCards);
  expect(cardRects[1].x).toBeGreaterThan(cardRects[0].x);
  expect(cardRects[1].y).toBe(cardRects[0].y);
  expect(Math.abs(cardRects[1].width - cardRects[0].width)).toBeLessThanOrEqual(2);
  expect(Math.abs(cardRects[1].height - cardRects[0].height)).toBeLessThanOrEqual(2);

  const currencyRects = await getRects(drawCards.first().getByTestId("lottery-currency-option"));
  expect(currencyRects).toHaveLength(4);
  expect(new Set(currencyRects.map((rect) => rect.x)).size).toBe(2);
  expect(new Set(currencyRects.map((rect) => rect.y)).size).toBe(2);

  const headingIcon = await getRects(page.getByTestId("lottery-latest-tickets-heading-icon"));
  const heading = await getRects(page.getByText("Latest tickets", { exact: true }));
  expect(headingIcon[0].right).toBeLessThanOrEqual(heading[0].x);

  const statusColor = await page
    .getByTestId("lottery-ticket-status")
    .evaluate((element) => getComputedStyle(element).color);
  expect(statusColor).toBe("rgb(17, 22, 51)");
});
