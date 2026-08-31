import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { ThemeProvider, createTheme } from "@mui/material/styles";
import { configureStore } from "@reduxjs/toolkit";
import { Provider } from "react-redux";

import { Currency } from "src/models/shared/Currency";
import { DialogMode } from "src/models/shared/DialogMode";
import {
  RegularExpenseType,
  type RegularExpenseReview,
} from "src/models/budgetTracker/regularExpenses";

import {
  deleteRegularExpenseReview,
  getRegularExpenseReviewTemplate,
  saveRegularExpenseReview,
} from "../api";
import ReviewDialog from "./ReviewDialog";

vi.mock("src/appUtils", () => ({
  default: () => ({
    isMobile: false,
    t: (key: string) => key,
  }),
}));

vi.mock("../api", async (importOriginal) => {
  const actual = await importOriginal<typeof import("../api")>();

  return {
    ...actual,
    deleteRegularExpenseReview: vi.fn(),
    getRegularExpenseReviewTemplate: vi.fn(),
    saveRegularExpenseReview: vi.fn(),
  };
});

const loadingReducer = (state = { loading: false }, _action: unknown) => state;

const renderDialog = (
  dialogMode: DialogMode,
  inputModel?: RegularExpenseReview,
  closeDialog = vi.fn(),
) => {
  const store = configureStore({ reducer: { loading: loadingReducer } });

  return {
    closeDialog,
    ...render(
      <Provider store={store}>
        <ThemeProvider theme={createTheme()}>
          <ReviewDialog
            dialogMode={dialogMode}
            inputModel={inputModel}
            closeDialog={closeDialog}
          />
        </ThemeProvider>
      </Provider>,
    ),
  };
};

const annualReview: RegularExpenseReview = {
  id: "review-id",
  month: "2026-08-15",
  expenses: [
    {
      regularExpenseId: "annual-id",
      name: "Insurance",
      type: RegularExpenseType.Annual,
      currency: Currency.USD,
      amount: 12_000,
      orderPriority: 0,
      monthlyContribution: 1_000,
    },
  ],
  ratesModel: {
    date: "2026-08-01",
    baseCurrency: Currency.USD,
    rates: {},
  },
};

describe("Regular expense review dialog", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(getRegularExpenseReviewTemplate).mockResolvedValue(annualReview);
    vi.mocked(saveRegularExpenseReview).mockResolvedValue(annualReview);
    vi.mocked(deleteRegularExpenseReview).mockResolvedValue(annualReview.id);
  });

  test("AnnualSnapshot_WhenOpened_ShowsMonthlyContribution", () => {
    renderDialog(DialogMode.Update, annualReview);

    expect(document.body.textContent).toContain("10");
    expect(document.body.textContent).toContain("$");
  });

  test("Update_WhenSaved_NormalizesMonthAndSendsMinorUnits", async () => {
    renderDialog(DialogMode.Update, annualReview);

    fireEvent.change(
      screen.getByLabelText("budgetTracker:regular_expenses_yearly_amount_column"),
      { target: { value: "100" } },
    );
    fireEvent.click(
      screen.getByRole("button", {
        name: "budgetTracker:regular_expenses_save_review",
      }),
    );

    await waitFor(() => expect(saveRegularExpenseReview).toHaveBeenCalled());
    expect(saveRegularExpenseReview).toHaveBeenCalledWith(
      expect.anything(),
      expect.objectContaining({
        id: annualReview.id,
        month: "2026-08-01",
        expenses: [
          expect.objectContaining({
            regularExpenseId: "annual-id",
            amount: 10_000,
          }),
        ],
      }),
    );
  });

  test("Delete_WhenReviewExists_DeletesOnlySelectedReview", async () => {
    const { closeDialog } = renderDialog(DialogMode.Delete, annualReview);

    fireEvent.click(screen.getByRole("button", { name: "Delete" }));

    await waitFor(() => expect(deleteRegularExpenseReview).toHaveBeenCalled());
    expect(deleteRegularExpenseReview).toHaveBeenCalledWith(
      expect.anything(),
      annualReview.id,
    );
    expect(closeDialog).toHaveBeenCalledTimes(1);
  });
});
