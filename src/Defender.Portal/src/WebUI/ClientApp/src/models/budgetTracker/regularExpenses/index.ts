export * from "./RegularExpense";
export * from "./RegularExpenseDiagramSetup";
export * from "./RegularExpenseReview";
export * from "./RegularExpenseType";

import type { RegularExpense, RegularExpenseReview } from ".";

export interface RegularExpensesPageResponse {
  items: RegularExpense[];
  totalItemsCount: number;
  currentPage: number;
  pageSize: number;
  totalPagesCount: number;
}

export interface RegularExpenseReviewsPageResponse {
  items: RegularExpenseReview[];
  totalItemsCount: number;
  currentPage: number;
  pageSize: number;
  totalPagesCount: number;
}
