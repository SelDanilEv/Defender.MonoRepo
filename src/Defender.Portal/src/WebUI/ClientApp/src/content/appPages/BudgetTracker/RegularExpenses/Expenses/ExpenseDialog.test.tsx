import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { ThemeProvider, createTheme } from "@mui/material/styles";
import { configureStore } from "@reduxjs/toolkit";
import { Provider } from "react-redux";

import { Currency } from "src/models/shared/Currency";
import { DialogMode } from "src/models/shared/DialogMode";
import {
  RegularExpenseType,
  type RegularExpense,
} from "src/models/budgetTracker/regularExpenses";

import {
  createRegularExpense,
  deleteRegularExpense,
  updateRegularExpense,
} from "../api";
import ExpenseDialog from "./ExpenseDialog";

vi.mock("src/appUtils", () => ({
  default: () => ({
    isMobile: false,
    t: (key: string) => key,
  }),
}));

vi.mock("../api", () => ({
  createRegularExpense: vi.fn(),
  deleteRegularExpense: vi.fn(),
  updateRegularExpense: vi.fn(),
}));

const loadingReducer = (state = { loading: false }, _action: unknown) => state;

const renderDialog = (
  dialogMode: DialogMode,
  inputModel?: RegularExpense,
  closeDialog = vi.fn(),
) => {
  const store = configureStore({ reducer: { loading: loadingReducer } });

  return {
    closeDialog,
    ...render(
      <Provider store={store}>
        <ThemeProvider theme={createTheme()}>
          <ExpenseDialog
            dialogMode={dialogMode}
            inputModel={inputModel}
            closeDialog={closeDialog}
          />
        </ThemeProvider>
      </Provider>,
    ),
  };
};

const annualExpense: RegularExpense = {
  id: "annual-id",
  name: "Insurance",
  type: RegularExpenseType.Annual,
  currency: Currency.USD,
  defaultAmount: 12_000,
  orderPriority: 0,
};

describe("Regular expense dialog", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(createRegularExpense).mockResolvedValue(annualExpense);
    vi.mocked(updateRegularExpense).mockResolvedValue(annualExpense);
    vi.mocked(deleteRegularExpense).mockResolvedValue(annualExpense.id);
  });

  test("AnnualExpense_WhenOpened_ShowsAmountDividedByTwelve", () => {
    renderDialog(DialogMode.Update, annualExpense);

    expect(
      screen.getByText(
        /budgetTracker:regular_expenses_monthly_contribution_column: 10 USD/,
      ),
    ).not.toBeNull();
  });

  test("Create_WhenValidAnnualAmountSubmitted_SendsMinorUnitPayload", async () => {
    const { closeDialog } = renderDialog(DialogMode.Create);

    fireEvent.change(
      screen.getByLabelText("budgetTracker:regular_expenses_name_column"),
      { target: { value: "Insurance" } },
    );
    fireEvent.mouseDown(
      screen.getByLabelText("budgetTracker:regular_expenses_type_column"),
    );
    fireEvent.click(
      screen.getByRole("option", {
        name: "budgetTracker:regular_expenses_type_annual",
      }),
    );
    fireEvent.change(
      screen.getByLabelText("budgetTracker:regular_expenses_yearly_amount_column"),
      { target: { value: "120" } },
    );

    fireEvent.click(screen.getByRole("button", { name: "Create" }));

    await waitFor(() => expect(createRegularExpense).toHaveBeenCalled());
    expect(createRegularExpense).toHaveBeenCalledWith(
      expect.anything(),
      expect.objectContaining({
        name: "Insurance",
        type: RegularExpenseType.Annual,
        currency: Currency.USD,
        defaultAmount: 12_000,
      }),
    );
    expect(closeDialog).toHaveBeenCalledTimes(1);
  });

  test("Create_WhenAmountIsNegative_DisablesSubmit", () => {
    renderDialog(DialogMode.Create);

    fireEvent.change(
      screen.getByLabelText("budgetTracker:regular_expenses_name_column"),
      { target: { value: "Invalid" } },
    );
    fireEvent.change(
      screen.getByLabelText("budgetTracker:regular_expenses_default_amount_column"),
      { target: { value: "-1" } },
    );

    expect(screen.getByRole("button", { name: "Create" })).toHaveProperty(
      "disabled",
      true,
    );
  });
});
