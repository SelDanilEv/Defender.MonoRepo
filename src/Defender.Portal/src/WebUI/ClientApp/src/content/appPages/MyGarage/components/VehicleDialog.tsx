import { useEffect, useState } from "react";
import {
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Grid,
  TextField,
} from "@mui/material";
import { useTranslation } from "react-i18next";

import type { CreateVehicleRequest } from "src/models/myGarage/CarRequests";
import type { Vehicle, VehicleSummary } from "src/models/myGarage/CarModels";

interface VehicleDialogProps {
  open: boolean;
  vehicle?: Vehicle | VehicleSummary | null;
  busy?: boolean;
  submitError?: string | null;
  onClose: () => void;
  onSubmit: (request: CreateVehicleRequest) => Promise<void> | void;
}

const currentYear = new Date().getUTCFullYear();

export default function VehicleDialog({
  open,
  vehicle,
  busy = false,
  submitError,
  onClose,
  onSubmit,
}: VehicleDialogProps) {
  const { t } = useTranslation("myGarage");
  const [form, setForm] = useState<CreateVehicleRequest>({ displayName: "", make: "", model: "", year: currentYear, plate: "", vin: null });
  const [fieldError, setFieldError] = useState<string | null>(null);

  useEffect(() => {
    setForm({
      displayName: vehicle?.displayName ?? "",
      make: vehicle?.make ?? "",
      model: vehicle?.model ?? "",
      year: vehicle?.year ?? currentYear,
      plate: vehicle?.plate ?? "",
      vin: vehicle?.vin ?? null,
    });
    setFieldError(null);
  }, [vehicle, open]);

  const update = (field: keyof CreateVehicleRequest, value: string | number | null) => {
    setForm((current) => ({ ...current, [field]: value }));
    setFieldError(null);
  };

  const submit = async (event: React.FormEvent) => {
    event.preventDefault();
    if (!form.displayName.trim() || !form.make.trim() || !form.model.trim() || !form.plate.trim()) {
      setFieldError(t("validation.required"));
      return;
    }
    if (!Number.isInteger(form.year) || form.year < 1886 || form.year > currentYear + 1) {
      setFieldError(t("validation.vehicleYear"));
      return;
    }
    await onSubmit({ ...form, displayName: form.displayName.trim(), make: form.make.trim(), model: form.model.trim(), plate: form.plate.trim(), vin: form.vin?.trim() || null });
  };

  const editing = Boolean(vehicle);

  return (
    <Dialog open={open} onClose={busy ? undefined : onClose} fullWidth maxWidth="sm">
      <form onSubmit={submit} noValidate>
        <DialogTitle>{t(editing ? "actions.editVehicle" : "actions.addVehicle")}</DialogTitle>
        <DialogContent>
          <Grid container spacing={2} sx={{ pt: 1 }}>
            <Grid size={{ xs: 12 }}>
              <TextField autoFocus fullWidth required label={t("fields.displayName")} value={form.displayName} onChange={(event) => update("displayName", event.target.value)} error={Boolean(fieldError)} />
            </Grid>
            <Grid size={{ xs: 12, sm: 6 }}>
              <TextField fullWidth required label={t("fields.make")} value={form.make} onChange={(event) => update("make", event.target.value)} />
            </Grid>
            <Grid size={{ xs: 12, sm: 6 }}>
              <TextField fullWidth required label={t("fields.model")} value={form.model} onChange={(event) => update("model", event.target.value)} />
            </Grid>
            <Grid size={{ xs: 12, sm: 6 }}>
              <TextField fullWidth required type="number" label={t("fields.year")} value={form.year} onChange={(event) => update("year", Number(event.target.value))} error={Boolean(fieldError && (form.year < 1886 || form.year > currentYear + 1))} />
            </Grid>
            <Grid size={{ xs: 12, sm: 6 }}>
              <TextField fullWidth required label={t("fields.plate")} value={form.plate} onChange={(event) => update("plate", event.target.value)} />
            </Grid>
            <Grid size={{ xs: 12 }}>
              <TextField fullWidth label={t("fields.vin")} value={form.vin ?? ""} onChange={(event) => update("vin", event.target.value)} slotProps={{ htmlInput: { maxLength: 17 } }} />
            </Grid>
            {fieldError || submitError ? <Grid size={{ xs: 12 }}><div role="alert">{submitError || fieldError}</div></Grid> : null}
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
