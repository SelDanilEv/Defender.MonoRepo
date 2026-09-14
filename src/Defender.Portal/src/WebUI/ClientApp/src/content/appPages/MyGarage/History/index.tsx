import { useCallback, useEffect, useMemo, useState } from "react";
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
  FormControl,
  Grid,
  InputLabel,
  LinearProgress,
  MenuItem,
  Select,
  Stack,
  TableCell,
  TablePagination,
  TableRow,
  TextField,
  Typography,
} from "@mui/material";
import AddIcon from "@mui/icons-material/Add";
import ArrowBackIcon from "@mui/icons-material/ArrowBack";
import DeleteOutlineIcon from "@mui/icons-material/DeleteOutlined";
import EditOutlinedIcon from "@mui/icons-material/EditOutlined";

import { getVehicle, getHistory, createHistory, updateHistory, deleteHistory } from "src/api/myGarage";
import type { APICallFailure } from "src/api/APIWrapper/interfaces/APICallProps";
import type { CreateServiceHistoryRequest } from "src/models/myGarage/CarRequests";
import { HistoryType, type ServiceHistoryPage, type ServiceHistoryRecord, type VehicleDetail } from "src/models/myGarage/CarModels";
import SuccessToast from "src/components/Toast/DefaultSuccessToast";

import GarageTable from "../components/GarageTable";
import HistoryDialog from "../components/HistoryDialog";
import { getLinkedMaintenanceLabels } from "../helpers/historySelection";
import { formatMinorCost } from "../helpers/money";
import { getGarageFailureMessage } from "../helpers/status";

const formatDate = (value: string, locale: string) => new Intl.DateTimeFormat(locale).format(new Date(`${value}T00:00:00`));

export default function HistoryPage() {
  const { vehicleId } = useParams();
  const { t, i18n } = useTranslation("myGarage");
  const navigate = useNavigate();
  const [detail, setDetail] = useState<VehicleDetail | null>(null);
  const [history, setHistory] = useState<ServiceHistoryPage | null>(null);
  const [page, setPage] = useState(0);
  const [pageSize, setPageSize] = useState(25);
  const [typeFilter, setTypeFilter] = useState<HistoryType | "">("");
  const [fromDate, setFromDate] = useState("");
  const [toDate, setToDate] = useState("");
  const [loading, setLoading] = useState(true);
  const [mutating, setMutating] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [dialogOpen, setDialogOpen] = useState(false);
  const [selectedRecord, setSelectedRecord] = useState<ServiceHistoryRecord | null>(null);
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [deleteTarget, setDeleteTarget] = useState<ServiceHistoryRecord | null>(null);
  const locale = i18n.language === "ru" ? "ru-RU" : "en-US";

  const load = useCallback(async (signal?: AbortSignal) => {
    if (!vehicleId) return;
    setLoading(true);
    setError(null);
    try {
      const [nextDetail, nextHistory] = await Promise.all([
        getVehicle(vehicleId, null, signal),
        getHistory(vehicleId, page, pageSize, null, signal),
      ]);
      setDetail(nextDetail);
      setHistory(nextHistory);
    } catch (failure) {
      if (!signal?.aborted) setError(getGarageFailureMessage(failure as APICallFailure, t));
    } finally {
      if (!signal?.aborted) setLoading(false);
    }
  }, [page, pageSize, t, vehicleId]);

  useEffect(() => {
    const controller = new AbortController();
    void load(controller.signal);
    return () => controller.abort();
  }, [load]);

  const filteredItems = useMemo(() => (history?.items ?? []).filter((record) => {
    if (typeFilter && record.type !== typeFilter) return false;
    if (fromDate && record.date < fromDate) return false;
    if (toDate && record.date > toDate) return false;
    return true;
  }), [fromDate, history?.items, toDate, typeFilter]);

  const openCreate = () => {
    setSelectedRecord(null);
    setSubmitError(null);
    setDialogOpen(true);
  };

  const openEdit = (record: ServiceHistoryRecord) => {
    setSelectedRecord(record);
    setSubmitError(null);
    setDialogOpen(true);
  };

  const submit = async (request: CreateServiceHistoryRequest) => {
    if (!vehicleId) return;
    setMutating(true);
    setSubmitError(null);
    try {
      if (selectedRecord) {
        await updateHistory(vehicleId, selectedRecord.id, request, null);
        await SuccessToast(t("success.historyUpdated"));
      } else {
        await createHistory(vehicleId, request, null);
        await SuccessToast(t("success.historyCreated"));
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
      await deleteHistory(vehicleId, deleteTarget.id, null);
      await SuccessToast(t("success.historyDeleted"));
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
  if (error || !detail || !history || !vehicleId) return <Box sx={{ p: { xs: 2, sm: 3, lg: 4 } }}><Alert severity="error" action={<Button color="inherit" onClick={() => void load()}>{t("retry")}</Button>}>{error || t("errors.CAR_VEHICLE_NOT_FOUND")}</Alert></Box>;

  const readOnly = detail.vehicle.archived;

  return (
    <Box sx={{ p: { xs: 2, sm: 3, lg: 4 } }}>
      <Stack direction={{ xs: "column", sm: "row" }} spacing={2} sx={{ alignItems: { sm: "center" }, mb: 2 }}>
        <Button startIcon={<ArrowBackIcon />} onClick={() => navigate(`/my-garage/vehicles/${vehicleId}`)} disabled={mutating}>{detail.vehicle.displayName}</Button>
        <Box sx={{ flex: 1 }}><Typography component="h1" variant="h4">{t("actions.recordService")}</Typography><Typography color="text.secondary">{detail.vehicle.make} {detail.vehicle.model}</Typography></Box>
        <Button variant="contained" startIcon={<AddIcon />} onClick={openCreate} disabled={mutating || readOnly} aria-label={t("actions.recordService")}>{t("actions.recordService")}</Button>
      </Stack>
      {readOnly ? <Alert severity="info" sx={{ mb: 2 }}>{t("conflicts.vehicleArchived")}</Alert> : null}
      {error ? <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert> : null}

      <Grid container spacing={2} sx={{ mb: 2 }}>
        <Grid size={{ xs: 12, sm: 4 }}><FormControl fullWidth disabled={mutating}><InputLabel>{t("fields.type")}</InputLabel><Select label={t("fields.type")} value={typeFilter} onChange={(event) => setTypeFilter(event.target.value as HistoryType | "")}><MenuItem value="">-</MenuItem>{Object.values(HistoryType).map((type) => <MenuItem key={type} value={type}>{t(`types.${type}`)}</MenuItem>)}</Select></FormControl></Grid>
        <Grid size={{ xs: 12, sm: 4 }}><TextField fullWidth type="date" label={t("fields.startDate")} value={fromDate} onChange={(event) => setFromDate(event.target.value)} slotProps={{ inputLabel: { shrink: true } }} disabled={mutating} /></Grid>
        <Grid size={{ xs: 12, sm: 4 }}><TextField fullWidth type="date" label={t("fields.endDate")} value={toDate} onChange={(event) => setToDate(event.target.value)} slotProps={{ inputLabel: { shrink: true } }} disabled={mutating} /></Grid>
      </Grid>

      <Card><CardContent sx={{ p: 0, "&:last-child": { pb: 0 } }}>
        <GarageTable ariaLabel={t("actions.recordService")} headers={[t("fields.date"), t("fields.type"), t("fields.title"), t("fields.odometerKm"), t("fields.cost"), t("fields.linkedMaintenance"), t("table_actions_column")]} empty={filteredItems.length === 0} emptyMessage={t("empty.history")}>
          {filteredItems.map((record) => <TableRow hover key={record.id}>
            <TableCell>{formatDate(record.date, locale)}</TableCell>
            <TableCell>{t(`types.${record.type}`)}</TableCell>
            <TableCell><Typography sx={{ fontWeight: 700, overflowWrap: "anywhere" }}>{record.title}</Typography></TableCell>
            <TableCell>{record.odometerKm.toLocaleString(locale)} {t("units.km")}</TableCell>
            <TableCell>{formatMinorCost(record.costAmountMinor, record.costCurrency)}</TableCell>
            <TableCell>{getLinkedMaintenanceLabels(record.linkedMaintenanceItemIds, detail.maintenanceItems).join(", ") || "-"}</TableCell>
            <TableCell><Stack direction="row" spacing={0.5}><Button size="small" onClick={() => openEdit(record)} disabled={mutating || readOnly} aria-label={`${t("actions.editHistory")}: ${record.title}`}><EditOutlinedIcon fontSize="small" /></Button><Button size="small" color="error" onClick={() => setDeleteTarget(record)} disabled={mutating || readOnly} aria-label={`${t("actions.deleteHistory")}: ${record.title}`}><DeleteOutlineIcon fontSize="small" /></Button></Stack></TableCell>
          </TableRow>)}
        </GarageTable>
        <TablePagination component="div" count={history.totalItemsCount} page={history.currentPage} rowsPerPage={Math.min(history.pageSize, 100)} rowsPerPageOptions={[25, 50, 100]} onPageChange={(_, nextPage) => setPage(nextPage)} onRowsPerPageChange={(event) => { setPage(0); setPageSize(Math.min(Number(event.target.value), 100)); }} labelRowsPerPage={t("table_rows_per_page_label")} disabled={mutating} slotProps={{ select: { inputProps: { "aria-label": t("table_rows_per_page_label") } } }} />
      </CardContent></Card>

      <HistoryDialog open={dialogOpen} record={selectedRecord} maintenanceItems={detail.maintenanceItems} busy={mutating} submitError={submitError} onClose={() => setDialogOpen(false)} onSubmit={submit} />
      <Dialog open={Boolean(deleteTarget)} onClose={mutating ? undefined : () => setDeleteTarget(null)}>
        <DialogTitle>{t("actions.deleteHistory")}</DialogTitle>
        <DialogContent><Typography>{deleteTarget?.title}</Typography></DialogContent>
        <DialogActions><Button onClick={() => setDeleteTarget(null)} disabled={mutating}>{t("actions.cancel")}</Button><Button color="error" variant="contained" onClick={() => void remove()} disabled={mutating}>{t("actions.deleteHistory")}</Button></DialogActions>
      </Dialog>
    </Box>
  );
}
