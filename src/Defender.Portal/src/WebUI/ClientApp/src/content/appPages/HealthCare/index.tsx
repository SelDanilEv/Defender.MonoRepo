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
import { LineChart } from "@mui/x-charts/LineChart";
import DeleteIcon from "@mui/icons-material/Delete";
import EditIcon from "@mui/icons-material/Edit";
import LocalHospitalIcon from "@mui/icons-material/LocalHospital";
import { healthCareApi, HealthEvent, HealthEventType } from "src/api/healthCare";
import useUtils from "src/appUtils";
import {
  buildHealthCareChartData,
  ChartTimeRange,
  eventAxisMax,
  eventAxisMin,
  medicationLane,
  sleepLane,
} from "./chartData";

const snapDateTimeInput = (value: string) => {
  const [date, time] = value.split("T");
  const [hour, minute] = (time || "").split(":");

  if (!date || !hour || !minute) {
    return value;
  }

  return `${date}T${hour}:${Number(minute) < 30 ? "00" : "30"}`;
};

const nowInput = () => snapDateTimeInput(new Date().toISOString().slice(0, 16));

const toDateTimeInput = (value?: string) =>
  value ? snapDateTimeInput(new Date(value).toISOString().slice(0, 16)) : nowInput();

const dateTimeInputProps = { step: 1800 };

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
  const [startedAt, setStartedAt] = useState(nowInput());
  const [endedAt, setEndedAt] = useState(nowInput());
  const [temperature, setTemperature] = useState("37.0");
  const [medicationName, setMedicationName] = useState("");
  const [medicationAmount, setMedicationAmount] = useState("1");
  const [medicationUnit, setMedicationUnit] = useState(() => u.t("healthCare:unit_tablet"));
  const [notes, setNotes] = useState("");
  const [chartTimeRange, setChartTimeRange] = useState<ChartTimeRange>("week");
  const [shareLink, setShareLink] = useState("");
  const [shareCopied, setShareCopied] = useState(false);
  const [editingEventId, setEditingEventId] = useState<string | null>(null);

  const refresh = () => healthCareApi.getEvents().then(setEvents);

  useEffect(() => { refresh(); }, []);

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
      startedAt: new Date(snapDateTimeInput(startedAt)).toISOString(),
      endedAt: type === "Sleep" ? new Date(snapDateTimeInput(endedAt)).toISOString() : undefined,
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
      });
    } else {
      await healthCareApi.createEvent(eventPayload());
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
    await healthCareApi.deleteEvent(id);

    if (editingEventId === id) {
      resetForm();
    }

    refresh();
  };

  const shareChart = async () => {
    const payload = btoa(unescape(encodeURIComponent(JSON.stringify(chartData.chartEvents))));
    const url = `${window.location.origin}/health-care/share?data=${payload}`;
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
              <TextField label={u.t("healthCare:start")} type="datetime-local" value={startedAt} onChange={(e) => setStartedAt(e.target.value)} onBlur={() => setStartedAt(snapDateTimeInput(startedAt))} inputProps={dateTimeInputProps} InputLabelProps={{ shrink: true }} size="small" />
              {type === "Sleep" && <TextField label={u.t("healthCare:sleep_end")} type="datetime-local" value={endedAt} onChange={(e) => setEndedAt(e.target.value)} onBlur={() => setEndedAt(snapDateTimeInput(endedAt))} inputProps={dateTimeInputProps} InputLabelProps={{ shrink: true }} size="small" />}
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
            {chartData.chartEvents.length > 0 ? (
              <LineChart
                height={300}
                margin={{ left: 55, right: 90 }}
                xAxis={[{ scaleType: "point", data: chartData.xLabels }]}
                yAxis={[
                  { id: "temperature", label: "°C" },
                  {
                    id: "events",
                    position: "right",
                    min: eventAxisMin,
                    max: eventAxisMax,
                    valueFormatter: (value) =>
                      value === medicationLane
                        ? u.t("healthCare:event_medication")
                        : value === sleepLane
                          ? u.t("healthCare:event_sleep")
                          : "",
                  },
                ]}
                series={[
                  { label: "°C", data: chartData.temperatureData, yAxisId: "temperature", showMark: true },
                  { label: u.t("healthCare:event_medication"), data: chartData.medicationData, yAxisId: "events", showMark: true },
                  { label: u.t("healthCare:event_sleep"), data: chartData.sleepData, yAxisId: "events", showMark: true },
                ]}
              />
            ) : (
              <Typography color="text.secondary">{u.t("healthCare:no_events_to_display")}</Typography>
            )}
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
