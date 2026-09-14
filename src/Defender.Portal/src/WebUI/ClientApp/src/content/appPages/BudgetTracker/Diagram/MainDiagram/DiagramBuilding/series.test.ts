import dayjs from "dayjs";

import { Currency } from "src/models/shared/Currency";
import { BudgetDiagramGroup } from "src/models/budgetTracker/BudgetDiagramGroups";
import { DatasetItem } from "src/models/budgetTracker/diagramData/DatasetItem";
import IUtils from "src/appUtils/interface";

import { generateSeries } from "./series";
import { buildDatasetItemId, getTrendLineName } from "./convention";

const dataKey = buildDatasetItemId(Currency.USD, "group1");

const buildGroup = (): BudgetDiagramGroup => ({
  id: "group1",
  name: "Group1",
  isActive: true,
  tags: [],
  mainColor: "#336699",
  showTrendLine: true,
  trendLineColor: "#996633",
});

const buildUtils = (): IUtils =>
  ({
    t: (key: string) => key,
  } as unknown as IUtils);

const findTrendSeriesData = (dataset: DatasetItem[]): number[] => {
  const group = buildGroup();
  const u = buildUtils();

  const series = generateSeries(dataset, [group], u);

  const trendLabel = getTrendLineName(Currency.USD, group.name, u);
  const trendSeries = series.find((s) => s.label === trendLabel);

  expect(trendSeries).toBeDefined();
  expect(trendSeries.curve).toBe("linear");

  return trendSeries.data as number[];
};

const isCollinear = (
  a: { x: number; y: number },
  b: { x: number; y: number },
  c: { x: number; y: number },
  epsilon: number
): boolean => {
  // Twice the triangle area formed by three points; zero for collinear points.
  const area = (b.x - a.x) * (c.y - a.y) - (c.x - a.x) * (b.y - a.y);
  return Math.abs(area) < epsilon;
};

describe("generateSeries trend line", () => {
  test("WhenRecordsAreIrregularlySpacedInTime_TrendPointsStayCollinearOnTheDateAxis", () => {
    const firstDate = dayjs().subtract(100, "day");

    const daysFromFirst = [0, 2, 9, 11, 27];

    const historicalRecords: DatasetItem[] = daysFromFirst.map((days) => {
      const date = firstDate.add(days, "day").toDate();

      return {
        date,
        [dataKey]: 100 + 5 * days,
      } as DatasetItem;
    });

    const futureRecords: DatasetItem[] = [10, 40].map((days) => ({
      date: dayjs().add(days, "day").toDate(),
      [dataKey]: null,
    })) as DatasetItem[];

    const dataset = [...historicalRecords, ...futureRecords];

    const trendData = findTrendSeriesData(dataset);

    expect(trendData).toHaveLength(dataset.length);

    const points = dataset.map((record, index) => ({
      x: dayjs(record.date).valueOf(),
      y: trendData[index],
    }));

    for (let i = 0; i < points.length - 2; i++) {
      expect(
        isCollinear(points[i], points[i + 1], points[i + 2], 1e-6)
      ).toBe(true);
    }
  });

  test("WhenFewerThanTwoHistoricalPointsHaveData_ReturnsEmptySeriesData", () => {
    const dataset: DatasetItem[] = [
      {
        date: dayjs().subtract(5, "day").toDate(),
        [dataKey]: 42,
      } as DatasetItem,
      {
        date: dayjs().add(5, "day").toDate(),
        [dataKey]: null,
      } as DatasetItem,
    ];

    expect(findTrendSeriesData(dataset)).toEqual([]);
  });

  test("WhenAllFittingPointsShareTheSameDate_ReturnsEmptySeriesDataWithoutThrowing", () => {
    const sameDate = dayjs().subtract(3, "day").toDate();

    const dataset: DatasetItem[] = [
      { date: sameDate, [dataKey]: 10 } as DatasetItem,
      { date: sameDate, [dataKey]: 20 } as DatasetItem,
      { date: sameDate, [dataKey]: 30 } as DatasetItem,
    ];

    expect(() => findTrendSeriesData(dataset)).not.toThrow();
    expect(findTrendSeriesData(dataset)).toEqual([]);
  });
});
