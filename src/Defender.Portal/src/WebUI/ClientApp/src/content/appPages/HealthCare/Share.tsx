import { useMemo } from "react";
import { Box, Card, CardContent, Chip, Stack, Typography } from "@mui/material";
import { LineChart } from "@mui/x-charts/LineChart";
import LocalHospitalIcon from "@mui/icons-material/LocalHospital";
import { HealthEvent } from "src/api/healthCare";
import useUtils from "src/appUtils";
import {
  buildHealthCareChartData,
  eventAxisMax,
  eventAxisMin,
  medicationLane,
  sleepLane,
} from "./chartData";

const decodeEvents = (): HealthEvent[] => {
  const params = new URLSearchParams(window.location.search);
  const data = params.get("data");
  if (!data) return [];

  try {
    const json = decodeURIComponent(escape(atob(data)));
    return JSON.parse(json) as HealthEvent[];
  } catch {
    return [];
  }
};

const formatEvent = (event: HealthEvent, t: (key: string, options?: object) => string) => {
  if (event.type === "Temperature") return `${event.temperatureCelsius} °C`;
  if (event.type === "Medication") return `${event.medicationName || t("healthCare:medication_fallback")} ${event.medicationAmount || ""} ${event.medicationUnit || ""}`;
  const time = new Date(event.endedAt || event.startedAt).toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" });
  return t("healthCare:sleep_until", { time });
};

const HealthCareSharePage = () => {
  const u = useUtils();

  const events = useMemo(() => decodeEvents(), []);
  const chartData = useMemo(() => buildHealthCareChartData(events), [events]);

  return (
    <Box p={2}>
      <Stack direction="row" alignItems="center" gap={1} mb={2}>
        <LocalHospitalIcon color="primary" />
        <Typography variant="h3">{u.t("healthCare:share_page_title")}</Typography>
      </Stack>
      <Typography color="text.secondary" mb={2}>
        {u.t("healthCare:share_description")}
      </Typography>
      <Card><CardContent>
        <Typography variant="h4" mb={1}>{u.t("healthCare:events_chart")}</Typography>
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
            <Chip key={event.id} label={`${new Date(event.startedAt).toLocaleString([], { day: "2-digit", month: "2-digit", hour: "2-digit", minute: "2-digit" })}: ${formatEvent(event, u.t)}`} color={event.type === "Sleep" ? "info" : event.type === "Medication" ? "secondary" : "primary"} variant="outlined" />
          ))}
        </Stack>
      </CardContent></Card>
    </Box>
  );
};

export default HealthCareSharePage;
