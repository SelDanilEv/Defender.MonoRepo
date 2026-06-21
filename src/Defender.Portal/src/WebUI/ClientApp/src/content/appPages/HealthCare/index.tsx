import { useEffect, useMemo, useState } from "react";
import type { ChangeEvent } from "react";
import {
  Autocomplete,
  Box,
  Button,
  Card,
  CardContent,
  Grid,
  IconButton,
  MenuItem,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TablePagination,
  TableRow,
  TextField,
  Tooltip,
  Typography,
} from "@mui/material";
import { DateTimePicker } from "@mui/x-date-pickers/DateTimePicker";
import DeleteIcon from "@mui/icons-material/Delete";
import EditIcon from "@mui/icons-material/Edit";
import LocalHospitalIcon from "@mui/icons-material/LocalHospital";
import { healthCareApi, HealthChartShare, HealthEvent, HealthEventType } from "src/api/healthCare";
import useUtils from "src/appUtils";
import {
  buildHealthCareChartData,
  ChartTimeRange,
  getTimeRangeBounds,
  paginateHealthEvents,
  wellbeingScoreToEmoji,
} from "./chartData";
import HealthCareChart from "./HealthCareChart";
import TemperatureSlider, { normalizeTemperature } from "./TemperatureSlider";
import WellbeingSelector from "./WellbeingSelector";
import WellbeingSummary from "./WellbeingSummary";

const snapDate = (value: Date) => {
  const snapped = new Date(value);
  snapped.setSeconds(0, 0);
  snapped.setMinutes(snapped.getMinutes() < 30 ? 0 : 30);

  return snapped;
};

const nowInput = () => snapDate(new Date());

const toDateTimeInput = (value?: string) =>
  value ? snapDate(new Date(value)) : nowInput();

const uniqueMedicationNames = (events: HealthEvent[]) => {
  const names = new Map<string, string>();

  events
    .filter((event) => event.type === "Medication" && event.medicationName)
    .forEach((event) => {
      const name = event.medicationName?.trim();

      if (name) {
        names.set(name.toLocaleLowerCase(), name);
      }
    });

  return [...names.values()].sort((left, right) => left.localeCompare(right));
};

const HealthCarePage = () => {
  const u = useUtils();

  const [events, setEvents] = useState<HealthEvent[]>([]);
  const [type, setType] = useState<HealthEventType>("Temperature");
  const [startedAt, setStartedAt] = useState<Date>(nowInput());
  const [endedAt, setEndedAt] = useState<Date>(nowInput());
  const [temperature, setTemperature] = useState("37.0");
  const [medicationName, setMedicationName] = useState("");
  const [medicationAmount, setMedicationAmount] = useState("1");
  const [medicationUnit, setMedicationUnit] = useState(() => u.t("healthCare:unit_tablet"));
  const [wellbeingScore, setWellbeingScore] = useState(3);
  const [notes, setNotes] = useState("");
  const [chartTimeRange, setChartTimeRange] = useState<ChartTimeRange>("week");
  const [share, setShare] = useState<HealthChartShare | null>(null);
  const [shareCopied, setShareCopied] = useState(false);
  const [shareStatusUpdating, setShareStatusUpdating] = useState(false);
  const [editingEventId, setEditingEventId] = useState<string | null>(null);
  const [page, setPage] = useState(0);
  const [rowsPerPage, setRowsPerPage] = useState(10);

  const refresh = () => healthCareApi.getEvents(undefined, undefined, u).then(setEvents);
  const refreshShare = () => healthCareApi.getCurrentShare(u).then(setShare);

  useEffect(() => {
    refresh();
    refreshShare();
  }, []); // eslint-disable-line react-hooks/exhaustive-deps

  const chartData = useMemo(
    () => buildHealthCareChartData(events, chartTimeRange),
    [events, chartTimeRange]
  );
  const medicationNameOptions = useMemo(
    () => uniqueMedicationNames(events),
    [events]
  );
  const pagedEvents = useMemo(
    () => paginateHealthEvents(events, page, rowsPerPage),
    [events, page, rowsPerPage]
  );
  const shareLink = share ? `${window.location.origin}${share.publicUrl}` : "";
  const shareHelperText = shareCopied
    ? u.t("healthCare:copied_to_clipboard")
    : share?.isEnabled
      ? u.t("healthCare:share_link_active_helper")
      : u.t("healthCare:share_link_disabled_helper");

  useEffect(() => {
    setShareCopied(false);
  }, [events, chartTimeRange]);

  useEffect(() => {
    const maxPage =
      events.length === 0
        ? 0
        : Math.max(0, Math.ceil(events.length / rowsPerPage) - 1);

    setPage((currentPage) => Math.min(currentPage, maxPage));
  }, [events.length, rowsPerPage]);

  const resetForm = () => {
    setEditingEventId(null);
    setType("Temperature");
    setStartedAt(nowInput());
    setEndedAt(nowInput());
    setTemperature("37.0");
    setMedicationName("");
    setMedicationAmount("1");
    setMedicationUnit(u.t("healthCare:unit_tablet"));
    setWellbeingScore(3);
    setNotes("");
  };

  const eventPayload = () => ({
      type,
      startedAt: snapDate(startedAt).toISOString(),
      endedAt: type === "Sleep" ? snapDate(endedAt).toISOString() : undefined,
      temperatureCelsius: type === "Temperature" ? Number(normalizeTemperature(temperature).toFixed(1)) : undefined,
      medicationName: type === "Medication" ? medicationName : undefined,
      medicationAmount: type === "Medication" ? Number(medicationAmount) : undefined,
      medicationUnit: type === "Medication" ? medicationUnit : undefined,
      wellbeingScore: type === "Wellbeing" ? wellbeingScore : undefined,
      notes,
  });

  const saveEvent = async () => {
    if (editingEventId) {
      await healthCareApi.updateEvent({
        ...eventPayload(),
        id: editingEventId,
      }, u);
    } else {
      await healthCareApi.createEvent(eventPayload(), u);
    }

    resetForm();
    refresh();
  };

  const editEvent = (event: HealthEvent) => {
    setEditingEventId(event.id);
    setType(event.type);
    setStartedAt(toDateTimeInput(event.startedAt));
    setEndedAt(toDateTimeInput(event.endedAt || event.startedAt));
    setTemperature(String(event.temperatureCelsius ?? "37.0"));
    setMedicationName(event.medicationName || "");
    setMedicationAmount(String(event.medicationAmount || "1"));
    setMedicationUnit(event.medicationUnit || u.t("healthCare:unit_tablet"));
    setWellbeingScore(event.wellbeingScore || 3);
    setNotes(event.notes || "");
  };

  const deleteEvent = async (id: string) => {
    await healthCareApi.deleteEvent(id, u);

    if (editingEventId === id) {
      resetForm();
    }

    refresh();
  };

  const handleChangePage = (_event: unknown, newPage: number) => {
    setPage(newPage);
  };

  const handleChangeRowsPerPage = (
    event: ChangeEvent<HTMLInputElement | HTMLTextAreaElement>
  ) => {
    setRowsPerPage(parseInt(event.target.value, 10));
    setPage(0);
  };

  const shareChart = async () => {
    const bounds = getTimeRangeBounds(chartTimeRange);
    const share = await healthCareApi.createShare(
      {
        from: bounds.from?.toISOString(),
        to: bounds.to?.toISOString(),
      },
      u
    );
    const url = `${window.location.origin}${share.publicUrl}`;
    setShare(share);
    setShareCopied(false);

    try {
      await navigator.clipboard.writeText(url);
      setShareCopied(true);
    } catch {
      setShareCopied(false);
    }
  };

  const updateShareStatus = async (isEnabled: boolean) => {
    if (shareStatusUpdating) {
      return;
    }

    setShareStatusUpdating(true);

    try {
      const updatedShare = await healthCareApi.updateShareStatus({ isEnabled }, u);

      if (updatedShare) {
        setShare(updatedShare);
        setShareCopied(false);
      }
    } finally {
      setShareStatusUpdating(false);
    }
  };

  const formatEvent = (event: HealthEvent) => {
    if (event.type === "Temperature") {
      return event.temperatureCelsius === undefined || event.temperatureCelsius === null
        ? "-"
        : `${event.temperatureCelsius.toFixed(1)} \u00b0C`;
    }

    if (event.type === "Medication") return `${event.medicationName || u.t("healthCare:medication_fallback")} ${event.medicationAmount || ""} ${event.medicationUnit || ""}`;
    if (event.type === "Wellbeing") return `${wellbeingScoreToEmoji(event.wellbeingScore)} ${event.wellbeingScore || ""}/5`;
    const time = new Date(event.endedAt || event.startedAt).toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" });
    return u.t("healthCare:sleep_until", { time });
  };

  const formatType = (eventType: HealthEventType) => {
    if (eventType === "Temperature") return u.t("healthCare:event_temperature");
    if (eventType === "Medication") return u.t("healthCare:event_medication");
    if (eventType === "Wellbeing") return u.t("healthCare:event_wellbeing");
    return u.t("healthCare:event_sleep");
  };

  const wellbeingDescriptions = [
    u.t("healthCare:wellbeing_1"),
    u.t("healthCare:wellbeing_2"),
    u.t("healthCare:wellbeing_3"),
    u.t("healthCare:wellbeing_4"),
    u.t("healthCare:wellbeing_5"),
  ];

  return (
    <Box p={1}>
      <Stack direction="row" alignItems="center" gap={1} mb={2}>
        <LocalHospitalIcon color="primary" />
        <Typography variant="h3">{u.t("healthCare:page_title")}</Typography>
        <Stack direction="row" gap={1} flexWrap="wrap">
          <Button variant="outlined" size="small" onClick={shareChart} disabled={chartData.chartEvents.length === 0}>
            {share ? u.t("healthCare:update_shared_range") : u.t("healthCare:share_chart")}
          </Button>
          {share && (
            <Button
              variant="outlined"
              color={share.isEnabled ? "warning" : "success"}
              size="small"
              disabled={shareStatusUpdating}
              onClick={() => updateShareStatus(!share.isEnabled)}
            >
              {share.isEnabled ? u.t("healthCare:stop_sharing") : u.t("healthCare:resume_sharing")}
            </Button>
          )}
        </Stack>
      </Stack>
      <Typography color="text.secondary" mb={2}>
        {u.t("healthCare:page_description")}
      </Typography>
      {share && (
        <TextField
          fullWidth
          label={u.t("healthCare:share_link")}
          value={shareLink}
          helperText={shareHelperText}
          InputProps={{ readOnly: true }}
          size="small"
          sx={{ mb: 2 }}
        />
      )}

      <Grid container spacing={2}>
        <Grid item xs={12} md={4}>
          <Card><CardContent>
            <Stack gap={2}>
              <TextField select label={u.t("healthCare:event_type")} value={type} onChange={(e) => setType(e.target.value as HealthEventType)} size="small">
                <MenuItem value="Temperature">{u.t("healthCare:event_temperature")}</MenuItem>
                <MenuItem value="Medication">{u.t("healthCare:event_medication")}</MenuItem>
                <MenuItem value="Sleep">{u.t("healthCare:event_sleep")}</MenuItem>
                <MenuItem value="Wellbeing">{u.t("healthCare:event_wellbeing")}</MenuItem>
              </TextField>
              <DateTimePicker
                label={u.t("healthCare:start")}
                value={startedAt}
                onChange={(value) => value && setStartedAt(snapDate(value))}
                minutesStep={30}
                timeSteps={{ minutes: 30 }}
                skipDisabled
                slotProps={{ textField: { size: "small" } }}
              />
              {type === "Sleep" && (
                <DateTimePicker
                  label={u.t("healthCare:sleep_end")}
                  value={endedAt}
                  onChange={(value) => value && setEndedAt(snapDate(value))}
                  minutesStep={30}
                  timeSteps={{ minutes: 30 }}
                  skipDisabled
                  slotProps={{ textField: { size: "small" } }}
                />
              )}
              {type === "Temperature" && (
                <TemperatureSlider
                  label={u.t("healthCare:temperature_celsius")}
                  value={temperature}
                  onChange={setTemperature}
                />
              )}
              {type === "Medication" && <>
                <Autocomplete
                  freeSolo
                  options={medicationNameOptions}
                  value={medicationName}
                  onChange={(_, value) => setMedicationName(value || "")}
                  onInputChange={(_, value) => setMedicationName(value)}
                  renderInput={(params) => (
                    <TextField {...params} label={u.t("healthCare:medication_name")} size="small" />
                  )}
                />
                <TextField label={u.t("healthCare:medication_amount")} value={medicationAmount} onChange={(e) => setMedicationAmount(e.target.value)} size="small" />
                <TextField label={u.t("healthCare:medication_unit")} value={medicationUnit} onChange={(e) => setMedicationUnit(e.target.value)} size="small" />
              </>}
              {type === "Wellbeing" && (
                <WellbeingSelector
                  label={u.t("healthCare:wellbeing")}
                  score={wellbeingScore}
                  descriptions={wellbeingDescriptions}
                  onChange={setWellbeingScore}
                />
              )}
              <TextField label={u.t("healthCare:notes")} value={notes} onChange={(e) => setNotes(e.target.value)} size="small" multiline minRows={2} />
              <Stack direction="row" gap={1}>
                <Button variant="contained" onClick={saveEvent}>
                  {editingEventId ? u.t("healthCare:save") : u.t("healthCare:add")}
                </Button>
                {editingEventId && (
                  <Button variant="outlined" onClick={resetForm}>{u.t("healthCare:cancel")}</Button>
                )}
              </Stack>
            </Stack>
          </CardContent></Card>
        </Grid>

        <Grid item xs={12} md={8}>
          <Card><CardContent>
            <Stack direction={{ xs: "column", sm: "row" }} alignItems={{ xs: "stretch", sm: "center" }} justifyContent="space-between" gap={1} mb={1}>
              <Typography variant="h4">{u.t("healthCare:events_chart")}</Typography>
              <TextField
                select
                label={u.t("healthCare:chart_time_range")}
                value={chartTimeRange}
                onChange={(event) => setChartTimeRange(event.target.value as ChartTimeRange)}
                size="small"
                sx={{ minWidth: 180 }}
              >
                <MenuItem value="day">{u.t("healthCare:range_day")}</MenuItem>
                <MenuItem value="week">{u.t("healthCare:range_week")}</MenuItem>
                <MenuItem value="month">{u.t("healthCare:range_month")}</MenuItem>
                <MenuItem value="all">{u.t("healthCare:range_all")}</MenuItem>
              </TextField>
            </Stack>
            <WellbeingSummary
              events={events}
              timeRange={chartTimeRange}
              title={u.t("healthCare:latest_wellbeing")}
              scoreLabel={(score) => u.t("healthCare:wellbeing_score", { score })}
            />
            <HealthCareChart events={events} timeRange={chartTimeRange} />
            <Typography variant="h4" mt={3} mb={1}>{u.t("healthCare:events_grid")}</Typography>
            <TableContainer>
              <Table size="small">
                <TableHead>
                  <TableRow>
                    <TableCell>{u.t("healthCare:grid_started_at")}</TableCell>
                    <TableCell>{u.t("healthCare:grid_type")}</TableCell>
                    <TableCell>{u.t("healthCare:grid_value")}</TableCell>
                    <TableCell>{u.t("healthCare:notes")}</TableCell>
                    <TableCell align="right">{u.t("healthCare:grid_actions")}</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {pagedEvents.map((event) => (
                    <TableRow key={event.id} selected={editingEventId === event.id}>
                      <TableCell>
                        {new Date(event.startedAt).toLocaleString([], {
                          day: "2-digit",
                          month: "2-digit",
                          hour: "2-digit",
                          minute: "2-digit",
                        })}
                      </TableCell>
                      <TableCell>{formatType(event.type)}</TableCell>
                      <TableCell>{formatEvent(event)}</TableCell>
                      <TableCell>{event.notes || ""}</TableCell>
                      <TableCell align="right">
                        <Tooltip title={u.t("healthCare:edit")}>
                          <IconButton size="small" onClick={() => editEvent(event)}>
                            <EditIcon fontSize="small" />
                          </IconButton>
                        </Tooltip>
                        <Tooltip title={u.t("healthCare:delete")}>
                          <IconButton size="small" onClick={() => deleteEvent(event.id)}>
                            <DeleteIcon fontSize="small" />
                          </IconButton>
                        </Tooltip>
                      </TableCell>
                    </TableRow>
                  ))}
                  {events.length === 0 && (
                    <TableRow>
                      <TableCell colSpan={5}>
                        <Typography color="text.secondary">{u.t("healthCare:no_events_to_display")}</Typography>
                      </TableCell>
                    </TableRow>
                  )}
                </TableBody>
              </Table>
            </TableContainer>
            {events.length > 0 && (
              <Box
                sx={{
                  display: "flex",
                  alignItems: "center",
                  justifyContent: "flex-end",
                  flexWrap: "wrap",
                  gap: 0.5,
                  px: 1.5,
                  pt: 0.5,
                }}
              >
                <Typography
                  variant="body2"
                  sx={{
                    textAlign: "center",
                    fontSize: { xs: "0.68rem", sm: "0.8rem" },
                    lineHeight: 1.2,
                  }}
                >
                  {u.t("table_rows_per_page_label")}
                </Typography>
                <TablePagination
                  component="div"
                  count={events.length}
                  page={page}
                  onPageChange={handleChangePage}
                  rowsPerPage={rowsPerPage}
                  onRowsPerPageChange={handleChangeRowsPerPage}
                  rowsPerPageOptions={[10, 25, 50]}
                  labelRowsPerPage=""
                />
              </Box>
            )}
          </CardContent></Card>
        </Grid>
      </Grid>
    </Box>
  );
};

export default HealthCarePage;
