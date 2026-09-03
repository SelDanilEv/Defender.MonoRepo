import { useEffect, useRef, useState } from "react";
import { Alert, Box, Card, CardContent, CardHeader, CircularProgress, FormControl, Grid, InputLabel, MenuItem, Stack, Typography, useTheme } from "@mui/material";
import { LineChart } from "@mui/x-charts/LineChart";
import dayjs from "dayjs";

import useUtils from "src/appUtils";
import LockedSelect from "src/components/LockedComponents/LockedSelect/LockedSelect";
import LockedTextField from "src/components/LockedComponents/LockedTextField/LockedTextField";
import CurrencySymbolsMap from "src/consts/CurrencySymbolsMap";
import { BudgetTrackerSupportedCurrencies } from "src/consts/SupportedCurrencies";
import { Currency } from "src/models/shared/Currency";
import type {
  RegularExpenseDiagramSetup,
  RegularExpenseReview,
} from "src/models/budgetTracker/regularExpenses";

import {
  getRegularExpenseDiagramSetup,
  getRegularExpenseReviewsByDateRange,
  normalizeMonth,
  saveRegularExpenseDiagramSetup,
} from "../api";
import { buildRegularExpenseChartDataset } from "./chartData";

const DEFAULT_LAST_MONTHS = 12;
const DEFAULT_MAIN_CURRENCY = Currency.USD;

const currentMonth = (): string => dayjs().startOf("month").format("YYYY-MM");

const normalizeSetup = (
  setup?: Partial<RegularExpenseDiagramSetup> | null,
): RegularExpenseDiagramSetup => {
  const requestedCurrency = setup?.mainCurrency ?? DEFAULT_MAIN_CURRENCY;
  const mainCurrency = BudgetTrackerSupportedCurrencies.includes(requestedCurrency)
    ? (requestedCurrency as Currency)
    : DEFAULT_MAIN_CURRENCY;
  const lastMonths =
    typeof setup?.lastMonths === "number" && setup.lastMonths > 0
      ? Math.floor(setup.lastMonths)
      : DEFAULT_LAST_MONTHS;

  return {
    id: setup?.id,
    userId: setup?.userId,
    mainCurrency,
    lastMonths,
    endMonth: setup?.endMonth ? normalizeMonth(setup.endMonth).slice(0, 7) : currentMonth(),
  };
};

const getReviewRange = (endMonthValue?: string, lastMonthsValue?: number) => {
  const end = dayjs(`${endMonthValue ?? currentMonth()}-01`);
  const lastMonths = Math.max(1, lastMonthsValue ?? DEFAULT_LAST_MONTHS);

  return {
    startMonth: end.subtract(lastMonths - 1, "month").format("YYYY-MM-01"),
    endMonth: end.format("YYYY-MM-01"),
  };
};

const formatAmount = (amount: number, currency: Currency): string =>
  `${amount.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })} ${CurrencySymbolsMap[currency] || currency}`;

const formatMonth = (month: string): string =>
  dayjs(`${month}-01`).format("MMM YYYY");

const typeSeries = [
  { key: "regular", label: "budgetTracker:regular_expenses_chart_regular", color: "primary" },
  { key: "subscription", label: "budgetTracker:regular_expenses_chart_subscription", color: "secondary" },
  { key: "annual", label: "budgetTracker:regular_expenses_chart_annual", color: "warning" },
] as const;

const RegularExpensesDiagramPage = () => {
  const u = useUtils();
  const theme = useTheme();
  const utilsRef = useRef(u);
  utilsRef.current = u;

  const [setup, setSetup] = useState<RegularExpenseDiagramSetup>(() => normalizeSetup());
  const [setupLoaded, setSetupLoaded] = useState(false);
  const [reviews, setReviews] = useState<RegularExpenseReview[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(false);

  useEffect(() => {
    let cancelled = false;

    getRegularExpenseDiagramSetup(utilsRef.current)
      .then((loadedSetup) => {
        if (cancelled) return;
        setSetup(normalizeSetup(loadedSetup));
      })
      .catch(() => {
        if (!cancelled) setError(true);
      })
      .finally(() => {
        if (!cancelled) setSetupLoaded(true);
      });

    return () => {
      cancelled = true;
    };
  }, []);

  useEffect(() => {
    if (!setupLoaded) return;

    let cancelled = false;
    const range = getReviewRange(setup.endMonth, setup.lastMonths);
    setLoading(true);

    getRegularExpenseReviewsByDateRange(
      utilsRef.current,
      range.startMonth,
      range.endMonth,
    )
      .then((loadedReviews) => {
        if (cancelled) return;
        setReviews(loadedReviews ?? []);
        setError(false);
      })
      .catch(() => {
        if (cancelled) return;
        setReviews([]);
        setError(true);
      })
      .finally(() => {
        if (!cancelled) setLoading(false);
      });

    return () => {
      cancelled = true;
    };
  }, [setupLoaded, setup.endMonth, setup.lastMonths]);

  const persistSetup = (nextSetup: RegularExpenseDiagramSetup) => {
    setSetup(nextSetup);
    void saveRegularExpenseDiagramSetup(utilsRef.current, nextSetup)
      .catch(() => setError(true));
  };

  const updateCurrency = (value: Currency) => {
    persistSetup({ ...setup, mainCurrency: value });
  };

  const updateLastMonths = (value: string) => {
    const parsed = Number(value);
    if (!Number.isFinite(parsed) || parsed < 1) return;
    setSetup((current) => ({ ...current, lastMonths: Math.min(120, Math.floor(parsed)) }));
  };

  const persistLastMonths = () => {
    const nextSetup = normalizeSetup(setup);
    persistSetup(nextSetup);
  };

  const updateEndMonth = (value: string) => {
    if (!/^\d{4}-\d{2}$/.test(value)) return;
    persistSetup({ ...setup, endMonth: value });
  };

  const dataset = buildRegularExpenseChartDataset(reviews, setup.mainCurrency);
  const latest = dataset[dataset.length - 1];
  const currencySymbol = CurrencySymbolsMap[setup.mainCurrency] || setup.mainCurrency;
  const chartSeries = [
    ...typeSeries.map((series) => ({
      dataKey: series.key,
      label: u.t(series.label),
      color: theme.palette[series.color].main,
      curve: "monotoneX" as const,
      showMark: true,
      valueFormatter: (value: number | null) =>
        value === null ? "" : formatAmount(value, setup.mainCurrency),
    })),
    {
      dataKey: "totalComfort",
      label: u.t("budgetTracker:regular_expenses_chart_total"),
      color: theme.palette.text.primary,
      curve: "monotoneX" as const,
      showMark: true,
      valueFormatter: (value: number | null) =>
        value === null ? "" : formatAmount(value, setup.mainCurrency),
    },
  ];

  return (
    <Box sx={{ width: "100%" }}>
      <Card>
        <CardHeader
          title={
            <Typography sx={{ fontSize: "1.7em", fontWeight: "bold" }}>
              {u.t("budgetTracker:regular_expenses_chart_title")}
            </Typography>
          }
        />
        <CardContent>
          <Grid container spacing={2}>
            <Grid size={{ xs: 12, sm: 4 }}>
              <FormControl fullWidth>
                <InputLabel id="regular-expenses-main-currency-label">
                  {u.t("budgetTracker:regular_expenses_main_currency_label")}
                </InputLabel>
                <LockedSelect
                  labelId="regular-expenses-main-currency-label"
                  label={u.t("budgetTracker:regular_expenses_main_currency_label")}
                  value={setup.mainCurrency}
                  onChange={(event) => updateCurrency(event.target.value as Currency)}
                >
                  {BudgetTrackerSupportedCurrencies.map((currency) => (
                    <MenuItem key={currency} value={currency}>
                      {currency}
                    </MenuItem>
                  ))}
                </LockedSelect>
              </FormControl>
            </Grid>
            <Grid size={{ xs: 12, sm: 4 }}>
              <LockedTextField
                fullWidth
                type="month"
                label={u.t("budgetTracker:regular_expenses_end_month_label")}
                value={setup.endMonth ?? ""}
                onChange={(event) => updateEndMonth(event.target.value)}
                slotProps={{ inputLabel: { shrink: true } }}
              />
            </Grid>
            <Grid size={{ xs: 12, sm: 4 }}>
              <LockedTextField
                fullWidth
                type="number"
                label={u.t("budgetTracker:regular_expenses_last_months_label")}
                value={setup.lastMonths ?? DEFAULT_LAST_MONTHS}
                slotProps={{ htmlInput: { min: 1, max: 120, step: 1, inputMode: "numeric" } }}
                onChange={(event) => updateLastMonths(event.target.value)}
                onBlur={persistLastMonths}
              />
            </Grid>
          </Grid>
        </CardContent>
      </Card>

      {error && (
        <Alert severity="error" sx={{ mt: 2 }}>
          {u.t("budgetTracker:regular_expenses_diagram_error")}
        </Alert>
      )}

      {loading ? (
        <Box role="status" sx={{ display: "flex", justifyContent: "center", py: 8 }}>
          <CircularProgress aria-label={u.t("budgetTracker:regular_expenses_diagram_loading")} />
        </Box>
      ) : dataset.length === 0 ? (
        <Card sx={{ mt: 2 }}>
          <CardContent>
            <Typography color="text.secondary" role="status" sx={{ py: 5, textAlign: "center" }}>
              {u.t("budgetTracker:regular_expenses_chart_empty")}
            </Typography>
          </CardContent>
        </Card>
      ) : (
        <Card sx={{ mt: 2 }}>
          {latest && (
            <CardContent sx={{ pb: 0 }}>
              <Stack direction={{ xs: "column", sm: "row" }} spacing={1} useFlexGap sx={{ flexWrap: "wrap" }}>
                <Typography sx={{ fontWeight: "bold" }}>
                  {formatMonth(latest.month)}: {formatAmount(latest.totalComfort, setup.mainCurrency)}
                </Typography>
                <Typography color="text.secondary">
                  {u.t("budgetTracker:regular_expenses_chart_total")} ({currencySymbol})
                </Typography>
              </Stack>
            </CardContent>
          )}
          <Box role="img" aria-label={u.t("budgetTracker:regular_expenses_chart_title")} sx={{ width: "100%", overflowX: "auto", px: { xs: 0, xl: 2 } }}>
            <LineChart
              dataset={dataset as unknown as Record<string, unknown>[]}
              height={u.isLargeScreen ? 480 : 360}
              margin={{ left: u.isLargeScreen ? 80 : 60, right: u.isLargeScreen ? 40 : 20, top: u.isLargeScreen ? 24 : 16, bottom: u.isLargeScreen ? 80 : 56 }}
              series={chartSeries}
              xAxis={[{
                scaleType: "point",
                dataKey: "month",
                valueFormatter: (value: string) => formatMonth(value),
              }]}
              yAxis={[{
                valueFormatter: (value: number) => formatAmount(value, setup.mainCurrency),
              }]}
              grid={{ horizontal: true }}
              slotProps={{
                legend: {
                  direction: "horizontal",
                  position: { vertical: "bottom", horizontal: "center" },
                },
              }}
            />
          </Box>
        </Card>
      )}
    </Box>
  );
};

export default RegularExpensesDiagramPage;
