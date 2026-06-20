import { useMemo } from "react";
import { Box, Card, CardContent, Chip, Stack, Typography } from "@mui/material";
import { LineChart } from "@mui/x-charts/LineChart";
import LocalHospitalIcon from "@mui/icons-material/LocalHospital";
import { HealthEvent } from "src/api/healthCare";
import useUtils from "src/appUtils";

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
  const chartEvents = useMemo(
    () => events.filter((event) => event.type === "Temperature" && event.temperatureCelsius !== undefined),
    [events]
  );

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
        <Typography variant="h4" mb={1}>{u.t("healthCare:temperature_chart")}</Typography>
        {chartEvents.length > 0 ? (
          <LineChart
            height={300}
            xAxis={[{ scaleType: "point", data: chartEvents.map((event) => new Date(event.startedAt).toLocaleString([], { day: "2-digit", month: "2-digit", hour: "2-digit", minute: "2-digit" })) }]}
            series={[{ label: "°C", data: chartEvents.map((event) => event.temperatureCelsius || null) }]}
          />
        ) : (
          <Typography color="text.secondary">{u.t("healthCare:no_temperature_points")}</Typography>
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
