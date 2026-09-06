import type {
  RegularExpenseReview,
  ReviewedRegularExpense,
} from "src/models/budgetTracker/regularExpenses";
import type { Currency } from "src/models/shared/Currency";
import { BudgetTrackerSupportedCurrencies } from "src/consts/SupportedCurrencies";

import {
  calculateMonthlyContribution,
  convertMonthlyContribution,
} from "../Diagram/chartData";

export const reviewExpenseMonthlyMajor = (expense: ReviewedRegularExpense): number =>
  calculateMonthlyContribution(expense) / 100;

export const resolveReviewDisplayCurrency = (
  review: RegularExpenseReview,
  displayCurrency?: Currency | string | null,
): Currency =>
  typeof displayCurrency === "string" &&
  BudgetTrackerSupportedCurrencies.includes(displayCurrency)
    ? (displayCurrency as Currency)
    : review.ratesModel.baseCurrency;

export const calculateReviewTotalMonthlyMajor = (
  review: RegularExpenseReview,
  displayCurrency?: Currency | string | null,
): number => {
  const targetCurrency = resolveReviewDisplayCurrency(review, displayCurrency);
  const totalMinor = review.expenses.reduce((total, expense) => {
    const converted = convertMonthlyContribution(
      calculateMonthlyContribution(expense),
      expense.currency,
      targetCurrency,
      review,
    );

    return total + (converted ?? 0);
  }, 0);

  return Math.round(totalMinor) / 100;
};
