import { Currency } from "src/models/shared/Currency";

import { RegularExpenseType } from "./RegularExpenseType";

export interface RegularExpenseRatesModel {
  date: string;
  baseCurrency: Currency;
  rates: Partial<Record<Currency, number>>;
}

export interface ReviewedRegularExpense {
  regularExpenseId: string;
  name: string;
  type: RegularExpenseType;
  currency: Currency;
  amount: number;
  orderPriority: number;
  monthlyContribution: number;
}

export interface RegularExpenseReview {
  id: string;
  userId?: string;
  month: string;
  expenses: ReviewedRegularExpense[];
  ratesModel: RegularExpenseRatesModel;
}

export interface RegularExpenseReviewItemInput {
  regularExpenseId: string;
  amount: number;
}
