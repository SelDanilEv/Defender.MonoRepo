import chroma from "chroma-js";
import dayjs from "dayjs";

import { AllAvailableCurrencies } from "src/models/shared/Currency";
import { BudgetDiagramGroup } from "src/models/budgetTracker/BudgetDiagramGroups";
import { DatasetItem } from "src/models/budgetTracker/diagramData/DatasetItem";
import IUtils from "src/appUtils/interface";

import {
  buildDatasetItemId,
  getLabelName,
  getTrendLineName,
} from "./convention";
import { hasData } from "./dataset";

export const generateSeries = (
  dataset: DatasetItem[],
  groups: BudgetDiagramGroup[],
  u: IUtils
): any[] => {
  const series: any[] = [];

  groups.forEach((group) => {
    const currenciesWithData = AllAvailableCurrencies.filter((currency) =>
      hasData(currency, group.id, dataset)
    );

    const colors = generateSimilarColors(
      group.mainColor,
      currenciesWithData.length
    );

    const groupSeries = currenciesWithData.map((currency, index) => ({
      dataKey: buildDatasetItemId(currency, group.id),
      label: getLabelName(currency, group.name),
      type: "line",
      color: colors[index],
      connectNulls: true,
      curve: "monotoneX",
      showMark: ({ index }) =>
        index % (Math.round(dataset.length / 25) || 1) === 0,
    }));

    if (group.showTrendLine) {
      const trendLineColors = generateSimilarColors(
        group.trendLineColor,
        currenciesWithData.length
      );
      currenciesWithData.forEach((currency, index) => {
        const dataKey = buildDatasetItemId(currency, group.id);

        series.push({
          label: getTrendLineName(currency, group.name, u),
          type: "line",
          data: calculateTrendLine(dataset, dataKey),
          color: chroma(trendLineColors[index]).alpha(0.4).css(),
          connectNulls: true,
          curve: "linear",
          showMark: false,
        });
      });
    }

    series.push(...groupSeries);
  });

  return series;
};

const generateSimilarColors = (
  baseColor: string,
  numColors: number
): string[] => {
  return chroma
    .scale([baseColor, chroma(baseColor).darken(2)])
    .colors(numColors);
};

type TrendFitPoint = { timestampMs: number; value: number };

// Least-squares fit on the actual record date (ms since epoch), not the
// array index, so the trend line stays straight even when records are
// irregularly spaced along the time axis.
const calculateTrendLine = (
  dataset: DatasetItem[],
  dataKey: string
): number[] => {
  const startOfToday = dayjs().startOf("day");

  const fitPoints: TrendFitPoint[] = dataset
    .filter((record) => dayjs(record.date).isBefore(startOfToday))
    .filter(
      (record) => record[dataKey] !== null && record[dataKey] !== undefined
    )
    .map((record) => ({
      timestampMs: dayjs(record.date).valueOf(),
      value: record[dataKey] as number,
    }));

  if (fitPoints.length < 2) {
    return [];
  }

  // Normalize x around the first fitting point's timestamp to avoid
  // float precision loss when squaring raw millisecond epoch values.
  const originMs = fitPoints[0].timestampMs;

  const n = fitPoints.length;
  const sumX = fitPoints.reduce(
    (sum, p) => sum + (p.timestampMs - originMs),
    0
  );
  const sumY = fitPoints.reduce((sum, p) => sum + p.value, 0);
  const sumXY = fitPoints.reduce(
    (sum, p) => sum + (p.timestampMs - originMs) * p.value,
    0
  );
  const sumX2 = fitPoints.reduce(
    (sum, p) => sum + (p.timestampMs - originMs) * (p.timestampMs - originMs),
    0
  );

  const denominator = n * sumX2 - sumX * sumX;

  if (denominator === 0) {
    return [];
  }

  const slope = (n * sumXY - sumX * sumY) / denominator;
  const intercept = (sumY - slope * sumX) / n;

  return dataset.map(
    (record) =>
      slope * (dayjs(record.date).valueOf() - originMs) + intercept
  );
};
