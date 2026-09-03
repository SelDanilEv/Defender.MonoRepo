import { describe, expect, test } from "vitest";

import { Currency } from "src/models/shared/Currency";
import {
  RegularExpenseType,
  type RegularExpenseReview,
} from "src/models/budgetTracker/regularExpenses";

import { calculateReviewTotalMonthlyMajor } from "./reviewData";

const review: RegularExpenseReview = {
  id: "review-1",
  userId: "user-1",
  month: "2026-09-01",
  expenses: [
    {
      regularExpenseId: "pln-rent",
      name: "Rent",
      type: RegularExpenseType.Regular,
      currency: Currency.PLN,
      amount: 40_000,
      orderPriority: 0,
      monthlyContribution: 40_000,
    },
    {
      regularExpenseId: "usd-subscription",
      name: "Subscription",
      type: RegularExpenseType.Subscription,
      currency: Currency.USD,
      amount: 11_000,
      orderPriority: 1,
      monthlyContribution: 11_000,
    },
    {
      regularExpenseId: "eur-insurance",
      name: "Insurance",
      type: RegularExpenseType.Annual,
      currency: Currency.EUR,
      amount: 12_000,
      orderPriority: 2,
      monthlyContribution: 1_000,
    },
  ],
  ratesModel: {
    date: "2026-09-01",
    baseCurrency: Currency.EUR,
    rates: {
      [Currency.EUR]: 1,
      [Currency.PLN]: 4,
      [Currency.USD]: 1.1,
    },
  },
};

describe("regular expense review data", () => {
  test("Review_WhenExpensesUseMultipleCurrencies_ConvertsMonthlyContributionsToBaseCurrency", () => {
    expect(calculateReviewTotalMonthlyMajor(review)).toBe(210);
  });
});
