import { HealthEvent } from "src/api/healthCare";

export const eventAxisMin = 0.5;
export const eventAxisMax = 2.5;
export const medicationLane = 1;
export const sleepLane = 2;

const eventTimeLabel = (event: HealthEvent) =>
  new Date(event.startedAt).toLocaleString([], {
    day: "2-digit",
    month: "2-digit",
    hour: "2-digit",
    minute: "2-digit",
  });

export const buildHealthCareChartData = (events: HealthEvent[]) => {
  const chartEvents = [...events].sort(
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
