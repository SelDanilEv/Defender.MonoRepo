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

  test("Review_WhenDisplayCurrencySelected_RoundsFractionalConvertedTotalToCents", () => {
    const fractionalReview: RegularExpenseReview = {
      ...review,
      expenses: [
        {
          ...review.expenses[0],
          amount: 1_000,
          monthlyContribution: 1_000,
        },
      ],
      ratesModel: {
        ...review.ratesModel,
        rates: {
          ...review.ratesModel.rates,
          [Currency.PLN]: 3,
          [Currency.USD]: 1.1,
        },
      },
    };
    expect(calculateReviewTotalMonthlyMajor(fractionalReview, Currency.USD)).toBe(3.67);
  });

  test("Review_WhenMonthlyContributionHasFractionalMinorUnits_RoundsTotalToCents", () => {
    const annualReview: RegularExpenseReview = {
      ...review,
      expenses: [{
        ...review.expenses[0],
        type: RegularExpenseType.Annual,
        amount: 1_000,
        monthlyContribution: 1_000 / 12,
      }],
      ratesModel: {
        ...review.ratesModel,
        baseCurrency: Currency.PLN,
        rates: { [Currency.PLN]: 1 },
      },
    };

    expect(calculateReviewTotalMonthlyMajor(annualReview)).toBe(0.83);
  });

  test("Review_WhenDisplayCurrencyMissing_UsesReviewBaseCurrency", () => {
    const fractionalReview: RegularExpenseReview = {
      ...review,
      expenses: [
        {
          ...review.expenses[0],
          amount: 1_000,
          monthlyContribution: 1_000,
        },
      ],
      ratesModel: {
        ...review.ratesModel,
        rates: {
          ...review.ratesModel.rates,
          [Currency.PLN]: 3,
        },
      },
    };

    expect(calculateReviewTotalMonthlyMajor(fractionalReview)).toBe(3.33);
  });

  test("Review_WhenDisplayCurrencyUnknown_UsesReviewBaseCurrency", () => {
    expect(calculateReviewTotalMonthlyMajor(review, "GBP")).toBe(210);
  });
});
