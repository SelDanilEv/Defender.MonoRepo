import type { HealthChartShare } from "src/api/healthCare";
import type {
  DateRangeBounds,
  HealthDateRangeSelection,
} from "./dateRange";

export const getNextDisplayedShare = (
  currentShare: HealthChartShare | null,
  fetchedShare: HealthChartShare | null,
  showLoading: boolean
) => {
  if (!showLoading && !fetchedShare && currentShare) {
    return currentShare;
  }

  return fetchedShare;
};

const parseShareDate = (value?: string) => {
  if (!value) {
    return undefined;
  }

  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? undefined : date;
};

const inferShareRangeMode = (share: HealthChartShare) =>
  share.rangeMode ??
  (share.from && share.to ? "Rolling" : share.from || share.to ? "Absolute" : "All");

export const getHealthShareAllowedBounds = (
  share: HealthChartShare
): DateRangeBounds => ({
  from: parseShareDate(share.from),
  to: parseShareDate(share.to),
});

export const getInitialHealthShareSelection = (
  share: HealthChartShare
): HealthDateRangeSelection => {
  const from = parseShareDate(share.from);
  const to = parseShareDate(share.to);

  if (inferShareRangeMode(share) === "Absolute" && from && to && from < to) {
    return { kind: "custom", from, to };
  }

  return {
    kind: "preset",
    preset: inferShareRangeMode(share) === "All" ? "all" : "week",
  };
};

export const clampHealthDateRangeSelection = (
  selection: HealthDateRangeSelection,
  allowedBounds: DateRangeBounds
): HealthDateRangeSelection => {
  if (selection.kind !== "custom") {
    return selection;
  }

  const from = new Date(
    Math.max(selection.from.getTime(), allowedBounds.from?.getTime() ?? Number.MIN_SAFE_INTEGER)
  );
  const to = new Date(
    Math.min(selection.to.getTime(), allowedBounds.to?.getTime() ?? Number.MAX_SAFE_INTEGER)
  );

  if (from > to) {
    return { kind: "preset", preset: "all" };
  }

  return { kind: "custom", from, to };
};
