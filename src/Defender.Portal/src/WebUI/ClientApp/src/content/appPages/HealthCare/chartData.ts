import { HealthEvent } from "src/api/healthCare";
import {
  filterEventsByDateRange,
  HealthDateRangePreset,
  HealthDateRangeSelection,
  resolveHealthDateRange,
} from "./dateRange";

export const eventAxisMin = 0.5;
export const eventAxisMax = 2.5;
export const medicationLane = 1;
export const sleepLane = 2;
export type ChartTimeRange = HealthDateRangePreset;
export type HealthChartRange = ChartTimeRange | HealthDateRangeSelection;

const wellbeingEmojiByScore = ["😢", "😟", "😐", "🙂", "😄"];

const toSelection = (timeRange: HealthChartRange): HealthDateRangeSelection =>
  typeof timeRange === "string"
    ? { kind: "preset", preset: timeRange }
    : timeRange;

export const filterEventsByTimeRange = (
  events: HealthEvent[],
  timeRange: HealthChartRange,
  now = new Date()
) => {
  return filterEventsByDateRange(events, toSelection(timeRange), now);
};

export const getTimeRangeBounds = (
  timeRange: HealthChartRange,
  now = new Date()
) => {
  return resolveHealthDateRange(toSelection(timeRange), now);
};

export const wellbeingScoreToEmoji = (score?: number) => {
  if (!score) {
    return "";
  }

  const normalizedScore = Math.max(1, Math.min(5, Math.round(score)));

  return wellbeingEmojiByScore[normalizedScore - 1];
};

export const getLatestWellbeingEvent = (
  events: HealthEvent[],
  timeRange: HealthChartRange = "all",
  now = new Date()
) =>
  [...filterEventsByTimeRange(events, timeRange, now)]
    .filter(
      (event) =>
        event.type === "Wellbeing" && event.wellbeingScore !== undefined
    )
    .sort(
      (left, right) =>
        new Date(right.startedAt).getTime() -
        new Date(left.startedAt).getTime()
    )[0];

export const paginateHealthEvents = (
  events: HealthEvent[],
  page: number,
  rowsPerPage: number
) => {
  const startIndex = page * rowsPerPage;

  return events.slice(startIndex, startIndex + rowsPerPage);
};

const eventTimeLabel = (event: HealthEvent) =>
  new Date(event.startedAt).toLocaleString([], {
    day: "2-digit",
    month: "2-digit",
    hour: "2-digit",
    minute: "2-digit",
  });

export const buildHealthCareChartData = (
  events: HealthEvent[],
  timeRange: HealthChartRange = "all",
  anchor = new Date()
) => {
  const chartEvents = [...filterEventsByTimeRange(events, timeRange, anchor)].sort(
    (left, right) =>
      new Date(left.startedAt).getTime() - new Date(right.startedAt).getTime()
  );
  const temperatureEvents = chartEvents.filter(
    (event) =>
      event.type === "Temperature" && event.temperatureCelsius !== undefined
  );
  const bounds = getTimeRangeBounds(timeRange, anchor);
  const eventTimes = chartEvents.flatMap((event) => [
    new Date(event.startedAt).getTime(),
    event.endedAt ? new Date(event.endedAt).getTime() : new Date(event.startedAt).getTime(),
  ]);
  const fallbackNow = Date.now();
  const minTime =
    bounds.from?.getTime() ??
    (eventTimes.length > 0 ? Math.min(...eventTimes) : fallbackNow - 60 * 60 * 1000);
  const maxTime =
    bounds.to?.getTime() ??
    (eventTimes.length > 0 ? Math.max(...eventTimes) : fallbackNow);

  return {
    chartEvents,
    temperatureEvents,
    minTime,
    maxTime: maxTime === minTime ? minTime + 60 * 60 * 1000 : maxTime,
    xLabels: chartEvents.map(eventTimeLabel),
    temperatureXAxis: temperatureEvents.map((event) => new Date(event.startedAt)),
    temperatureData: temperatureEvents.map((event) => event.temperatureCelsius ?? null),
    medicationData: chartEvents.map((event) =>
      event.type === "Medication" ? medicationLane : null
    ),
    sleepData: chartEvents.map((event) =>
      event.type === "Sleep" ? sleepLane : null
    ),
  };
};
