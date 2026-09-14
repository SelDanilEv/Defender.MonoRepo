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

import type { CreateInsurancePolicyRequest } from "src/models/myGarage/CarRequests";
import type { InsurancePolicy } from "src/models/myGarage/CarModels";

interface InsuranceDialogProps {
  open: boolean;
  policy?: InsurancePolicy | null;
  busy?: boolean;
  submitError?: string | null;
  onClose: () => void;
  onSubmit: (request: CreateInsurancePolicyRequest) => Promise<void> | void;
}

interface InsuranceForm {
  provider: string;
  policyNumber: string;
  coverageType: string;
  startDate: string;
  endDate: string;
  notes: string;
}

const emptyForm: InsuranceForm = { provider: "", policyNumber: "", coverageType: "", startDate: "", endDate: "", notes: "" };

export default function InsuranceDialog({
  open,
  policy,
  busy = false,
  submitError,
  onClose,
  onSubmit,
}: InsuranceDialogProps) {
  const { t } = useTranslation("myGarage");
  const [form, setForm] = useState<InsuranceForm>(emptyForm);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    setForm(policy ? {
      provider: policy.provider,
      policyNumber: policy.policyNumber ?? "",
      coverageType: policy.coverageType ?? "",
      startDate: policy.startDate,
      endDate: policy.endDate,
      notes: policy.notes ?? "",
    } : emptyForm);
    setError(null);
  }, [policy, open]);

  const update = (field: keyof InsuranceForm, value: string) => {
    setForm((current) => ({ ...current, [field]: value }));
    setError(null);
  };

  const submit = async (event: React.FormEvent) => {
    event.preventDefault();
    if (!form.provider.trim()) {
      setError(t("errors.CAR_INSURANCE_PROVIDER_REQUIRED"));
      return;
    }
    if (!form.startDate || !form.endDate || form.startDate > form.endDate) {
      setError(t("validation.insuranceDateRange"));
      return;
    }
    await onSubmit({ provider: form.provider.trim(), policyNumber: form.policyNumber.trim() || null, coverageType: form.coverageType.trim() || null, startDate: form.startDate, endDate: form.endDate, notes: form.notes.trim() || null });
  };

  const editing = Boolean(policy);

  return (
    <Dialog open={open} onClose={busy ? undefined : onClose} fullWidth maxWidth="sm">
      <form onSubmit={submit} noValidate>
        <DialogTitle>{t(editing ? "actions.editInsurance" : "actions.addInsurance")}</DialogTitle>
        <DialogContent>
          <Grid container spacing={2} sx={{ pt: 1 }}>
            <Grid size={{ xs: 12 }}><TextField autoFocus fullWidth required label={t("fields.provider")} value={form.provider} onChange={(event) => update("provider", event.target.value)} disabled={busy} /></Grid>
            <Grid size={{ xs: 12, sm: 6 }}><TextField fullWidth label={t("fields.policyNumber")} value={form.policyNumber} onChange={(event) => update("policyNumber", event.target.value)} disabled={busy} /></Grid>
            <Grid size={{ xs: 12, sm: 6 }}><TextField fullWidth label={t("fields.coverageType")} value={form.coverageType} onChange={(event) => update("coverageType", event.target.value)} disabled={busy} /></Grid>
            <Grid size={{ xs: 12, sm: 6 }}><TextField fullWidth required type="date" label={t("fields.startDate")} value={form.startDate} onChange={(event) => update("startDate", event.target.value)} slotProps={{ inputLabel: { shrink: true } }} disabled={busy} /></Grid>
            <Grid size={{ xs: 12, sm: 6 }}><TextField fullWidth required type="date" label={t("fields.endDate")} value={form.endDate} onChange={(event) => update("endDate", event.target.value)} slotProps={{ inputLabel: { shrink: true } }} disabled={busy} /></Grid>
            <Grid size={{ xs: 12 }}><TextField fullWidth multiline minRows={2} label={t("fields.notes")} value={form.notes} onChange={(event) => update("notes", event.target.value)} disabled={busy} /></Grid>
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
