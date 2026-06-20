import { useMemo } from "react";
import { Box, Card, CardContent, Chip, Stack, Typography } from "@mui/material";
import { LineChart } from "@mui/x-charts/LineChart";
import LocalHospitalIcon from "@mui/icons-material/LocalHospital";
import { HealthEvent } from "src/api/healthCare";

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

const formatEvent = (event: HealthEvent) => {
  if (event.type === "Temperature") return `${event.temperatureCelsius} °C`;
  if (event.type === "Medication") return `${event.medicationName || "Лекарство"} ${event.medicationAmount || ""} ${event.medicationUnit || ""}`;
  return `Сон до ${new Date(event.endedAt || event.startedAt).toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" })}`;
};

const HealthCareSharePage = () => {
  const events = useMemo(() => decodeEvents(), []);
  const chartEvents = useMemo(
    () => events.filter((event) => event.type === "Temperature" && event.temperatureCelsius !== undefined),
    [events]
  );

  return (
    <Box p={2}>
      <Stack direction="row" alignItems="center" gap={1} mb={2}>
        <LocalHospitalIcon color="primary" />
        <Typography variant="h3">Shared Health Care chart</Typography>
      </Stack>
      <Typography color="text.secondary" mb={2}>
        Публичная ссылка на график. На графике показаны только заполненные температурные фреймы, а лекарства и сон отображаются событиями ниже.
      </Typography>
      <Card><CardContent>
        <Typography variant="h4" mb={1}>График температуры</Typography>
        {chartEvents.length > 0 ? (
          <LineChart
            height={300}
            xAxis={[{ scaleType: "point", data: chartEvents.map((event) => new Date(event.startedAt).toLocaleString([], { day: "2-digit", month: "2-digit", hour: "2-digit", minute: "2-digit" })) }]}
            series={[{ label: "°C", data: chartEvents.map((event) => event.temperatureCelsius || null) }]}
          />
        ) : (
          <Typography color="text.secondary">Нет температурных точек для отображения.</Typography>
        )}
        <Stack direction="row" flexWrap="wrap" gap={1} mt={2}>
          {events.map((event) => (
            <Chip key={event.id} label={`${new Date(event.startedAt).toLocaleString([], { day: "2-digit", month: "2-digit", hour: "2-digit", minute: "2-digit" })}: ${formatEvent(event)}`} color={event.type === "Sleep" ? "info" : event.type === "Medication" ? "secondary" : "primary"} variant="outlined" />
          ))}
        </Stack>
      </CardContent></Card>
    </Box>
  );
};

export default HealthCareSharePage;
