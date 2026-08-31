import { CircularProgress, Divider, Grid, Paper, Typography } from "@mui/material";
import { useEffect, useMemo, useState } from "react";

import useUtils from "src/appUtils";
import LockedButton from "src/components/LockedComponents/LockedButton/LockedButton";
import LockedTextField from "src/components/LockedComponents/LockedTextField/LockedTextField";
import CurrencySymbolsMap from "src/consts/CurrencySymbolsMap";
import { DialogMode } from "src/models/shared/DialogMode";
import type {
  RegularExpenseReview,
  ReviewedRegularExpense,
} from "src/models/budgetTracker/regularExpenses";
import { RegularExpenseType } from "src/models/budgetTracker/regularExpenses";

import {
  deleteRegularExpenseReview,
  getRegularExpenseReviewTemplate,
  monthInputValue,
  normalizeMonth,
  saveRegularExpenseReview,
} from "../api";
import { majorToMinor, minorToMajor } from "../money";
import { reviewExpenseMonthlyMajor } from "./reviewData";

interface ReviewDialogProps {
  dialogMode: DialogMode;
  inputModel?: RegularExpenseReview;
  closeDialog: () => void;
}

const currentMonth = (): string => {
  const now = new Date();
  return `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, "0")}`;
};

const typeLabelKey = (type: ReviewedRegularExpense["type"]): string => {
  switch (type) {
    case RegularExpenseType.Subscription:
      return "budgetTracker:regular_expenses_type_subscription";
    case RegularExpenseType.Annual:
      return "budgetTracker:regular_expenses_type_annual";
    default:
      return "budgetTracker:regular_expenses_type_regular";
  }
};

const amountLabelKey = (type: ReviewedRegularExpense["type"]): string =>
  type === RegularExpenseType.Annual
    ? "budgetTracker:regular_expenses_yearly_amount_column"
    : "budgetTracker:regular_expenses_default_amount_column";

const ReviewDialog = ({ dialogMode, inputModel, closeDialog }: ReviewDialogProps) => {
  const u = useUtils();
  const [month, setMonth] = useState(monthInputValue(inputModel?.month) || currentMonth());
  const [review, setReview] = useState<RegularExpenseReview | undefined>(inputModel);
  const [amountInputs, setAmountInputs] = useState<Record<string, string>>({});
  const [templateLoading, setTemplateLoading] = useState(false);
  const [templateError, setTemplateError] = useState(false);
  const [lastLoadedMonth, setLastLoadedMonth] = useState<string>();
  const isDelete = dialogMode === DialogMode.Delete;
  const isCreate = dialogMode === DialogMode.Create;

  useEffect(() => {
    const nextMonth = monthInputValue(inputModel?.month) || currentMonth();
    setMonth(nextMonth);
    setReview(inputModel);
    setLastLoadedMonth(inputModel ? nextMonth : undefined);
    setTemplateError(false);
    setAmountInputs(
      Object.fromEntries(
        (inputModel?.expenses ?? []).map((expense) => [expense.regularExpenseId, minorToMajor(expense.amount)]),
      ),
    );
  }, [inputModel]);

  useEffect(() => {
    if (!isCreate || !month || month.length !== 7 || lastLoadedMonth === month) return;

    setTemplateLoading(true);
    setTemplateError(false);
    setLastLoadedMonth(month);
    getRegularExpenseReviewTemplate(u, normalizeMonth(month))
      .then((template) => {
        setReview(template);
        setMonth(monthInputValue(template.month) || month);
        setAmountInputs(
          Object.fromEntries(
            (template.expenses ?? []).map((expense) => [expense.regularExpenseId, minorToMajor(expense.amount)]),
          ),
        );
      })
      .catch(() => {
        setReview(undefined);
        setTemplateError(true);
      })
      .finally(() => setTemplateLoading(false));
  }, [isCreate, month, lastLoadedMonth, u]);

  const isMonthValid = /^\d{4}-(0[1-9]|1[0-2])$/.test(month);
  const expenseInputs = useMemo(
    () => (review?.expenses ?? []).map((expense) => ({
      expense,
      amountMinor: majorToMinor(amountInputs[expense.regularExpenseId] ?? minorToMajor(expense.amount)),
    })),
    [review, amountInputs],
  );
  const canSave = !!review && isMonthValid && expenseInputs.every((item) => item.amountMinor !== null);

  if (dialogMode === DialogMode.Hide) return null;

  const updateAmount = (expense: ReviewedRegularExpense, value: string) => {
    setAmountInputs((current) => ({ ...current, [expense.regularExpenseId]: value }));
  };

  const submit = async () => {
    if (isDelete) {
      if (!review?.id) return;
      try {
        await deleteRegularExpenseReview(u, review.id);
        closeDialog();
      } catch {
        // APICallWrapper displays the translated API failure.
      }
      return;
    }

    if (!review || !canSave) return;
    try {
      await saveRegularExpenseReview(u, {
        id: review.id,
        month: normalizeMonth(month),
        expenses: review.expenses.map((expense) => ({
          ...expense,
          amount: majorToMinor(amountInputs[expense.regularExpenseId] ?? minorToMajor(expense.amount)) ?? 0,
        })),
      });
      closeDialog();
    } catch {
      // APICallWrapper displays the translated API failure.
    }
  };

  const loadTemplate = () => {
    setLastLoadedMonth(undefined);
  };

  return (
    <Grid container spacing={2} sx={{ p: 2, minWidth: { xs: "min(82vw, 380px)", sm: 500 }, maxWidth: 640 }}>
      <Grid size={{ xs: 12 }}>
        <LockedTextField
          fullWidth
          type="month"
          label={u.t("budgetTracker:regular_expenses_month_column")}
          value={month}
          onChange={(event) => {
            setMonth(event.target.value);
            if (isCreate) setReview(undefined);
          }}
          disabled={!isCreate || isDelete}
          error={!isMonthValid}
          helperText={!isMonthValid ? u.t("budgetTracker:regular_expenses_validation_month") : undefined}
          slotProps={{ inputLabel: { shrink: true } }}
        />
      </Grid>
      {isCreate && (templateLoading || !review) && (
        <Grid size={{ xs: 12 }} sx={{ display: "flex", justifyContent: "center", py: 2 }}>
          {templateLoading ? <CircularProgress size={28} aria-label={u.t("budgetTracker:regular_expenses_load_template")} /> : (
            <Grid container spacing={1} sx={{ width: "100%" }}>
              <Grid size={{ xs: 12 }}>
                <Typography color={templateError ? "error" : "text.secondary"}>
                  {templateError ? u.t("budgetTracker:regular_expenses_template_error") : u.t("budgetTracker:regular_expenses_no_expenses")}
                </Typography>
              </Grid>
              <Grid size={{ xs: 12 }}>
                <LockedButton variant="outlined" onClick={loadTemplate} disabled={!isMonthValid}>
                  {u.t("budgetTracker:regular_expenses_load_template")}
                </LockedButton>
              </Grid>
            </Grid>
          )}
        </Grid>
      )}
      {review && (
        <Grid size={{ xs: 12 }}>
          {review.expenses.length === 0 ? (
            <Typography color="text.secondary" sx={{ py: 1 }}>
              {u.t("budgetTracker:regular_expenses_no_expenses")}
            </Typography>
          ) : review.expenses.map((expense) => (
            <Paper key={expense.regularExpenseId} variant="outlined" sx={{ p: 1.5, mb: 1.2 }}>
              <Grid container spacing={1.5} sx={{ alignItems: "center" }}>
                <Grid size={{ xs: 12, sm: 5 }}>
                  <Typography sx={{ fontWeight: "bold" }}>{expense.name}</Typography>
                  <Typography variant="caption" color="text.secondary">
                    {u.t(typeLabelKey(expense.type))} · {expense.currency}
                  </Typography>
                </Grid>
                <Grid size={{ xs: 7, sm: 4 }}>
                  <LockedTextField
                    fullWidth
                    label={u.t(amountLabelKey(expense.type))}
                    type="number"
                    slotProps={{ htmlInput: { min: 0, step: "0.01", inputMode: "decimal" } }}
                    value={amountInputs[expense.regularExpenseId] ?? minorToMajor(expense.amount)}
                    onChange={(event) => updateAmount(expense, event.target.value)}
                    disabled={isDelete}
                    error={majorToMinor(amountInputs[expense.regularExpenseId] ?? minorToMajor(expense.amount)) === null}
                    variant="standard"
                  />
                </Grid>
                <Grid size={{ xs: 5, sm: 3 }}>
                  <Typography variant="body2" color="text.secondary">
                    {u.t("budgetTracker:regular_expenses_monthly_contribution_column")}
                  </Typography>
                  <Typography sx={{ fontWeight: "bold" }}>
                    {reviewExpenseMonthlyMajor({
                      ...expense,
                      amount: majorToMinor(amountInputs[expense.regularExpenseId] ?? minorToMajor(expense.amount)) ?? expense.amount,
                    })} {CurrencySymbolsMap[expense.currency] || expense.currency}
                  </Typography>
                </Grid>
              </Grid>
            </Paper>
          ))}
        </Grid>
      )}
      <Grid size={{ xs: 12 }}><Divider /></Grid>
      <Grid size={{ xs: 12 }} sx={{ display: "flex", justifyContent: "center" }}>
        <LockedButton
          fullWidth={u.isMobile}
          color={isDelete ? "error" : "primary"}
          disabled={!isDelete && !canSave}
          onClick={() => void submit()}
          variant="outlined"
        >
          {u.t(isDelete ? "Delete" : "budgetTracker:regular_expenses_save_review")}
        </LockedButton>
      </Grid>
    </Grid>
  );
};

export default ReviewDialog;
