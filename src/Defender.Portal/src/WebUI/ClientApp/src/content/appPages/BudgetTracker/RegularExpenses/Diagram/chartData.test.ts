import { describe, expect, test } from "vitest";

import { Currency } from "src/models/shared/Currency";
import {
  RegularExpenseType,
  type RegularExpenseReview,
} from "src/models/budgetTracker/regularExpenses";

import {
  buildRegularExpenseChartDataset,
  calculateMonthlyContribution,
} from "./chartData";

const rates = (overrides: Record<string, number> = {}) => ({
  date: "2026-01-01",
  baseCurrency: Currency.EUR,
  rates: {
    [Currency.EUR]: 1,
    [Currency.PLN]: 4,
    [Currency.USD]: 1.1,
    ...overrides,
  },
});

const review = (
  month: string,
  expenses: RegularExpenseReview["expenses"],
  reviewRates = rates(),
): RegularExpenseReview => ({
  id: month,
  userId: "user-1",
  month,
  expenses,
  ratesModel: reviewRates,
});

describe("regular expense chart data", () => {
  test("AnnualAmount_When12000MinorUnits_Returns1000MinorMonthlyContribution", () => {
    expect(
      calculateMonthlyContribution({
        regularExpenseId: "annual",
        name: "Insurance",
        type: RegularExpenseType.Annual,
        currency: Currency.PLN,
        amount: 12_000,
        orderPriority: 0,
        monthlyContribution: 0,
      }),
    ).toBe(1_000);
  });

  test("RegularAndSubscription_WhenCalculated_KeepFullMinorAmount", () => {
    const base = {
      regularExpenseId: "expense",
      name: "Expense",
      currency: Currency.PLN,
      amount: 2_500,
      orderPriority: 0,
      monthlyContribution: 0,
    };

    expect(
      calculateMonthlyContribution({ ...base, type: RegularExpenseType.Regular }),
    ).toBe(2_500);
    expect(
      calculateMonthlyContribution({ ...base, type: RegularExpenseType.Subscription }),
    ).toBe(2_500);
  });

  test("Chart_WhenDisplayCurrencySelected_UsesCapturedReviewRatesAndMajorUnits", () => {
    const [row] = buildRegularExpenseChartDataset(
      [
        review("2026-01-01", [
          {
            regularExpenseId: "rent",
            name: "Rent",
            type: RegularExpenseType.Regular,
            currency: Currency.PLN,
            amount: 40_000,
            orderPriority: 0,
            monthlyContribution: 40_000,
          },
        ]),
      ],
      Currency.USD,
    );

    expect(row).toMatchObject({
      month: "2026-01",
      regular: 110,
      subscription: 0,
      annual: 0,
      totalComfort: 110,
    });
  });

  test("Chart_WhenMonthsAreMissing_EmitsOnlySavedReviewRows", () => {
    const dataset = buildRegularExpenseChartDataset(
      [
        review("2026-03-01", []),
        review("2026-01-01", []),
      ],
      Currency.EUR,
    );

    expect(dataset.map((row) => row.month)).toEqual(["2026-01", "2026-03"]);
    expect(dataset.some((row) => row.month === "2026-02")).toBe(false);
  });

  test("Chart_WhenTypesAreMixed_TotalEqualsBreakdownSum", () => {
    const [row] = buildRegularExpenseChartDataset(
      [
        review("2026-01-01", [
          {
            regularExpenseId: "regular",
            name: "Rent",
            type: RegularExpenseType.Regular,
            currency: Currency.EUR,
            amount: 3_000,
            orderPriority: 0,
            monthlyContribution: 3_000,
          },
          {
            regularExpenseId: "subscription",
            name: "Cloud",
            type: RegularExpenseType.Subscription,
            currency: Currency.EUR,
            amount: 2_000,
            orderPriority: 0,
            monthlyContribution: 2_000,
          },
          {
            regularExpenseId: "annual",
            name: "Insurance",
            type: RegularExpenseType.Annual,
            currency: Currency.EUR,
            amount: 12_000,
            orderPriority: 0,
            monthlyContribution: 1_000,
          },
        ]),
      ],
      Currency.EUR,
    );

    expect(row.totalComfort).toBe(row.regular + row.subscription + row.annual);
    expect(row.totalComfort).toBe(60);
  });
});
