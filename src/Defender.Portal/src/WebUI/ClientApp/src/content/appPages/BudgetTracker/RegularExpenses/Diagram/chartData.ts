import type { Currency } from "src/models/shared/Currency";
import type {
  RegularExpenseChartRow,
  RegularExpenseReview,
  ReviewedRegularExpense,
} from "src/models/budgetTracker/regularExpenses";
import { RegularExpenseType } from "src/models/budgetTracker/regularExpenses";

/** Return monthly amount in source-currency minor units. */
export const calculateMonthlyContribution = (
  expense: ReviewedRegularExpense,
): number => {
  if (expense.type === RegularExpenseType.Annual) {
    return expense.amount / 12;
  }

  if (
    expense.type === RegularExpenseType.Regular ||
    expense.type === RegularExpenseType.Subscription
  ) {
    return expense.amount;
  }

  return 0;
};

/** Convert a source-currency minor amount using rates captured with review. */
export const convertMonthlyContribution = (
  amount: number,
  sourceCurrency: Currency,
  displayCurrency: Currency,
  review: RegularExpenseReview,
): number | null => {
  if (sourceCurrency === displayCurrency) {
    return amount;
  }

  const rates = review.ratesModel?.rates ?? {};
  const baseCurrency = review.ratesModel?.baseCurrency;
  const sourceRate = sourceCurrency === baseCurrency ? 1 : rates[sourceCurrency];
  const displayRate = displayCurrency === baseCurrency ? 1 : rates[displayCurrency];

  if (
    typeof sourceRate !== "number" ||
    typeof displayRate !== "number" ||
    sourceRate <= 0 ||
    displayRate <= 0
  ) {
    return null;
  }

  return (amount / sourceRate) * displayRate;
};

const normalizeMonth = (month: string): string => month.slice(0, 7);

const sumType = (
  review: RegularExpenseReview,
  displayCurrency: Currency,
  type: RegularExpenseType,
): number =>
  review.expenses
    .filter((expense) => expense.type === type)
    .reduce((total, expense) => {
      const converted = convertMonthlyContribution(
        calculateMonthlyContribution(expense),
        expense.currency,
        displayCurrency,
        review,
      );

      return total + (converted ?? 0);
    }, 0) / 100;

/** Build chart rows from saved reviews only. No rows are generated for gaps. */
export const buildRegularExpenseChartDataset = (
  reviews: RegularExpenseReview[],
  displayCurrency: Currency,
): RegularExpenseChartRow[] =>
  [...reviews]
    .sort((left, right) => normalizeMonth(left.month).localeCompare(normalizeMonth(right.month)))
    .map((review) => {
      const regular = sumType(review, displayCurrency, RegularExpenseType.Regular);
      const subscription = sumType(
        review,
        displayCurrency,
        RegularExpenseType.Subscription,
      );
      const annual = sumType(review, displayCurrency, RegularExpenseType.Annual);

      return {
        month: normalizeMonth(review.month),
        regular,
        subscription,
        annual,
        totalComfort: regular + subscription + annual,
      };
    });
