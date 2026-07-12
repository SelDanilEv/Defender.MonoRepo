import { Box, Tooltip, Typography } from "@mui/material";
import { LineChart } from "@mui/x-charts/LineChart";
import { HealthEvent } from "src/api/healthCare";
import useUtils from "src/appUtils";
import { buildHealthCareChartData, ChartTimeRange } from "./chartData";
import { formatEventTime } from "./dateFormat";

interface HealthCareChartProps {
  events: HealthEvent[];
  timeRange?: ChartTimeRange;
  height?: number;
  language?: string;
}

const chartMargin = { left: 55, right: 24, top: 24, bottom: 48 };
const mobileChartMargin = { left: 36, right: 8, top: 12, bottom: 36 };
const tooltipTouchDelayMs = 150;

const clamp = (value: number) => Math.max(0, Math.min(100, value));

const eventPercent = (time: number, minTime: number, maxTime: number) =>
  clamp(((time - minTime) / (maxTime - minTime)) * 100);

const medicationLabel = (event: HealthEvent, fallback: string) =>
  `${event.medicationName || fallback} ${event.medicationAmount || ""} ${event.medicationUnit || ""}`.trim();

const HealthCareChart = ({
  events,
  timeRange = "all",
  height = 300,
  language = "en",
}: HealthCareChartProps) => {
  const u = useUtils();
  const chartData = buildHealthCareChartData(events, timeRange);
  const medicationEvents = chartData.chartEvents.filter((event) => event.type === "Medication");
  const sleepEvents = chartData.chartEvents.filter((event) => event.type === "Sleep");
  const margin = u.isMobile ? mobileChartMargin : chartMargin;
  const medicationLineWidth = u.isMobile ? 14 : 10;

  if (chartData.chartEvents.length === 0) {
    return (
      <Typography sx={{
        color: "text.secondary"
      }}>{u.t("healthCare:no_events_to_display")}</Typography>
    );
  }

  return (
    <Box
      sx={{
        position: "relative",
        height: height
      }}>
      {chartData.temperatureEvents.length > 0 ? (
        <LineChart
          height={height}
          margin={margin}
          xAxis={[
            {
              scaleType: "time",
              data: chartData.temperatureXAxis,
              min: new Date(chartData.minTime),
              max: new Date(chartData.maxTime),
              valueFormatter: (value) => formatEventTime(new Date(value), language),
            },
          ]}
          yAxis={[{ id: "temperature" }]}
          series={[
            {
              data: chartData.temperatureData,
              yAxisId: "temperature",
              showMark: true,
              connectNulls: true,
            },
          ]}
          hideLegend
        />
      ) : (
        <Box
          sx={{
            height: height,
            display: "flex",
            alignItems: "center",
            justifyContent: "center"
          }}>
          <Typography sx={{
            color: "text.secondary"
          }}>{u.t("healthCare:no_events_to_display")}</Typography>
        </Box>
      )}
      <Box
        sx={{
          position: "absolute",
          left: margin.left,
          right: margin.right,
          top: margin.top,
          bottom: margin.bottom,
          pointerEvents: "none"
        }}>
        {sleepEvents.map((event) => {
          const start = new Date(event.startedAt).getTime();
          const end = new Date(event.endedAt || event.startedAt).getTime();
          const left = eventPercent(start, chartData.minTime, chartData.maxTime);
          const right = eventPercent(end, chartData.minTime, chartData.maxTime);

          return (
            <Tooltip
              key={event.id}
              enterTouchDelay={tooltipTouchDelayMs}
              leaveTouchDelay={3000}
              title={u.t("healthCare:sleep_until", {
                time: formatEventTime(new Date(event.endedAt || event.startedAt), language),
              })}
            >
              <Box
                sx={{
                  position: "absolute",
                  top: "12%",
                  bottom: "12%",
                  left: `${Math.min(left, right)}%`,
                  width: `${Math.max(Math.abs(right - left), 1)}%`,
                  minWidth: 6,
                  bgcolor: "info.light",
                  opacity: 0.24,
                  border: 1,
                  borderColor: "info.main",
                  borderRadius: 1,
                  pointerEvents: "auto"
                }} />
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
              enterTouchDelay={tooltipTouchDelayMs}
              leaveTouchDelay={3000}
              title={medicationLabel(event, u.t("healthCare:medication_fallback"))}
            >
              <Box
                sx={{
                  position: "absolute",
                  top: 0,
                  bottom: 0,
                  left: `${left}%`,
                  width: medicationLineWidth,
                  transform: "translateX(-50%)",
                  bgcolor: "#ffffff",
                  border: "1px solid rgba(15, 23, 42, 0.2)",
                  boxShadow: "0 1px 4px rgba(15, 23, 42, 0.18)",
                  borderRadius: 999,
                  pointerEvents: "auto"
                }} />
            </Tooltip>
          );
        })}
      </Box>
    </Box>
  );
};

export default HealthCareChart;
