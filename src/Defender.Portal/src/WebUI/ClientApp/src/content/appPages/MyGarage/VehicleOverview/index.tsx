import { useCallback, useEffect, useState } from "react";
import { useTranslation } from "react-i18next";
import { useNavigate, useParams } from "react-router";
import {
  Alert,
  Box,
  Button,
  Card,
  CardContent,
  Chip,
  Grid,
  LinearProgress,
  Stack,
  Typography,
} from "@mui/material";
import ArrowBackIcon from "@mui/icons-material/ArrowBack";
import BuildOutlinedIcon from "@mui/icons-material/BuildOutlined";
import HistoryOutlinedIcon from "@mui/icons-material/HistoryOutlined";
import ShieldOutlinedIcon from "@mui/icons-material/ShieldOutlined";

import useUtils from "src/appUtils";
import { getVehicle, getHistory } from "src/api/myGarage";
import type { APICallFailure } from "src/api/APIWrapper/interfaces/APICallProps";
import { InsuranceStatus, type ServiceHistoryPage, type VehicleDetail } from "src/models/myGarage/CarModels";

import StatusBadge from "../components/StatusBadge";
import { getGarageFailureMessage } from "../helpers/status";
import { formatMinorCost } from "../helpers/money";

const formatDate = (value: string, locale: string) => new Intl.DateTimeFormat(locale).format(new Date(`${value}T00:00:00`));

export default function VehicleOverviewPage() {
  const { vehicleId } = useParams();
  const { t, i18n } = useTranslation("myGarage");
  const u = useUtils();
  const navigate = useNavigate();
  const [detail, setDetail] = useState<VehicleDetail | null>(null);
  const [history, setHistory] = useState<ServiceHistoryPage | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const locale = i18n.language === "ru" ? "ru-RU" : "en-US";

  const load = useCallback(async (signal?: AbortSignal) => {
    if (!vehicleId) return;
    setLoading(true);
    setError(null);
    try {
      const [nextDetail, nextHistory] = await Promise.all([
        getVehicle(vehicleId, null, signal),
        getHistory(vehicleId, 0, 5, null, signal),
      ]);
      setDetail(nextDetail);
      setHistory(nextHistory);
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

  if (loading) return <Box sx={{ p: { xs: 2, sm: 3, lg: 4 } }}><LinearProgress /></Box>;
  if (error || !detail) {
    return <Box sx={{ p: { xs: 2, sm: 3, lg: 4 } }}><Alert severity="error" action={<Button color="inherit" onClick={() => void load()}>{t("retry")}</Button>}>{error || t("errors.CAR_VEHICLE_NOT_FOUND")}</Alert></Box>;
  }

  const { vehicle, maintenanceItems, insurancePolicies } = detail;
  const historyItems = history?.items ?? [];
  const counts = maintenanceItems.reduce((result, item) => ({ ...result, [item.status]: result[item.status] + 1 }), { Overdue: 0, DueSoon: 0, Upcoming: 0, NotStarted: 0 });
  const insuranceStatus = insurancePolicies.find((policy) => policy.status === InsuranceStatus.Active)?.status
    ?? insurancePolicies.find((policy) => policy.status === InsuranceStatus.ExpiringSoon)?.status
    ?? insurancePolicies[0]?.status;

  return (
    <Box sx={{ p: { xs: 2, sm: 3, lg: 4 } }}>
      <Stack direction={{ xs: "column", sm: "row" }} spacing={2} sx={{ alignItems: { sm: "center" }, mb: 3 }}>
        <Button startIcon={<ArrowBackIcon />} onClick={() => navigate("/my-garage/vehicles")}>{t("actions.close")}</Button>
        <Box sx={{ flex: 1 }}><Typography component="h1" variant="h4" sx={{ overflowWrap: "anywhere" }}>{vehicle.displayName}</Typography><Typography color="text.secondary">{vehicle.make} {vehicle.model} · {vehicle.year} · {vehicle.plate}</Typography></Box>
        {vehicle.archived ? <Chip label={t("actions.includeArchived")} /> : null}
      </Stack>

      <Grid container spacing={2} sx={{ mb: 2 }}>
        <Grid size={{ xs: 12, sm: 6, lg: 3 }}><Card><CardContent><Typography color="text.secondary">{t("fields.currentOdometerKm")}</Typography><Typography variant="h5">{vehicle.currentOdometerKm === null ? "-" : `${vehicle.currentOdometerKm.toLocaleString(locale)} ${t("units.km")}`}</Typography></CardContent></Card></Grid>
        <Grid size={{ xs: 6, lg: 3 }}><Card><CardContent><Typography color="text.secondary">{t("statuses.Overdue")}</Typography><Typography variant="h5">{counts.Overdue}</Typography></CardContent></Card></Grid>
        <Grid size={{ xs: 6, lg: 3 }}><Card><CardContent><Typography color="text.secondary">{t("statuses.DueSoon")}</Typography><Typography variant="h5">{counts.DueSoon}</Typography></CardContent></Card></Grid>
        <Grid size={{ xs: 12, lg: 3 }}><Card><CardContent><Typography color="text.secondary">{t("fields.provider")}</Typography>{insuranceStatus ? <StatusBadge status={insuranceStatus} /> : <Typography>{t("empty.insurance")}</Typography>}</CardContent></Card></Grid>
      </Grid>

      <Grid container spacing={2}>
        <Grid size={{ xs: 12, lg: 4 }}>
          <Card sx={{ height: "100%" }}><CardContent><Stack direction="row" sx={{ justifyContent: "space-between", alignItems: "center", mb: 1 }}><Typography variant="h6">{t("fields.name")}</Typography><Button size="small" startIcon={<BuildOutlinedIcon />} onClick={() => navigate(`/my-garage/vehicles/${vehicle.id}/maintenance`)}>{t("actions.addMaintenance")}</Button></Stack>{maintenanceItems.length === 0 ? <Typography color="text.secondary">{t("empty.maintenance")}</Typography> : <Stack spacing={1}>{maintenanceItems.slice(0, 5).map((item) => <Stack key={item.id} direction="row" sx={{ justifyContent: "space-between", gap: 1 }}><Typography sx={{ overflowWrap: "anywhere" }}>{item.name}</Typography><StatusBadge status={item.status} /></Stack>)}</Stack>}</CardContent></Card>
        </Grid>
        <Grid size={{ xs: 12, lg: 4 }}>
          <Card sx={{ height: "100%" }}><CardContent><Stack direction="row" sx={{ justifyContent: "space-between", alignItems: "center", mb: 1 }}><Typography variant="h6">{t("fields.date")}</Typography><Button size="small" startIcon={<HistoryOutlinedIcon />} onClick={() => navigate(`/my-garage/vehicles/${vehicle.id}/history`)}>{t("actions.recordService")}</Button></Stack>{historyItems.length === 0 ? <Typography color="text.secondary">{t("empty.overviewHistory")}</Typography> : <Stack spacing={1}>{historyItems.slice(0, 5).map((record) => <Box key={record.id}><Typography sx={{ fontWeight: 700 }}>{record.title}</Typography><Typography variant="body2" color="text.secondary">{formatDate(record.date, locale)} · {record.odometerKm.toLocaleString(locale)} {t("units.km")}{record.costAmountMinor !== null ? ` · ${formatMinorCost(record.costAmountMinor, record.costCurrency)}` : ""}</Typography></Box>)}</Stack>}</CardContent></Card>
        </Grid>
        <Grid size={{ xs: 12, lg: 4 }}>
          <Card sx={{ height: "100%" }}><CardContent><Stack direction="row" sx={{ justifyContent: "space-between", alignItems: "center", mb: 1 }}><Typography variant="h6">{t("fields.provider")}</Typography><Button size="small" startIcon={<ShieldOutlinedIcon />} onClick={() => navigate(`/my-garage/vehicles/${vehicle.id}/insurance`)}>{t("actions.addInsurance")}</Button></Stack>{insurancePolicies.length === 0 ? <Typography color="text.secondary">{t("empty.insurance")}</Typography> : <Stack spacing={1}>{insurancePolicies.slice(0, 5).map((policy) => <Stack key={policy.id} direction="row" sx={{ justifyContent: "space-between", gap: 1 }}><Typography sx={{ overflowWrap: "anywhere" }}>{policy.provider}</Typography><StatusBadge status={policy.status} /></Stack>)}</Stack>}</CardContent></Card>
        </Grid>
      </Grid>

      <Stack direction={{ xs: "column", sm: "row" }} spacing={1} sx={{ mt: 3 }}>
        <Button variant="outlined" onClick={() => navigate(`/my-garage/vehicles/${vehicle.id}/maintenance`)}>{t("actions.addMaintenance")}</Button>
        <Button variant="outlined" onClick={() => navigate(`/my-garage/vehicles/${vehicle.id}/history`)}>{t("actions.recordService")}</Button>
        <Button variant="outlined" onClick={() => navigate(`/my-garage/vehicles/${vehicle.id}/insurance`)}>{t("actions.addInsurance")}</Button>
      </Stack>
    </Box>
  );
}
