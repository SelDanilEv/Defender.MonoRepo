import { useCallback, useEffect, useMemo, useState } from "react";
import { useTranslation } from "react-i18next";
import { useNavigate, useParams } from "react-router";
import {
  Alert,
  Box,
  Button,
  Card,
  CardContent,
  Grid,
  LinearProgress,
  Stack,
  TableCell,
  TableRow,
  Typography,
} from "@mui/material";
import AddIcon from "@mui/icons-material/Add";
import ArrowBackIcon from "@mui/icons-material/ArrowBack";
import EditOutlinedIcon from "@mui/icons-material/EditOutlined";

import { getVehicle, getInsurancePolicies, createInsurancePolicy, updateInsurancePolicy } from "src/api/myGarage";
import type { APICallFailure } from "src/api/APIWrapper/interfaces/APICallProps";
import type { CreateInsurancePolicyRequest } from "src/models/myGarage/CarRequests";
import type { InsurancePolicy, VehicleDetail } from "src/models/myGarage/CarModels";
import SuccessToast from "src/components/Toast/DefaultSuccessToast";

import GarageTable from "../components/GarageTable";
import InsuranceDialog from "../components/InsuranceDialog";
import StatusBadge from "../components/StatusBadge";
import { getGarageFailureMessage } from "../helpers/status";

const formatDate = (value: string, locale: string) => new Intl.DateTimeFormat(locale).format(new Date(`${value}T00:00:00`));

export default function InsurancePage() {
  const { vehicleId } = useParams();
  const { t, i18n } = useTranslation("myGarage");
  const navigate = useNavigate();
  const [detail, setDetail] = useState<VehicleDetail | null>(null);
  const [policies, setPolicies] = useState<InsurancePolicy[]>([]);
  const [loading, setLoading] = useState(true);
  const [mutating, setMutating] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [dialogOpen, setDialogOpen] = useState(false);
  const [selectedPolicy, setSelectedPolicy] = useState<InsurancePolicy | null>(null);
  const [submitError, setSubmitError] = useState<string | null>(null);
  const locale = i18n.language === "ru" ? "ru-RU" : "en-US";

  const load = useCallback(async (signal?: AbortSignal) => {
    if (!vehicleId) return;
    setLoading(true);
    setError(null);
    try {
      const [nextDetail, nextPolicies] = await Promise.all([
        getVehicle(vehicleId, null, signal),
        getInsurancePolicies(vehicleId, null, signal),
      ]);
      setDetail(nextDetail);
      setPolicies(nextPolicies);
    } catch (failure) {
      if (!signal?.aborted) setError(getGarageFailureMessage(failure as APICallFailure, t));
    } finally {
      if (!signal?.aborted) setLoading(false);
    }
  }, [t, vehicleId]);

  useEffect(() => {
    const controller = new AbortController();
    void load(controller.signal);
    return () => controller.abort();
  }, [load]);

  const sortedPolicies = useMemo(() => [...policies].sort((left, right) => right.endDate.localeCompare(left.endDate)), [policies]);

  const openCreate = () => {
    setSelectedPolicy(null);
    setSubmitError(null);
    setDialogOpen(true);
  };

  const openEdit = (policy: InsurancePolicy) => {
    setSelectedPolicy(policy);
    setSubmitError(null);
    setDialogOpen(true);
  };

  const submit = async (request: CreateInsurancePolicyRequest) => {
    if (!vehicleId) return;
    setMutating(true);
    setSubmitError(null);
    try {
      if (selectedPolicy) {
        await updateInsurancePolicy(vehicleId, selectedPolicy.id, request, null);
        await SuccessToast(t("success.insuranceUpdated"));
      } else {
        await createInsurancePolicy(vehicleId, request, null);
        await SuccessToast(t("success.insuranceCreated"));
      }
      setDialogOpen(false);
      await load();
    } catch (failure) {
      const typed = failure as APICallFailure;
      setSubmitError(getGarageFailureMessage(typed, t));
      if (typed.status === 409) await load();
    } finally {
      setMutating(false);
    }
  };

  if (loading) return <Box sx={{ p: { xs: 2, sm: 3, lg: 4 } }}><LinearProgress /></Box>;
  if (error || !detail || !vehicleId) return <Box sx={{ p: { xs: 2, sm: 3, lg: 4 } }}><Alert severity="error" action={<Button color="inherit" onClick={() => void load()}>{t("retry")}</Button>}>{error || t("errors.CAR_VEHICLE_NOT_FOUND")}</Alert></Box>;

  const readOnly = detail.vehicle.archived;

  return (
    <Box sx={{ p: { xs: 2, sm: 3, lg: 4 } }}>
      <Stack direction={{ xs: "column", sm: "row" }} spacing={2} sx={{ alignItems: { sm: "center" }, mb: 3 }}>
        <Button startIcon={<ArrowBackIcon />} onClick={() => navigate(`/my-garage/vehicles/${vehicleId}`)} disabled={mutating}>{detail.vehicle.displayName}</Button>
        <Box sx={{ flex: 1 }}><Typography component="h1" variant="h4">{t("actions.addInsurance")}</Typography><Typography color="text.secondary">{detail.vehicle.make} {detail.vehicle.model}</Typography></Box>
        <Button variant="contained" startIcon={<AddIcon />} onClick={openCreate} disabled={mutating || readOnly} aria-label={t("actions.addInsurance")}>{t("actions.addInsurance")}</Button>
      </Stack>
      {readOnly ? <Alert severity="info" sx={{ mb: 2 }}>{t("conflicts.vehicleArchived")}</Alert> : null}
      {error ? <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert> : null}
      <Card><CardContent sx={{ p: 0, "&:last-child": { pb: 0 } }}>
        <GarageTable ariaLabel={t("actions.addInsurance")} headers={[t("fields.provider"), t("fields.policyNumber"), t("fields.coverageType"), t("fields.startDate"), t("fields.endDate"), t("fields.status"), t("table_actions_column")]} empty={sortedPolicies.length === 0} emptyMessage={t("empty.insurance")}>
          {sortedPolicies.map((policy) => <TableRow hover key={policy.id}>
            <TableCell><Typography sx={{ fontWeight: 700, overflowWrap: "anywhere" }}>{policy.provider}</Typography>{policy.notes ? <Typography variant="caption" color="text.secondary" sx={{ display: "block", overflowWrap: "anywhere" }}>{policy.notes}</Typography> : null}</TableCell>
            <TableCell>{policy.policyNumber || "-"}</TableCell>
            <TableCell>{policy.coverageType || "-"}</TableCell>
            <TableCell>{formatDate(policy.startDate, locale)}</TableCell>
            <TableCell>{formatDate(policy.endDate, locale)}</TableCell>
            <TableCell><StatusBadge status={policy.status} /></TableCell>
            <TableCell><Button size="small" onClick={() => openEdit(policy)} disabled={mutating || readOnly} aria-label={`${t("actions.editInsurance")}: ${policy.provider}`}><EditOutlinedIcon fontSize="small" /></Button></TableCell>
          </TableRow>)}
        </GarageTable>
      </CardContent></Card>

      <InsuranceDialog open={dialogOpen} policy={selectedPolicy} busy={mutating} submitError={submitError} onClose={() => setDialogOpen(false)} onSubmit={submit} />
    </Box>
  );
}
