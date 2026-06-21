import { HealthEvent } from "src/api/healthCare";

export const eventAxisMin = 0.5;
export const eventAxisMax = 2.5;
export const medicationLane = 1;
export const sleepLane = 2;
export type ChartTimeRange = "day" | "week" | "month" | "all";

const wellbeingEmojiByScore = ["😢", "😟", "😐", "🙂", "😄"];

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
  const bounds = getTimeRangeBounds(timeRange, now);

  if (!bounds.from) {
    return events;
  }

  return events.filter((event) => new Date(event.startedAt) >= bounds.from!);
};

export const getTimeRangeBounds = (
  timeRange: ChartTimeRange,
  now = new Date()
) => {
  const days = rangeDays[timeRange];

  if (!days) {
    return { from: undefined, to: undefined };
  }

  const from = new Date(now);
  from.setDate(from.getDate() - days);

  return { from, to: now };
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
  timeRange: ChartTimeRange = "all",
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
  timeRange: ChartTimeRange = "all"
) => {
  const chartEvents = [...filterEventsByTimeRange(events, timeRange)].sort(
    (left, right) =>
      new Date(left.startedAt).getTime() - new Date(right.startedAt).getTime()
  );
  const temperatureEvents = chartEvents.filter(
    (event) =>
      event.type === "Temperature" && event.temperatureCelsius !== undefined
  );
  const bounds = getTimeRangeBounds(timeRange);
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
