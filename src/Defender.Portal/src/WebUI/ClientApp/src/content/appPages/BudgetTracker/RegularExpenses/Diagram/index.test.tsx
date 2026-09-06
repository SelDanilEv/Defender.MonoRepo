import { render, waitFor } from "@testing-library/react";
import { ThemeProvider, createTheme } from "@mui/material/styles";
import { configureStore } from "@reduxjs/toolkit";
import { Provider } from "react-redux";
import { describe, expect, test, vi } from "vitest";

import { Currency } from "src/models/shared/Currency";

import RegularExpensesDiagramPage from ".";

const appUtilsState = vi.hoisted(() => ({ isLargeScreen: true }));
const chartState = vi.hoisted(() => ({
  props: null as { margin?: { bottom?: number } } | null,
}));
const apiMocks = vi.hoisted(() => ({
  getSetup: vi.fn(),
  getReviews: vi.fn(),
  saveSetup: vi.fn(),
}));

vi.mock("@mui/x-charts/LineChart", () => ({
  LineChart: (props: { margin?: { bottom?: number } }) => {
    chartState.props = props;
    return null;
  },
}));

vi.mock("src/appUtils", () => ({
  default: () => ({
    isLargeScreen: appUtilsState.isLargeScreen,
    isMobile: false,
    t: (key: string) => key,
  }),
}));

vi.mock("../api", () => ({
  getRegularExpenseDiagramSetup: apiMocks.getSetup,
  getRegularExpenseReviewsByDateRange: apiMocks.getReviews,
  saveRegularExpenseDiagramSetup: apiMocks.saveSetup,
  normalizeMonth: (value: string) => `${value.slice(0, 7)}-01`,
}));

const renderDiagram = async (isLargeScreen: boolean) => {
  appUtilsState.isLargeScreen = isLargeScreen;
  chartState.props = null;
  const store = configureStore({
    reducer: { loading: (state = { loading: false }) => state },
  });

  render(
    <Provider store={store}>
      <ThemeProvider theme={createTheme()}>
        <RegularExpensesDiagramPage />
      </ThemeProvider>
    </Provider>,
  );

  await waitFor(() => expect(chartState.props).not.toBeNull());
};

describe("Regular expenses diagram layout", () => {
  beforeEach(() => {
    apiMocks.getSetup.mockResolvedValue({
      mainCurrency: Currency.USD,
      lastMonths: 12,
      endMonth: "2026-08-01",
    });
    apiMocks.getReviews.mockResolvedValue([
      {
        id: "review-1",
        userId: "user-1",
        month: "2026-08-01",
        expenses: [],
        ratesModel: {
          date: "2026-08-01",
          baseCurrency: Currency.USD,
          rates: { [Currency.USD]: 1 },
        },
      },
    ]);
  });

  test("WhenLargeScreen_RendersLegendWithoutBottomChartGap", async () => {
    await renderDiagram(true);

    expect(chartState.props?.margin?.bottom).toBe(0);
  });

  test("WhenCompact_RendersLegendWithoutBottomChartGap", async () => {
    await renderDiagram(false);

    expect(chartState.props?.margin?.bottom).toBe(0);
  });
});
