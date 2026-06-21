import { useEffect, useMemo, useState } from "react";
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
  TableRow,
  TextField,
  Tooltip,
  Typography,
} from "@mui/material";
import { DateTimePicker } from "@mui/x-date-pickers/DateTimePicker";
import DeleteIcon from "@mui/icons-material/Delete";
import EditIcon from "@mui/icons-material/Edit";
import LocalHospitalIcon from "@mui/icons-material/LocalHospital";
import { healthCareApi, HealthEvent, HealthEventType } from "src/api/healthCare";
import useUtils from "src/appUtils";
import {
  buildHealthCareChartData,
  ChartTimeRange,
  getTimeRangeBounds,
} from "./chartData";
import HealthCareChart from "./HealthCareChart";

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
  const [notes, setNotes] = useState("");
  const [chartTimeRange, setChartTimeRange] = useState<ChartTimeRange>("week");
  const [shareLink, setShareLink] = useState("");
  const [shareCopied, setShareCopied] = useState(false);
  const [editingEventId, setEditingEventId] = useState<string | null>(null);

  const refresh = () => healthCareApi.getEvents(undefined, undefined, u).then(setEvents);

  useEffect(() => { refresh(); }, []); // eslint-disable-line react-hooks/exhaustive-deps

  const chartData = useMemo(
    () => buildHealthCareChartData(events, chartTimeRange),
    [events, chartTimeRange]
  );
  const medicationNameOptions = useMemo(
    () => uniqueMedicationNames(events),
    [events]
  );

  useEffect(() => {
    setShareLink("");
    setShareCopied(false);
  }, [events, chartTimeRange]);

  const resetForm = () => {
    setEditingEventId(null);
    setType("Temperature");
    setStartedAt(nowInput());
    setEndedAt(nowInput());
    setTemperature("37.0");
    setMedicationName("");
    setMedicationAmount("1");
    setMedicationUnit(u.t("healthCare:unit_tablet"));
    setNotes("");
  };

  const eventPayload = () => ({
      type,
      startedAt: snapDate(startedAt).toISOString(),
      endedAt: type === "Sleep" ? snapDate(endedAt).toISOString() : undefined,
      temperatureCelsius: type === "Temperature" ? Number(temperature) : undefined,
      medicationName: type === "Medication" ? medicationName : undefined,
      medicationAmount: type === "Medication" ? Number(medicationAmount) : undefined,
      medicationUnit: type === "Medication" ? medicationUnit : undefined,
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
    setTemperature(String(event.temperatureCelsius || "37.0"));
    setMedicationName(event.medicationName || "");
    setMedicationAmount(String(event.medicationAmount || "1"));
    setMedicationUnit(event.medicationUnit || u.t("healthCare:unit_tablet"));
    setNotes(event.notes || "");
  };

  const deleteEvent = async (id: string) => {
    await healthCareApi.deleteEvent(id, u);

    if (editingEventId === id) {
      resetForm();
    }

    refresh();
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
    setShareLink(url);
    setShareCopied(false);

    try {
      await navigator.clipboard.writeText(url);
      setShareCopied(true);
    } catch {
      setShareCopied(false);
    }
  };

  const formatEvent = (event: HealthEvent) => {
    if (event.type === "Temperature") return `${event.temperatureCelsius} °C`;
    if (event.type === "Medication") return `${event.medicationName || u.t("healthCare:medication_fallback")} ${event.medicationAmount || ""} ${event.medicationUnit || ""}`;
    const time = new Date(event.endedAt || event.startedAt).toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" });
    return u.t("healthCare:sleep_until", { time });
  };

  const formatType = (eventType: HealthEventType) => {
    if (eventType === "Temperature") return u.t("healthCare:event_temperature");
    if (eventType === "Medication") return u.t("healthCare:event_medication");
    return u.t("healthCare:event_sleep");
  };

  return (
    <Box p={1}>
      <Stack direction="row" alignItems="center" gap={1} mb={2}>
        <LocalHospitalIcon color="primary" />
        <Typography variant="h3">{u.t("healthCare:page_title")}</Typography>
        <Button variant="outlined" size="small" onClick={shareChart} disabled={chartData.chartEvents.length === 0}>{u.t("healthCare:share_chart")}</Button>
      </Stack>
      <Typography color="text.secondary" mb={2}>
        {u.t("healthCare:page_description")}
      </Typography>
      {shareLink && (
        <TextField
          fullWidth
          label={u.t("healthCare:share_link")}
          value={shareLink}
          helperText={shareCopied ? u.t("healthCare:copied_to_clipboard") : u.t("healthCare:share_link_helper")}
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
              {type === "Temperature" && <TextField label={u.t("healthCare:temperature_celsius")} value={temperature} onChange={(e) => setTemperature(e.target.value)} size="small" />}
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
                  {events.map((event) => (
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
          </CardContent></Card>
        </Grid>
      </Grid>
    </Box>
  );
};

export default HealthCarePage;
