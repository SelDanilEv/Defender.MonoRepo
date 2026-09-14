import { useEffect, useState } from "react";
import {
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  FormHelperText,
  Grid,
  TextField,
} from "@mui/material";
import { useTranslation } from "react-i18next";

import type { CreateMaintenanceItemRequest } from "src/models/myGarage/CarRequests";
import type { MaintenanceItem } from "src/models/myGarage/CarModels";

interface MaintenanceDialogProps {
  open: boolean;
  item?: MaintenanceItem | null;
  baselineLocked?: boolean;
  busy?: boolean;
  submitError?: string | null;
  onClose: () => void;
  onSubmit: (request: CreateMaintenanceItemRequest) => Promise<void> | void;
}

interface MaintenanceForm {
  name: string;
  intervalMonths: string;
  intervalThousandKm: string;
  lastDate: string;
  lastOdometerKm: string;
}

const blankForm: MaintenanceForm = { name: "", intervalMonths: "", intervalThousandKm: "", lastDate: "", lastOdometerKm: "" };

export default function MaintenanceDialog({
  open,
  item,
  baselineLocked = false,
  busy = false,
  submitError,
  onClose,
  onSubmit,
}: MaintenanceDialogProps) {
  const { t } = useTranslation("myGarage");
  const [form, setForm] = useState<MaintenanceForm>(blankForm);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    setForm({
      name: item?.name ?? "",
      intervalMonths: item?.intervalMonths?.toString() ?? "",
      intervalThousandKm: item?.intervalThousandKm?.toString() ?? "",
      lastDate: item?.lastDate ?? "",
      lastOdometerKm: item?.lastOdometerKm?.toString() ?? "",
    });
    setError(null);
  }, [item, open]);

  const update = (field: keyof MaintenanceForm, value: string) => {
    setForm((current) => ({ ...current, [field]: value }));
    setError(null);
  };

  const parsePositive = (value: string): number | null => {
    if (!value.trim()) return null;
    const parsed = Number(value);
    return Number.isInteger(parsed) && parsed > 0 ? parsed : null;
  };

  const submit = async (event: React.FormEvent) => {
    event.preventDefault();
    const intervalMonths = parsePositive(form.intervalMonths);
    const intervalThousandKm = parsePositive(form.intervalThousandKm);
    if (!form.name.trim()) {
      setError(t("errors.CAR_MAINTENANCE_NAME_REQUIRED"));
      return;
    }
    if (intervalMonths === null && intervalThousandKm === null) {
      setError(t("validation.maintenanceInterval"));
      return;
    }
    if (form.intervalMonths.trim() && intervalMonths === null || form.intervalThousandKm.trim() && intervalThousandKm === null) {
      setError(t("errors.CAR_MAINTENANCE_INTERVAL_INVALID"));
      return;
    }
    const lastOdometerKm = form.lastOdometerKm.trim() ? Number(form.lastOdometerKm) : null;
    if (lastOdometerKm !== null && (!Number.isInteger(lastOdometerKm) || lastOdometerKm < 0)) {
      setError(t("errors.CAR_MAINTENANCE_BASELINE_INVALID"));
      return;
    }
    await onSubmit({
      name: form.name.trim(),
      intervalMonths,
      intervalThousandKm,
      lastDate: baselineLocked ? (item?.lastDate ?? null) : form.lastDate || null,
      lastOdometerKm: baselineLocked ? (item?.lastOdometerKm ?? null) : lastOdometerKm,
    });
  };

  const editing = Boolean(item);

  return (
    <Dialog open={open} onClose={busy ? undefined : onClose} fullWidth maxWidth="sm">
      <form onSubmit={submit} noValidate>
        <DialogTitle>{t(editing ? "actions.editMaintenance" : "actions.addMaintenance")}</DialogTitle>
        <DialogContent>
          <Grid container spacing={2} sx={{ pt: 1 }}>
            <Grid size={{ xs: 12 }}>
              <TextField autoFocus fullWidth required label={t("fields.name")} value={form.name} onChange={(event) => update("name", event.target.value)} />
            </Grid>
            <Grid size={{ xs: 12, sm: 6 }}>
              <TextField fullWidth type="number" label={t("fields.intervalMonths")} value={form.intervalMonths} onChange={(event) => update("intervalMonths", event.target.value)} slotProps={{ htmlInput: { min: 1, step: 1 } }} />
            </Grid>
            <Grid size={{ xs: 12, sm: 6 }}>
              <TextField fullWidth type="number" label={t("fields.intervalThousandKm")} value={form.intervalThousandKm} onChange={(event) => update("intervalThousandKm", event.target.value)} slotProps={{ htmlInput: { min: 1, step: 1 } }} />
            </Grid>
            <Grid size={{ xs: 12, sm: 6 }}>
              <TextField fullWidth type="date" label={t("fields.lastDate")} value={form.lastDate} onChange={(event) => update("lastDate", event.target.value)} slotProps={{ inputLabel: { shrink: true } }} disabled={baselineLocked} />
            </Grid>
            <Grid size={{ xs: 12, sm: 6 }}>
              <TextField fullWidth type="number" label={t("fields.lastOdometerKm")} value={form.lastOdometerKm} onChange={(event) => update("lastOdometerKm", event.target.value)} slotProps={{ htmlInput: { min: 0, step: 1 } }} disabled={baselineLocked} />
            </Grid>
            {baselineLocked ? <Grid size={{ xs: 12 }}><FormHelperText>{t("conflicts.baselineLocked")}</FormHelperText></Grid> : null}
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
