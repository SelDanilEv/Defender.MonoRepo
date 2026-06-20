import { useEffect, useMemo, useState } from "react";
import { Box, Button, Card, CardContent, Chip, Grid, MenuItem, Stack, TextField, Typography } from "@mui/material";
import { LineChart } from "@mui/x-charts/LineChart";
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

const nowInput = () => new Date().toISOString().slice(0, 16);

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

  const refresh = () => healthCareApi.getEvents().then(setEvents);

  useEffect(() => { refresh(); }, []);

  const chartData = useMemo(
    () => buildHealthCareChartData(events, chartTimeRange),
    [events, chartTimeRange]
  );

  useEffect(() => {
    setShareLink("");
    setShareCopied(false);
  }, [events, chartTimeRange]);

  const addEvent = async () => {
    await healthCareApi.createEvent({
      type,
      startedAt: new Date(startedAt).toISOString(),
      endedAt: type === "Sleep" ? new Date(endedAt).toISOString() : undefined,
      temperatureCelsius: type === "Temperature" ? Number(temperature) : undefined,
      medicationName: type === "Medication" ? medicationName : undefined,
      medicationAmount: type === "Medication" ? Number(medicationAmount) : undefined,
      medicationUnit: type === "Medication" ? medicationUnit : undefined,
      notes,
    });
    setNotes("");
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
              <TextField label={u.t("healthCare:start")} type="datetime-local" value={startedAt} onChange={(e) => setStartedAt(e.target.value)} InputLabelProps={{ shrink: true }} size="small" />
              {type === "Sleep" && <TextField label={u.t("healthCare:sleep_end")} type="datetime-local" value={endedAt} onChange={(e) => setEndedAt(e.target.value)} InputLabelProps={{ shrink: true }} size="small" />}
              {type === "Temperature" && <TextField label={u.t("healthCare:temperature_celsius")} value={temperature} onChange={(e) => setTemperature(e.target.value)} size="small" />}
              {type === "Medication" && <>
                <TextField label={u.t("healthCare:medication_name")} value={medicationName} onChange={(e) => setMedicationName(e.target.value)} size="small" />
                <TextField label={u.t("healthCare:medication_amount")} value={medicationAmount} onChange={(e) => setMedicationAmount(e.target.value)} size="small" />
                <TextField label={u.t("healthCare:medication_unit")} value={medicationUnit} onChange={(e) => setMedicationUnit(e.target.value)} size="small" />
              </>}
              <TextField label={u.t("healthCare:notes")} value={notes} onChange={(e) => setNotes(e.target.value)} size="small" multiline minRows={2} />
              <Button variant="contained" onClick={addEvent}>{u.t("healthCare:add")}</Button>
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
            <Stack direction="row" flexWrap="wrap" gap={1} mt={2}>
              {events.map((event) => (
                <Chip key={event.id} label={`${new Date(event.startedAt).toLocaleString([], { day: "2-digit", month: "2-digit", hour: "2-digit", minute: "2-digit" })}: ${formatEvent(event)}`} onDelete={() => healthCareApi.deleteEvent(event.id).then(refresh)} color={event.type === "Sleep" ? "info" : event.type === "Medication" ? "secondary" : "primary"} variant="outlined" />
              ))}
            </Stack>
          </CardContent></Card>
        </Grid>
      </Grid>
    </Box>
  );
};

export default HealthCarePage;
