import { Box, Tooltip, Typography } from "@mui/material";
import { LineChart } from "@mui/x-charts/LineChart";
import { HealthEvent } from "src/api/healthCare";
import useUtils from "src/appUtils";
import { buildHealthCareChartData, ChartTimeRange } from "./chartData";

interface HealthCareChartProps {
  events: HealthEvent[];
  timeRange?: ChartTimeRange;
  height?: number;
}

const chartMargin = { left: 55, right: 24, top: 24, bottom: 48 };

const clamp = (value: number) => Math.max(0, Math.min(100, value));

const eventPercent = (time: number, minTime: number, maxTime: number) =>
  clamp(((time - minTime) / (maxTime - minTime)) * 100);

const medicationLabel = (event: HealthEvent, fallback: string) =>
  `${event.medicationName || fallback} ${event.medicationAmount || ""} ${event.medicationUnit || ""}`.trim();

const HealthCareChart = ({ events, timeRange = "all", height = 300 }: HealthCareChartProps) => {
  const u = useUtils();
  const chartData = buildHealthCareChartData(events, timeRange);
  const medicationEvents = chartData.chartEvents.filter((event) => event.type === "Medication");
  const sleepEvents = chartData.chartEvents.filter((event) => event.type === "Sleep");

  if (chartData.chartEvents.length === 0) {
    return <Typography color="text.secondary">{u.t("healthCare:no_events_to_display")}</Typography>;
  }

  return (
    <Box position="relative" height={height}>
      {chartData.temperatureEvents.length > 0 ? (
        <LineChart
          height={height}
          margin={chartMargin}
          xAxis={[
            {
              scaleType: "time",
              data: chartData.temperatureXAxis,
              min: new Date(chartData.minTime),
              max: new Date(chartData.maxTime),
            },
          ]}
          yAxis={[{ id: "temperature", label: "C" }]}
          series={[
            {
              data: chartData.temperatureData,
              yAxisId: "temperature",
              showMark: true,
              connectNulls: true,
            },
          ]}
        />
      ) : (
        <Box height={height} display="flex" alignItems="center" justifyContent="center">
          <Typography color="text.secondary">{u.t("healthCare:no_events_to_display")}</Typography>
        </Box>
      )}

      <Box
        position="absolute"
        sx={{
          left: chartMargin.left,
          right: chartMargin.right,
          top: chartMargin.top,
          bottom: chartMargin.bottom,
          pointerEvents: "none",
        }}
      >
        {sleepEvents.map((event) => {
          const start = new Date(event.startedAt).getTime();
          const end = new Date(event.endedAt || event.startedAt).getTime();
          const left = eventPercent(start, chartData.minTime, chartData.maxTime);
          const right = eventPercent(end, chartData.minTime, chartData.maxTime);

          return (
            <Tooltip
              key={event.id}
              title={u.t("healthCare:sleep_until", {
                time: new Date(event.endedAt || event.startedAt).toLocaleTimeString([], {
                  hour: "2-digit",
                  minute: "2-digit",
                }),
              })}
            >
              <Box
                position="absolute"
                top="12%"
                bottom="12%"
                sx={{
                  left: `${Math.min(left, right)}%`,
                  width: `${Math.max(Math.abs(right - left), 1)}%`,
                  minWidth: 6,
                  bgcolor: "info.light",
                  opacity: 0.24,
                  border: 1,
                  borderColor: "info.main",
                  borderRadius: 1,
                  pointerEvents: "auto",
                }}
              />
            </Tooltip>
          );
        })}

        {medicationEvents.map((event) => {
          const left = eventPercent(
            new Date(event.startedAt).getTime(),
            chartData.minTime,
            chartData.maxTime
          );

          return (
            <Tooltip
              key={event.id}
              title={medicationLabel(event, u.t("healthCare:medication_fallback"))}
            >
              <Box
                position="absolute"
                top={0}
                bottom={0}
                sx={{
                  left: `${left}%`,
                  width: 2,
                  bgcolor: "secondary.main",
                  opacity: 0.8,
                  pointerEvents: "auto",
                }}
              />
            </Tooltip>
          );
        })}
      </Box>
    </Box>
  );
};

export default HealthCareChart;
