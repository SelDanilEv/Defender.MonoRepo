import type {
  RegularExpenseReview,
  ReviewedRegularExpense,
} from "src/models/budgetTracker/regularExpenses";

import { calculateMonthlyContribution } from "../Diagram/chartData";

export const reviewExpenseMonthlyMajor = (expense: ReviewedRegularExpense): number =>
  calculateMonthlyContribution(expense) / 100;

export const calculateReviewTotalMonthlyMajor = (review: RegularExpenseReview): number =>
  review.expenses.reduce((total, expense) => total + reviewExpenseMonthlyMajor(expense), 0);
