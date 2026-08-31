import APICallWrapper from "src/api/APIWrapper/APICallWrapper";
import RequestParamsBuilder from "src/api/APIWrapper/RequestParamsBuilder";
import apiUrls from "src/api/apiUrls";
import type IUtils from "src/appUtils/interface";
import type {
  RegularExpense,
  RegularExpenseDiagramSetup,
  RegularExpenseInput,
  RegularExpenseReview,
  RegularExpenseReviewItemInput,
  RegularExpensesPageResponse,
  RegularExpenseReviewsPageResponse,
} from "src/models/budgetTracker/regularExpenses";
import type { PaginationRequest } from "src/models/base/PaginationRequest";

type Failure = { status?: number; detail?: string };

const requestJson = <T>(
  url: string,
  options: RequestInit,
  utils: IUtils,
  settings: { showSuccess?: boolean; showError?: boolean; doLock?: boolean } = {},
): Promise<T> =>
  new Promise<T>((resolve, reject) => {
    APICallWrapper({
      url,
      options,
      utils,
      showSuccess: settings.showSuccess ?? false,
      showError: settings.showError ?? true,
      doLock: settings.doLock ?? true,
      onSuccess: async (response) => {
        try {
          resolve((await response.json()) as T);
        } catch (error) {
          reject(error);
        }
      },
      onFailure: async (failure: Failure) => {
        reject(failure);
      },
    });
  });

const jsonOptions = (method: string, body?: unknown): RequestInit => ({
  method,
  headers: { "Content-Type": "application/json" },
  ...(body === undefined
    ? {}
    : { body: RequestParamsBuilder.BuildBody(body) }),
});

export const getRegularExpenses = (
  utils: IUtils,
  pagination: PaginationRequest,
): Promise<RegularExpensesPageResponse> =>
  requestJson(
    `${apiUrls.budgetTracker.regularExpenses.getExpenses}${RequestParamsBuilder.BuildQuery(pagination)}`,
    jsonOptions("GET"),
    utils,
  );

export const createRegularExpense = (
  utils: IUtils,
  input: RegularExpenseInput,
): Promise<RegularExpense> =>
  requestJson(
    apiUrls.budgetTracker.regularExpenses.expense,
    jsonOptions("POST", input),
    utils,
    { showSuccess: true },
  );

export const updateRegularExpense = (
  utils: IUtils,
  input: RegularExpense,
): Promise<RegularExpense> =>
  requestJson(
    apiUrls.budgetTracker.regularExpenses.expense,
    jsonOptions("PUT", input),
    utils,
    { showSuccess: true },
  );

export const deleteRegularExpense = (
  utils: IUtils,
  id: string,
): Promise<string> =>
  requestJson(
    `${apiUrls.budgetTracker.regularExpenses.expense}/${id}`,
    jsonOptions("DELETE"),
    utils,
    { showSuccess: true },
  );

export const getRegularExpenseReviews = (
  utils: IUtils,
  pagination: PaginationRequest,
): Promise<RegularExpenseReviewsPageResponse> =>
  requestJson(
    `${apiUrls.budgetTracker.regularExpenses.getReviews}${RequestParamsBuilder.BuildQuery(pagination)}`,
    jsonOptions("GET"),
    utils,
  );

export const getRegularExpenseReviewsByDateRange = (
  utils: IUtils,
  startMonth: string,
  endMonth: string,
): Promise<RegularExpenseReview[]> =>
  requestJson(
    `${apiUrls.budgetTracker.regularExpenses.getReviewsByDateRange}${RequestParamsBuilder.BuildQuery({
      startMonth,
      endMonth,
    })}`,
    jsonOptions("GET"),
    utils,
    { doLock: false },
  );

export const getRegularExpenseReviewTemplate = (
  utils: IUtils,
  month: string,
): Promise<RegularExpenseReview> =>
  requestJson(
    `${apiUrls.budgetTracker.regularExpenses.getReviewTemplate}${RequestParamsBuilder.BuildQuery({ month })}`,
    jsonOptions("GET"),
    utils,
  );

export const saveRegularExpenseReview = (
  utils: IUtils,
  review: Pick<RegularExpenseReview, "id" | "month" | "expenses">,
): Promise<RegularExpenseReview> =>
  requestJson(
    apiUrls.budgetTracker.regularExpenses.review,
    jsonOptions("POST", {
      ...(review.id ? { id: review.id } : {}),
      month: normalizeMonth(review.month),
      expenses: review.expenses.map(
        (expense): RegularExpenseReviewItemInput => ({
          regularExpenseId: expense.regularExpenseId,
          amount: expense.amount,
        }),
      ),
    }),
    utils,
    { showSuccess: true },
  );

export const deleteRegularExpenseReview = (
  utils: IUtils,
  id: string,
): Promise<string> =>
  requestJson(
    `${apiUrls.budgetTracker.regularExpenses.review}/${id}`,
    jsonOptions("DELETE"),
    utils,
    { showSuccess: true },
  );

export const getRegularExpenseDiagramSetup = (
  utils: IUtils,
): Promise<RegularExpenseDiagramSetup> =>
  requestJson(
    apiUrls.budgetTracker.regularExpenses.diagramSetup,
    jsonOptions("GET"),
    utils,
  );

export const saveRegularExpenseDiagramSetup = (
  utils: IUtils,
  setup: RegularExpenseDiagramSetup,
): Promise<RegularExpenseDiagramSetup> =>
  requestJson(
    apiUrls.budgetTracker.regularExpenses.diagramSetup,
    jsonOptions("POST", {
      mainCurrency: setup.mainCurrency,
      lastMonths: setup.lastMonths,
      endMonth: setup.endMonth ? normalizeMonth(setup.endMonth) : undefined,
    }),
    utils,
    { doLock: false },
  );

export const normalizeMonth = (value: string): string => {
  if (!value) return value;
  const [year, month] = value.slice(0, 7).split("-");
  return `${year}-${month}-01`;
};

export const monthInputValue = (value: string | undefined): string =>
  value ? value.slice(0, 7) : "";
