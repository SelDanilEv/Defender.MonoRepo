import { useCallback, useEffect, useState } from "react";
import { useTranslation } from "react-i18next";
import { useNavigate } from "react-router";
import {
  Alert,
  Box,
  Button,
  Card,
  CardContent,
  Chip,
  FormControlLabel,
  Grid,
  LinearProgress,
  Stack,
  Switch,
  TableCell,
  TableRow,
  Typography,
} from "@mui/material";
import AddIcon from "@mui/icons-material/Add";
import ArchiveOutlinedIcon from "@mui/icons-material/ArchiveOutlined";
import EditOutlinedIcon from "@mui/icons-material/EditOutlined";
import OpenInNewOutlinedIcon from "@mui/icons-material/OpenInNewOutlined";
import UnarchiveOutlinedIcon from "@mui/icons-material/UnarchiveOutlined";

import SuccessToast from "src/components/Toast/DefaultSuccessToast";
import { getVehicles, createVehicle, updateVehicle, archiveVehicle, unarchiveVehicle } from "src/api/myGarage";
import type { APICallFailure } from "src/api/APIWrapper/interfaces/APICallProps";
import type { CreateVehicleRequest } from "src/models/myGarage/CarRequests";
import type { VehicleSummary } from "src/models/myGarage/CarModels";

import GarageTable from "../components/GarageTable";
import VehicleDialog from "../components/VehicleDialog";
import StatusBadge from "../components/StatusBadge";
import { getGarageFailureMessage } from "../helpers/status";

const formatKm = (value: number | null, locale: string, unit: string) =>
  value === null ? "-" : `${value.toLocaleString(locale)} ${unit}`;

export default function VehiclesPage() {
  const { t, i18n } = useTranslation("myGarage");
  const navigate = useNavigate();
  const [vehicles, setVehicles] = useState<VehicleSummary[]>([]);
  const [includeArchived, setIncludeArchived] = useState(false);
  const [loading, setLoading] = useState(true);
  const [mutating, setMutating] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [dialogOpen, setDialogOpen] = useState(false);
  const [selectedVehicle, setSelectedVehicle] = useState<VehicleSummary | null>(null);
  const [submitError, setSubmitError] = useState<string | null>(null);
  const locale = i18n.language === "ru" ? "ru-RU" : "en-US";

  const load = useCallback(async (signal?: AbortSignal) => {
    setLoading(true);
    setError(null);
    try {
      setVehicles(await getVehicles(includeArchived, null, signal));
    } catch (failure) {
      if (signal?.aborted) return;
      setError(getGarageFailureMessage(failure as APICallFailure, t));
    } finally {
      if (!signal?.aborted) setLoading(false);
    }
  }, [includeArchived, t]);

  useEffect(() => {
    const controller = new AbortController();
    void load(controller.signal);
    return () => controller.abort();
  }, [load]);

  const openCreate = () => {
    setSelectedVehicle(null);
    setSubmitError(null);
    setDialogOpen(true);
  };

  const openEdit = (vehicle: VehicleSummary) => {
    setSelectedVehicle(vehicle);
    setSubmitError(null);
    setDialogOpen(true);
  };

  const submitVehicle = async (request: CreateVehicleRequest) => {
    setMutating(true);
    setSubmitError(null);
    try {
      if (selectedVehicle) {
        await updateVehicle(selectedVehicle.id, request, null);
        await SuccessToast(t("success.vehicleUpdated"));
      } else {
        await createVehicle(request, null);
        await SuccessToast(t("success.vehicleCreated"));
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

  const toggleArchive = async (vehicle: VehicleSummary) => {
    const action = vehicle.archived ? t("actions.unarchiveVehicle") : t("actions.archiveVehicle");
    if (!window.confirm(`${action}: ${vehicle.displayName}?`)) return;
    setMutating(true);
    setError(null);
    try {
      if (vehicle.archived) {
        await unarchiveVehicle(vehicle.id, null);
        await SuccessToast(t("success.vehicleUnarchived"));
      } else {
        await archiveVehicle(vehicle.id, null);
        await SuccessToast(t("success.vehicleArchived"));
      }
      await load();
    } catch (failure) {
      const typed = failure as APICallFailure;
      setError(getGarageFailureMessage(typed, t));
      if (typed.status === 409) await load();
    } finally {
      setMutating(false);
    }
  };

  return (
    <Box sx={{ p: { xs: 2, sm: 3, lg: 4 } }}>
      <Stack direction={{ xs: "column", sm: "row" }} spacing={2} sx={{ alignItems: { sm: "center" }, mb: 3 }}>
        <Box sx={{ flex: 1 }}>
          <Typography component="h1" variant="h4">{t("title")}</Typography>
          <Typography color="text.secondary">{t("subtitle")}</Typography>
        </Box>
        <Stack direction={{ xs: "column", sm: "row" }} spacing={1} sx={{ alignItems: { sm: "center" } }}>
          <FormControlLabel control={<Switch checked={includeArchived} onChange={(event) => setIncludeArchived(event.target.checked)} disabled={mutating} slotProps={{ input: { "aria-label": t("actions.includeArchived") } }} />} label={t("actions.includeArchived")} />
          <Button variant="contained" startIcon={<AddIcon />} onClick={openCreate} disabled={mutating} aria-label={t("actions.addVehicle")}>{t("actions.addVehicle")}</Button>
        </Stack>
      </Stack>

      {error ? <Alert severity="error" sx={{ mb: 2 }} action={<Button color="inherit" onClick={() => void load()}>{t("retry")}</Button>}>{error}</Alert> : null}
      {loading ? <LinearProgress sx={{ mb: 2 }} /> : null}

      <Card>
        <CardContent sx={{ p: 0, "&:last-child": { pb: 0 } }}>
          <GarageTable
            ariaLabel={t("title")}
            headers={[
              t("fields.displayName"),
               t("fields.make"),
               t("fields.model"),
               t("fields.currentOdometerKm"),
               t("fields.status"),
               t("fields.provider"),
               t("table_actions_column"),
            ]}
            empty={!loading && vehicles.length === 0}
            emptyMessage={t("empty.vehicles")}
            loading={loading}
          >
            {vehicles.map((vehicle) => (
              <TableRow hover key={vehicle.id}>
                <TableCell>
                  <Typography sx={{ fontWeight: 700, overflowWrap: "anywhere" }} title={vehicle.displayName}>{vehicle.displayName}</Typography>
                  <Typography variant="caption" color="text.secondary">{vehicle.year} · {vehicle.plate}</Typography>
                  {vehicle.archived ? <Chip size="small" label={t("actions.includeArchived")} sx={{ ml: 1 }} /> : null}
                </TableCell>
                <TableCell>{vehicle.make}</TableCell>
                <TableCell>{vehicle.model}</TableCell>
                <TableCell>{formatKm(vehicle.currentOdometerKm, locale, t("units.km"))}</TableCell>
                <TableCell><Stack direction="row" spacing={0.5} sx={{ flexWrap: "wrap" }}><Chip size="small" color="error" label={`${t("statuses.Overdue")}: ${vehicle.maintenanceCounts.overdue}`} /><Chip size="small" color="warning" label={`${t("statuses.DueSoon")}: ${vehicle.maintenanceCounts.dueSoon}`} /><Chip size="small" color="success" label={`${t("statuses.Upcoming")}: ${vehicle.maintenanceCounts.upcoming}`} /><Chip size="small" label={`${t("statuses.NotStarted")}: ${vehicle.maintenanceCounts.notStarted}`} /></Stack></TableCell>
                <TableCell>{vehicle.insuranceStatus ? <StatusBadge status={vehicle.insuranceStatus} /> : "-"}</TableCell>
                <TableCell>
                  <Stack direction="row" spacing={0.25}>
                    <Button size="small" startIcon={<OpenInNewOutlinedIcon />} onClick={() => navigate(`/my-garage/vehicles/${vehicle.id}`)} disabled={mutating} aria-label={`${t("actions.openVehicle")}: ${vehicle.displayName}`}>{t("actions.openVehicle")}</Button>
                    <Button size="small" onClick={() => openEdit(vehicle)} disabled={mutating} aria-label={`${t("actions.editVehicle")}: ${vehicle.displayName}`}><EditOutlinedIcon fontSize="small" /></Button>
                    <Button size="small" color={vehicle.archived ? "success" : "warning"} onClick={() => void toggleArchive(vehicle)} disabled={mutating} aria-label={`${vehicle.archived ? t("actions.unarchiveVehicle") : t("actions.archiveVehicle")}: ${vehicle.displayName}`}>{vehicle.archived ? <UnarchiveOutlinedIcon fontSize="small" /> : <ArchiveOutlinedIcon fontSize="small" />}</Button>
                  </Stack>
                </TableCell>
              </TableRow>
            ))}
          </GarageTable>
        </CardContent>
      </Card>

      <VehicleDialog open={dialogOpen} vehicle={selectedVehicle} busy={mutating} submitError={submitError} onClose={() => setDialogOpen(false)} onSubmit={submitVehicle} />
    </Box>
  );
}
