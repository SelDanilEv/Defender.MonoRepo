import { useCallback, useEffect, useMemo, useState } from "react";
import type { ChangeEvent } from "react";
import {
  Box,
  Card,
  CardContent,
  LinearProgress,
  MenuItem,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TablePagination,
  TableRow,
  TextField,
  Typography,
} from "@mui/material";
import LocalHospitalIcon from "@mui/icons-material/LocalHospital";
import { useParams } from "react-router";
import { healthCareApi, HealthChartShare, HealthEvent } from "src/api/healthCare";
import useUtils from "src/appUtils";
import LanguageSwitcher from "src/components/LanguageSwitcher";
import {
  ChartTimeRange,
  filterEventsByTimeRange,
  paginateHealthEvents,
  wellbeingScoreToEmoji,
} from "./chartData";
import HealthCareChart from "./HealthCareChart";
import { getNextDisplayedShare } from "./ShareState";
import WellbeingSummary from "./WellbeingSummary";

const formatEvent = (event: HealthEvent, t: (key: string, options?: object) => string) => {
  if (event.type === "Temperature") {
    return event.temperatureCelsius === undefined || event.temperatureCelsius === null
      ? "-"
      : `${event.temperatureCelsius.toFixed(1)} \u00b0C`;
  }

  if (event.type === "Medication") return `${event.medicationName || t("healthCare:medication_fallback")} ${event.medicationAmount || ""} ${event.medicationUnit || ""}`;
  if (event.type === "Wellbeing") return `${wellbeingScoreToEmoji(event.wellbeingScore)} ${event.wellbeingScore || ""}/5`;
  const time = new Date(event.endedAt || event.startedAt).toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" });
  return t("healthCare:sleep_until", { time });
};

const formatSharedRange = (
  from: string | undefined,
  to: string | undefined,
  fallback: string
) => {
  if (!from && !to) {
    return fallback;
  }

  const formatDate = (value?: string) =>
    value
      ? new Date(value).toLocaleString([], {
          day: "2-digit",
          month: "2-digit",
          year: "numeric",
          hour: "2-digit",
          minute: "2-digit",
        })
      : fallback;

  return `${formatDate(from)} - ${formatDate(to)}`;
};

const publicShareRefreshIntervalMs = 10000;

const HealthCareSharePage = () => {
  const u = useUtils();
  const { token } = useParams();
  const [share, setShare] = useState<HealthChartShare | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [chartTimeRange, setChartTimeRange] = useState<ChartTimeRange>("week");
  const [page, setPage] = useState(0);
  const [rowsPerPage, setRowsPerPage] = useState(10);
  const events = useMemo(() => share?.events ?? [], [share?.events]);
  const rangeAnchor = useMemo(
    () => (share?.to ? new Date(share.to) : undefined),
    [share?.to]
  );
  const visibleEvents = useMemo(
    () => filterEventsByTimeRange(events, chartTimeRange, rangeAnchor),
    [events, chartTimeRange, rangeAnchor]
  );
  const pagedEvents = useMemo(
    () => paginateHealthEvents(visibleEvents, page, rowsPerPage),
    [visibleEvents, page, rowsPerPage]
  );

  const refreshShare = useCallback((showLoading = false) => {
    if (!token) {
      setShare(null);
      setIsLoading(false);
      return Promise.resolve();
    }

    if (showLoading) {
      setIsLoading(true);
    }

    return healthCareApi
      .getPublicShare(token, u)
      .then((fetchedShare) => {
        setShare((currentShare) =>
          getNextDisplayedShare(currentShare, fetchedShare, showLoading)
        );
      })
      .finally(() => {
        if (showLoading) {
          setIsLoading(false);
        }
      });
  }, [token]); // eslint-disable-line react-hooks/exhaustive-deps

  useEffect(() => {
    refreshShare(true);

    if (!token) {
      return undefined;
    }

    const intervalId = window.setInterval(() => {
      refreshShare();
    }, publicShareRefreshIntervalMs);

    return () => window.clearInterval(intervalId);
  }, [refreshShare, token]);

  useEffect(() => {
    const maxPage =
      visibleEvents.length === 0
        ? 0
        : Math.max(0, Math.ceil(visibleEvents.length / rowsPerPage) - 1);

    setPage((currentPage) => Math.min(currentPage, maxPage));
  }, [visibleEvents.length, rowsPerPage]);

  const handleChangePage = (_event: unknown, newPage: number) => {
    setPage(newPage);
  };

  const handleChangeRowsPerPage = (
    event: ChangeEvent<HTMLInputElement | HTMLTextAreaElement>
  ) => {
    setRowsPerPage(parseInt(event.target.value, 10));
    setPage(0);
  };

  const formatType = (eventType: HealthEvent["type"]) => {
    if (eventType === "Temperature") return u.t("healthCare:event_temperature");
    if (eventType === "Medication") return u.t("healthCare:event_medication");
    if (eventType === "Wellbeing") return u.t("healthCare:event_wellbeing");
    return u.t("healthCare:event_sleep");
  };

  const content = isLoading ? (
    <LinearProgress />
  ) : !share ? (
    <Typography color="text.secondary">
      {u.t("healthCare:share_unavailable")}
    </Typography>
  ) : (
    <>
      <Stack direction={{ xs: "column", sm: "row" }} alignItems={{ xs: "stretch", sm: "center" }} justifyContent="space-between" gap={1} mb={1}>
        <Typography variant="h4">{u.t("healthCare:events_chart")}</Typography>
        <TextField
          select
          label={u.t("healthCare:chart_time_range")}
          value={chartTimeRange}
          onChange={(event) => setChartTimeRange(event.target.value as ChartTimeRange)}
          size="small"
          sx={{ minWidth: 180 }}
        >
          <MenuItem value="day">{u.t("healthCare:range_day")}</MenuItem>
          <MenuItem value="week">{u.t("healthCare:range_week")}</MenuItem>
          <MenuItem value="month">{u.t("healthCare:range_month")}</MenuItem>
          <MenuItem value="all">{u.t("healthCare:range_all")}</MenuItem>
        </TextField>
      </Stack>
      <Typography variant="body2" color="text.secondary" mb={1.5}>
        {u.t("healthCare:shared_range", {
          range: formatSharedRange(share?.from, share?.to, u.t("healthCare:range_all")),
        })}
      </Typography>
      <WellbeingSummary
        events={visibleEvents}
        timeRange="all"
        title={u.t("healthCare:latest_wellbeing")}
        scoreLabel={(score) => u.t("healthCare:wellbeing_score", { score })}
      />
      <HealthCareChart events={visibleEvents} timeRange="all" />
      <Typography variant="h4" mt={3} mb={1}>{u.t("healthCare:events_grid")}</Typography>
      <TableContainer>
        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell>{u.t("healthCare:grid_started_at")}</TableCell>
              <TableCell>{u.t("healthCare:grid_type")}</TableCell>
              <TableCell>{u.t("healthCare:grid_value")}</TableCell>
              <TableCell>{u.t("healthCare:notes")}</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {pagedEvents.map((event) => (
              <TableRow key={event.id}>
                <TableCell>
                  {new Date(event.startedAt).toLocaleString([], {
                    day: "2-digit",
                    month: "2-digit",
                    hour: "2-digit",
                    minute: "2-digit",
                  })}
                </TableCell>
                <TableCell>{formatType(event.type)}</TableCell>
                <TableCell>{formatEvent(event, u.t)}</TableCell>
                <TableCell>{event.notes || ""}</TableCell>
              </TableRow>
            ))}
            {visibleEvents.length === 0 && (
              <TableRow>
                <TableCell colSpan={4}>
                  <Typography color="text.secondary">{u.t("healthCare:no_events_to_display")}</Typography>
                </TableCell>
              </TableRow>
            )}
          </TableBody>
        </Table>
      </TableContainer>
      {visibleEvents.length > 0 && (
        <Box
          sx={{
            display: "flex",
            alignItems: "center",
            justifyContent: "flex-end",
            flexWrap: "wrap",
            gap: 0.5,
            px: 1.5,
            pt: 0.5,
          }}
        >
          <Typography
            variant="body2"
            sx={{
              textAlign: "center",
              fontSize: { xs: "0.68rem", sm: "0.8rem" },
              lineHeight: 1.2,
            }}
          >
            {u.t("table_rows_per_page_label")}
          </Typography>
          <TablePagination
            component="div"
            count={visibleEvents.length}
            page={page}
            onPageChange={handleChangePage}
            rowsPerPage={rowsPerPage}
            onRowsPerPageChange={handleChangeRowsPerPage}
            rowsPerPageOptions={[10, 25, 50]}
            labelRowsPerPage=""
          />
        </Box>
      )}
    </>
  );

  return (
    <Box p={2}>
      <Stack direction="row" alignItems="center" justifyContent="space-between" gap={1} mb={2}>
        <Stack direction="row" alignItems="center" gap={1}>
          <LocalHospitalIcon color="primary" />
          <Typography variant="h3">{u.t("healthCare:share_page_title")}</Typography>
        </Stack>
        <LanguageSwitcher />
      </Stack>
      <Typography color="text.secondary" mb={2}>
        {u.t("healthCare:share_description")}
      </Typography>
      <Card><CardContent>{content}</CardContent></Card>
    </Box>
  );
};

export default HealthCareSharePage;
