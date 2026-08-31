import { Divider, Grid, MenuItem, Typography } from "@mui/material";
import { useEffect, useState } from "react";

import useUtils from "src/appUtils";
import LockedButton from "src/components/LockedComponents/LockedButton/LockedButton";
import LockedSelect from "src/components/LockedComponents/LockedSelect/LockedSelect";
import LockedTextField from "src/components/LockedComponents/LockedTextField/LockedTextField";
import { BudgetTrackerAvailableCurrencies } from "src/models/shared/Currency";
import { DialogMode } from "src/models/shared/DialogMode";
import {
  RegularExpenseType,
  regularExpenseTypes,
  type RegularExpense,
} from "src/models/budgetTracker/regularExpenses";

import {
  createRegularExpense,
  deleteRegularExpense,
  updateRegularExpense,
} from "../api";
import { majorToMinor, minorToMajor } from "../money";

interface ExpenseDialogProps {
  dialogMode: DialogMode;
  inputModel?: RegularExpense;
  closeDialog: () => void;
}

const emptyExpense: RegularExpense = {
  id: "",
  name: "",
  type: RegularExpenseType.Regular,
  currency: BudgetTrackerAvailableCurrencies[0],
  defaultAmount: 0,
  orderPriority: 0,
};

const typeLabelKey = (type: RegularExpenseType): string => {
  switch (type) {
    case RegularExpenseType.Subscription:
      return "budgetTracker:regular_expenses_type_subscription";
    case RegularExpenseType.Annual:
      return "budgetTracker:regular_expenses_type_annual";
    default:
      return "budgetTracker:regular_expenses_type_regular";
  }
};

const amountLabelKey = (type: RegularExpenseType): string =>
  type === RegularExpenseType.Annual
    ? "budgetTracker:regular_expenses_yearly_amount_column"
    : "budgetTracker:regular_expenses_default_amount_column";

const ExpenseDialog = ({ dialogMode, inputModel, closeDialog }: ExpenseDialogProps) => {
  const u = useUtils();
  const [model, setModel] = useState<RegularExpense>(inputModel ?? emptyExpense);
  const [amountInput, setAmountInput] = useState(minorToMajor(inputModel?.defaultAmount ?? 0));
  const isDelete = dialogMode === DialogMode.Delete;

  useEffect(() => {
    const next = inputModel ?? emptyExpense;
    setModel(next);
    setAmountInput(minorToMajor(next.defaultAmount ?? 0));
  }, [inputModel]);

  if (dialogMode === DialogMode.Hide || !inputModel && dialogMode !== DialogMode.Create) {
    return null;
  }

  const amountMinor = majorToMinor(amountInput);
  const nameValid = model.name.trim().length > 0;
  const amountValid = amountMinor !== null;
  const isValid = nameValid && amountValid;

  const updateModel = (field: keyof RegularExpense, value: unknown) => {
    setModel((current) => ({ ...current, [field]: value }));
  };

  const submit = async () => {
    if (isDelete) {
      if (!model.id) return;
      try {
        await deleteRegularExpense(u, model.id);
        closeDialog();
      } catch {
        // APICallWrapper displays the translated API failure.
      }
      return;
    }

    if (!isValid || amountMinor === null) return;

    try {
      if (dialogMode === DialogMode.Create) {
        await createRegularExpense(u, {
          name: model.name.trim(),
          type: model.type,
          currency: model.currency,
          defaultAmount: amountMinor,
          orderPriority: Number(model.orderPriority) || 0,
        });
      } else {
        await updateRegularExpense(u, {
          ...model,
          name: model.name.trim(),
          defaultAmount: amountMinor,
          orderPriority: Number(model.orderPriority) || 0,
        });
      }
      closeDialog();
    } catch {
      // APICallWrapper displays the translated API failure.
    }
  };

  return (
    <Grid container spacing={2} sx={{ p: 2, minWidth: { xs: "min(80vw, 360px)", sm: 460 } }}>
      <Grid size={{ xs: 12 }}>
        <LockedTextField
          fullWidth
          label={u.t("budgetTracker:regular_expenses_name_column")}
          value={model.name}
          onChange={(event) => updateModel("name", event.target.value)}
          error={!nameValid && model.name.length > 0}
          helperText={!nameValid && model.name.length > 0 ? u.t("budgetTracker:regular_expenses_validation_name") : undefined}
          disabled={isDelete}
          autoFocus={!isDelete}
          variant="standard"
        />
      </Grid>
      <Grid size={{ xs: 12, sm: 6 }}>
        <LockedSelect
          fullWidth
          value={model.type}
          onChange={(event) => updateModel("type", event.target.value as RegularExpenseType)}
          disabled={isDelete}
          aria-label={u.t("budgetTracker:regular_expenses_type_column")}
        >
          {regularExpenseTypes.map((type) => (
            <MenuItem key={type} value={type}>
              {u.t(typeLabelKey(type))}
            </MenuItem>
          ))}
        </LockedSelect>
      </Grid>
      <Grid size={{ xs: 12, sm: 6 }}>
        <LockedSelect
          fullWidth
          value={model.currency}
          onChange={(event) => updateModel("currency", event.target.value)}
          disabled={isDelete}
          aria-label={u.t("budgetTracker:regular_expenses_currency_column")}
        >
          {BudgetTrackerAvailableCurrencies.map((currency) => (
            <MenuItem key={currency} value={currency}>
              {currency}
            </MenuItem>
          ))}
        </LockedSelect>
      </Grid>
      <Grid size={{ xs: 8, sm: 8 }}>
        <LockedTextField
          fullWidth
          label={u.t(amountLabelKey(model.type))}
          type="number"
          slotProps={{ htmlInput: { min: 0, step: "0.01", inputMode: "decimal" } }}
          value={amountInput}
          onChange={(event) => setAmountInput(event.target.value)}
          error={!amountValid}
          helperText={!amountValid ? u.t("budgetTracker:regular_expenses_validation_amount") : undefined}
          disabled={isDelete}
          variant="standard"
        />
      </Grid>
      <Grid size={{ xs: 4, sm: 4 }}>
        <LockedTextField
          fullWidth
          label={u.t("budgetTracker:regular_expenses_priority_column")}
          type="number"
          slotProps={{ htmlInput: { step: 1 } }}
          value={model.orderPriority}
          onChange={(event) => updateModel("orderPriority", Number(event.target.value))}
          disabled={isDelete}
          variant="standard"
        />
      </Grid>
      {model.type === RegularExpenseType.Annual && !isDelete && (
        <Grid size={{ xs: 12 }}>
          <Typography variant="caption" color="text.secondary">
            {u.t("budgetTracker:regular_expenses_monthly_contribution_column")}: {amountMinor === null ? "—" : minorToMajor(amountMinor / 12)} {model.currency}
          </Typography>
        </Grid>
      )}
      <Grid size={{ xs: 12 }}>
        <Divider />
      </Grid>
      <Grid size={{ xs: 12 }} sx={{ display: "flex", justifyContent: "center" }}>
        <LockedButton
          fullWidth={u.isMobile}
          color={isDelete ? "error" : "primary"}
          disabled={!isDelete && !isValid}
          onClick={() => void submit()}
          variant="outlined"
        >
          {u.t(isDelete ? "Delete" : dialogMode === DialogMode.Create ? "Create" : "Update")}
        </LockedButton>
      </Grid>
    </Grid>
  );
};

export default ExpenseDialog;
