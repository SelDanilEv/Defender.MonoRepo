import type {
  RegularExpenseReview,
  ReviewedRegularExpense,
} from "src/models/budgetTracker/regularExpenses";

import {
  calculateMonthlyContribution,
  convertMonthlyContribution,
} from "../Diagram/chartData";

export const reviewExpenseMonthlyMajor = (expense: ReviewedRegularExpense): number =>
  calculateMonthlyContribution(expense) / 100;

export const calculateReviewTotalMonthlyMajor = (review: RegularExpenseReview): number =>
  review.expenses.reduce((total, expense) => {
    const converted = convertMonthlyContribution(
      calculateMonthlyContribution(expense),
      expense.currency,
      review.ratesModel.baseCurrency,
      review,
    );

    return total + (converted ?? 0);
  }, 0) / 100;
