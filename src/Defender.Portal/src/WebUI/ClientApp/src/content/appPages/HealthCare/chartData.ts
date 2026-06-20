import { HealthEvent } from "src/api/healthCare";

export const eventAxisMin = 0.5;
export const eventAxisMax = 2.5;
export const medicationLane = 1;
export const sleepLane = 2;
export type ChartTimeRange = "day" | "week" | "month" | "all";

const rangeDays: Partial<Record<ChartTimeRange, number>> = {
  day: 1,
  week: 7,
  month: 30,
};

export const filterEventsByTimeRange = (
  events: HealthEvent[],
  timeRange: ChartTimeRange,
  now = new Date()
) => {
  const days = rangeDays[timeRange];

  if (!days) {
    return events;
  }

  const from = new Date(now);
  from.setDate(from.getDate() - days);

  return events.filter((event) => new Date(event.startedAt) >= from);
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
  timeRange: ChartTimeRange = "all"
) => {
  const chartEvents = [...filterEventsByTimeRange(events, timeRange)].sort(
    (left, right) =>
      new Date(left.startedAt).getTime() - new Date(right.startedAt).getTime()
  );

  return {
    chartEvents,
    xLabels: chartEvents.map(eventTimeLabel),
    temperatureData: chartEvents.map((event) =>
      event.type === "Temperature" && event.temperatureCelsius !== undefined
        ? event.temperatureCelsius
        : null
    ),
    medicationData: chartEvents.map((event) =>
      event.type === "Medication" ? medicationLane : null
    ),
    sleepData: chartEvents.map((event) =>
      event.type === "Sleep" ? sleepLane : null
    ),
  };
};
