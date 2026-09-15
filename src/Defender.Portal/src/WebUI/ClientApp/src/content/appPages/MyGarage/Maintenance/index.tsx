import { useCallback, useEffect, useState } from "react";
import { useTranslation } from "react-i18next";
import { useNavigate, useParams } from "react-router";
import {
  Alert,
  Box,
  Button,
  Card,
  CardContent,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Grid,
  LinearProgress,
  Stack,
  TableCell,
  TableRow,
  Typography,
} from "@mui/material";
import ArrowBackIcon from "@mui/icons-material/ArrowBack";
import AddIcon from "@mui/icons-material/Add";
import DeleteOutlineIcon from "@mui/icons-material/DeleteOutlined";
import EditOutlinedIcon from "@mui/icons-material/EditOutlined";

import { getVehicle, createMaintenanceItem, updateMaintenanceItem, deleteMaintenanceItem } from "src/api/myGarage";
import type { APICallFailure } from "src/api/APIWrapper/interfaces/APICallProps";
import type { CreateMaintenanceItemRequest } from "src/models/myGarage/CarRequests";
import type { MaintenanceItem, VehicleDetail } from "src/models/myGarage/CarModels";
import SuccessToast from "src/components/Toast/DefaultSuccessToast";

import GarageTable from "../components/GarageTable";
import MaintenanceDialog from "../components/MaintenanceDialog";
import StatusBadge from "../components/StatusBadge";
import { getGarageFailureMessage } from "../helpers/status";

const formatDate = (value: string | null, locale: string) => value ? new Intl.DateTimeFormat(locale).format(new Date(`${value}T00:00:00`)) : "-";

export default function MaintenancePage() {
  const { vehicleId } = useParams();
  const { t, i18n } = useTranslation("myGarage");
  const navigate = useNavigate();
  const [detail, setDetail] = useState<VehicleDetail | null>(null);
  const [loading, setLoading] = useState(true);
  const [mutating, setMutating] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [selectedItem, setSelectedItem] = useState<MaintenanceItem | null>(null);
  const [dialogOpen, setDialogOpen] = useState(false);
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [deleteTarget, setDeleteTarget] = useState<MaintenanceItem | null>(null);
  const locale = i18n.language === "ru" ? "ru-RU" : "en-US";

  const load = useCallback(async (signal?: AbortSignal) => {
    if (!vehicleId) return;
    setLoading(true);
    setError(null);
    try {
      setDetail(await getVehicle(vehicleId, null, signal));
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

  const openCreate = () => {
    setSelectedItem(null);
    setSubmitError(null);
    setDialogOpen(true);
  };

  const openEdit = (item: MaintenanceItem) => {
    setSelectedItem(item);
    setSubmitError(null);
    setDialogOpen(true);
  };

  const submit = async (request: CreateMaintenanceItemRequest) => {
    if (!vehicleId) return;
    setMutating(true);
    setSubmitError(null);
    try {
      if (selectedItem) {
        await updateMaintenanceItem(vehicleId, selectedItem.id, request, null);
        await SuccessToast(t("success.maintenanceUpdated"));
      } else {
        await createMaintenanceItem(vehicleId, request, null);
        await SuccessToast(t("success.maintenanceCreated"));
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

  const remove = async () => {
    if (!vehicleId || !deleteTarget) return;
    setMutating(true);
    try {
      await deleteMaintenanceItem(vehicleId, deleteTarget.id, null);
      await SuccessToast(t("success.maintenanceDeleted"));
      setDeleteTarget(null);
      await load();
    } catch (failure) {
      const typed = failure as APICallFailure;
      setError(getGarageFailureMessage(typed, t));
      setDeleteTarget(null);
      if (typed.status === 409) await load();
    } finally {
      setMutating(false);
    }
  };

  if (loading) return <Box sx={{ p: { xs: 2, sm: 3, lg: 4 } }}><LinearProgress /></Box>;
  if (error || !detail || !vehicleId) return <Box sx={{ p: { xs: 2, sm: 3, lg: 4 } }}><Alert severity="error" action={<Button color="inherit" onClick={() => void load()}>{t("retry")}</Button>}>{error || t("errors.CAR_VEHICLE_NOT_FOUND")}</Alert></Box>;

  const { vehicle, maintenanceItems } = detail;
  const readOnly = vehicle.archived;

  return (
    <Box sx={{ p: { xs: 2, sm: 3, lg: 4 } }}>
      <Stack direction={{ xs: "column", sm: "row" }} spacing={2} sx={{ alignItems: { sm: "center" }, mb: 3 }}>
        <Button startIcon={<ArrowBackIcon />} onClick={() => navigate(`/my-garage/vehicles/${vehicle.id}`)} disabled={mutating}>{vehicle.displayName}</Button>
        <Box sx={{ flex: 1 }}><Typography component="h1" variant="h4">{t("actions.addMaintenance")}</Typography><Typography color="text.secondary">{vehicle.make} {vehicle.model}</Typography></Box>
        <Button variant="contained" startIcon={<AddIcon />} onClick={openCreate} disabled={mutating || readOnly} aria-label={t("actions.addMaintenance")}>{t("actions.addMaintenance")}</Button>
      </Stack>
      {readOnly ? <Alert severity="info" sx={{ mb: 2 }}>{t("conflicts.vehicleArchived")}</Alert> : null}
      {error ? <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert> : null}
      <Card><CardContent sx={{ p: 0, "&:last-child": { pb: 0 } }}>
        <GarageTable ariaLabel={t("actions.addMaintenance")} headers={[t("fields.name"), t("fields.intervalMonths"), t("fields.intervalThousandKm"), t("fields.lastDate"), t("fields.nextDate"), t("fields.status"), t("table_actions_column")]} empty={maintenanceItems.length === 0} emptyMessage={t("empty.maintenance")}>
          {maintenanceItems.map((item) => {
            const referenced = item.hasLinkedHistory;
            return <TableRow hover key={item.id}>
              <TableCell><Typography sx={{ fontWeight: 700, overflowWrap: "anywhere" }}>{item.name}</Typography></TableCell>
              <TableCell>{item.intervalMonths ?? "-"}</TableCell>
              <TableCell>{item.intervalThousandKm ?? "-"}</TableCell>
              <TableCell>{formatDate(item.lastDate, locale)}</TableCell>
              <TableCell>{formatDate(item.nextDate, locale)}{item.nextOdometerKm == null ? "" : ` · ${item.nextOdometerKm.toLocaleString(locale)} ${t("units.km")}`}</TableCell>
              <TableCell><StatusBadge status={item.status} /></TableCell>
              <TableCell><Stack direction="row" spacing={0.5}><Button size="small" onClick={() => openEdit(item)} disabled={mutating || readOnly} aria-label={`${t("actions.editMaintenance")}: ${item.name}`}><EditOutlinedIcon fontSize="small" /></Button><Button size="small" color="error" onClick={() => setDeleteTarget(item)} disabled={mutating || readOnly || referenced} title={referenced ? t("conflicts.maintenanceReferenced") : undefined} aria-label={`${t("actions.deleteMaintenance")}: ${item.name}`}><DeleteOutlineIcon fontSize="small" /></Button></Stack></TableCell>
            </TableRow>;
          })}
        </GarageTable>
      </CardContent></Card>

      <MaintenanceDialog open={dialogOpen} item={selectedItem} baselineLocked={Boolean(selectedItem?.hasLinkedHistory)} busy={mutating} submitError={submitError} onClose={() => setDialogOpen(false)} onSubmit={submit} />
      <Dialog open={Boolean(deleteTarget)} onClose={mutating ? undefined : () => setDeleteTarget(null)}>
        <DialogTitle>{t("actions.deleteMaintenance")}</DialogTitle>
        <DialogContent><Typography>{deleteTarget?.name}</Typography></DialogContent>
        <DialogActions><Button onClick={() => setDeleteTarget(null)} disabled={mutating}>{t("actions.cancel")}</Button><Button color="error" variant="contained" onClick={() => void remove()} disabled={mutating}>{t("actions.deleteMaintenance")}</Button></DialogActions>
      </Dialog>
    </Box>
  );
}
