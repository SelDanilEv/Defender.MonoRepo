import type { HealthChartShare } from "src/api/healthCare";

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
