import { useEffect, useState } from "react";
import {
  Button,
  Checkbox,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  FormControl,
  FormControlLabel,
  FormGroup,
  FormHelperText,
  FormLabel,
  Grid,
  InputLabel,
  MenuItem,
  Select,
  TextField,
} from "@mui/material";
import { useTranslation } from "react-i18next";

import { Currency, HistoryType } from "src/models/myGarage/CarModels";
import { AllAvailableCurrencies } from "src/models/shared/Currency";
import type { CreateServiceHistoryRequest } from "src/models/myGarage/CarRequests";
import type { MaintenanceItem, ServiceHistoryRecord } from "src/models/myGarage/CarModels";

import { majorToMinor } from "../helpers/money";
import { normalizeMaintenanceSelection, toggleMaintenanceSelection } from "../helpers/historySelection";

interface HistoryDialogProps {
  open: boolean;
  record?: ServiceHistoryRecord | null;
  maintenanceItems: MaintenanceItem[];
  busy?: boolean;
  submitError?: string | null;
  onClose: () => void;
  onSubmit: (request: CreateServiceHistoryRequest) => Promise<void> | void;
}

interface HistoryForm {
  date: string;
  odometerKm: string;
  type: HistoryType;
  title: string;
  notes: string;
  linkedMaintenanceItemIds: string[];
  costAmount: string;
  costCurrency: Currency | "";
}

const today = () => new Date().toISOString().slice(0, 10);

const emptyForm = (): HistoryForm => ({ date: today(), odometerKm: "", type: HistoryType.Maintenance, title: "", notes: "", linkedMaintenanceItemIds: [], costAmount: "", costCurrency: "" });

export default function HistoryDialog({
  open,
  record,
  maintenanceItems,
  busy = false,
  submitError,
  onClose,
  onSubmit,
}: HistoryDialogProps) {
  const { t } = useTranslation("myGarage");
  const [form, setForm] = useState<HistoryForm>(emptyForm());
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    setForm(record ? {
      date: record.date,
      odometerKm: record.odometerKm.toString(),
      type: record.type,
      title: record.title,
      notes: record.notes ?? "",
      linkedMaintenanceItemIds: normalizeMaintenanceSelection(record.linkedMaintenanceItemIds),
      costAmount: record.costAmountMinor == null ? "" : (record.costAmountMinor / 100).toString(),
      costCurrency: record.costCurrency ?? "",
    } : emptyForm());
    setError(null);
  }, [record, open]);

  const update = <K extends keyof HistoryForm>(field: K, value: HistoryForm[K]) => {
    setForm((current) => ({ ...current, [field]: value }));
    setError(null);
  };

  const submit = async (event: React.FormEvent) => {
    event.preventDefault();
    const odometerKm = Number(form.odometerKm);
    if (!form.date) {
      setError(t("validation.historyDate"));
      return;
    }
    if (!Number.isInteger(odometerKm) || odometerKm < 0) {
      setError(t("validation.historyOdometer"));
      return;
    }
    if (!form.title.trim()) {
      setError(t("validation.historyTitle"));
      return;
    }
    const costAmountMinor = majorToMinor(form.costAmount);
    const hasAmount = costAmountMinor !== null;
    const hasCurrency = Boolean(form.costCurrency);
    if (hasAmount !== hasCurrency) {
      setError(t("validation.historyCostPair"));
      return;
    }
    await onSubmit({
      date: form.date,
      odometerKm,
      type: form.type,
      title: form.title.trim(),
      notes: form.notes.trim() || null,
      linkedMaintenanceItemIds: normalizeMaintenanceSelection(form.linkedMaintenanceItemIds),
      costAmountMinor: hasAmount ? costAmountMinor : null,
      costCurrency: hasCurrency ? form.costCurrency as Currency : null,
    });
  };

  const editing = Boolean(record);

  return (
    <Dialog open={open} onClose={busy ? undefined : onClose} fullWidth maxWidth="sm">
      <form onSubmit={submit} noValidate>
        <DialogTitle>{t(editing ? "actions.editHistory" : "actions.recordService")}</DialogTitle>
        <DialogContent>
          <Grid container spacing={2} sx={{ pt: 1 }}>
            <Grid size={{ xs: 12, sm: 6 }}>
              <TextField fullWidth required type="date" label={t("fields.date")} value={form.date} onChange={(event) => update("date", event.target.value)} slotProps={{ inputLabel: { shrink: true } }} disabled={busy} />
            </Grid>
            <Grid size={{ xs: 12, sm: 6 }}>
              <TextField fullWidth required type="number" label={t("fields.odometerKm")} value={form.odometerKm} onChange={(event) => update("odometerKm", event.target.value)} slotProps={{ htmlInput: { min: 0, step: 1 } }} disabled={busy} />
            </Grid>
            <Grid size={{ xs: 12, sm: 6 }}>
              <FormControl fullWidth disabled={busy}>
                <InputLabel id="history-type-label">{t("fields.type")}</InputLabel>
                <Select id="history-type" labelId="history-type-label" label={t("fields.type")} value={form.type} onChange={(event) => update("type", event.target.value as HistoryType)}>
                  {Object.values(HistoryType).map((type) => <MenuItem key={type} value={type}>{t(`types.${type}`)}</MenuItem>)}
                </Select>
              </FormControl>
            </Grid>
            <Grid size={{ xs: 12, sm: 6 }}>
              <TextField fullWidth required label={t("fields.title")} value={form.title} onChange={(event) => update("title", event.target.value)} disabled={busy} />
            </Grid>
            <Grid size={{ xs: 12 }}>
              <TextField fullWidth multiline minRows={2} label={t("fields.notes")} value={form.notes} onChange={(event) => update("notes", event.target.value)} disabled={busy} />
            </Grid>
            <Grid size={{ xs: 12 }}>
              <FormControl component="fieldset" fullWidth disabled={busy} aria-describedby="linked-maintenance-help">
                <FormLabel component="legend">{t("fields.linkedMaintenance")}</FormLabel>
                <FormGroup>
                  {maintenanceItems.map((item) => <FormControlLabel key={item.id} control={<Checkbox checked={form.linkedMaintenanceItemIds.includes(item.id)} onChange={() => update("linkedMaintenanceItemIds", toggleMaintenanceSelection(form.linkedMaintenanceItemIds, item.id))} />} label={item.name} />)}
                </FormGroup>
                <FormHelperText id="linked-maintenance-help">{t("fields.linkedMaintenanceHelp")}</FormHelperText>
              </FormControl>
            </Grid>
            <Grid size={{ xs: 12, sm: 6 }}>
              <TextField fullWidth type="number" label={t("fields.costAmount")} value={form.costAmount} onChange={(event) => update("costAmount", event.target.value)} slotProps={{ htmlInput: { min: 0, step: "0.01" } }} disabled={busy} />
            </Grid>
            <Grid size={{ xs: 12, sm: 6 }}>
              <FormControl fullWidth disabled={busy}>
                <InputLabel id="history-currency-label">{t("fields.costCurrency")}</InputLabel>
                <Select id="history-currency" labelId="history-currency-label" label={t("fields.costCurrency")} value={form.costCurrency} onChange={(event) => update("costCurrency", event.target.value as Currency | "")}>
                  <MenuItem value="">-</MenuItem>
                  {AllAvailableCurrencies.map((currency) => <MenuItem key={currency} value={currency}>{currency}</MenuItem>)}
                </Select>
              </FormControl>
            </Grid>
            {error || submitError ? <Grid size={{ xs: 12 }}><div role="alert">{submitError || error}</div></Grid> : null}
          </Grid>
        </DialogContent>
        <DialogActions>
          <Button onClick={onClose} disabled={busy}>{t("actions.cancel")}</Button>
          <Button type="submit" variant="contained" disabled={busy}>{t("actions.save")}</Button>
        </DialogActions>
      </form>
    </Dialog>
  );
}
