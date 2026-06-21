import { useEffect, useMemo, useState } from "react";
import { Box, Card, CardContent, Chip, MenuItem, Stack, TextField, Typography } from "@mui/material";
import LocalHospitalIcon from "@mui/icons-material/LocalHospital";
import { useParams } from "react-router";
import { healthCareApi, HealthChartShare, HealthEvent } from "src/api/healthCare";
import useUtils from "src/appUtils";
import LanguageSwitcher from "src/components/LanguageSwitcher";
import {
  ChartTimeRange,
  filterEventsByTimeRange,
  wellbeingScoreToEmoji,
} from "./chartData";
import HealthCareChart from "./HealthCareChart";
import WellbeingSummary from "./WellbeingSummary";

const formatEvent = (event: HealthEvent, t: (key: string, options?: object) => string) => {
  if (event.type === "Temperature") return `${event.temperatureCelsius} C`;
  if (event.type === "Medication") return `${event.medicationName || t("healthCare:medication_fallback")} ${event.medicationAmount || ""} ${event.medicationUnit || ""}`;
  if (event.type === "Wellbeing") return `${wellbeingScoreToEmoji(event.wellbeingScore)} ${event.wellbeingScore || ""}/5`;
  const time = new Date(event.endedAt || event.startedAt).toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" });
  return t("healthCare:sleep_until", { time });
};

const chipColor = (event: HealthEvent) => {
  if (event.type === "Sleep") return "info";
  if (event.type === "Medication") return "secondary";
  if (event.type === "Wellbeing") return "success";

  return "primary";
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

const HealthCareSharePage = () => {
  const u = useUtils();
  const { token } = useParams();
  const [share, setShare] = useState<HealthChartShare | null>(null);
  const [chartTimeRange, setChartTimeRange] = useState<ChartTimeRange>("all");
  const events = useMemo(() => share?.events ?? [], [share?.events]);
  const rangeAnchor = useMemo(
    () => (share?.to ? new Date(share.to) : undefined),
    [share?.to]
  );
  const visibleEvents = useMemo(
    () => filterEventsByTimeRange(events, chartTimeRange, rangeAnchor),
    [events, chartTimeRange, rangeAnchor]
  );

  useEffect(() => {
    if (!token) {
      setShare(null);
      return;
    }

    healthCareApi.getPublicShare(token, u).then((share) => {
      setShare(share);
    });
  }, [token]); // eslint-disable-line react-hooks/exhaustive-deps

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
      <Card><CardContent>
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
        <Stack direction="row" flexWrap="wrap" gap={1} mt={2}>
          {visibleEvents.map((event) => (
            <Chip key={event.id} label={`${new Date(event.startedAt).toLocaleString([], { day: "2-digit", month: "2-digit", hour: "2-digit", minute: "2-digit" })}: ${formatEvent(event, u.t)}`} color={chipColor(event)} variant="outlined" />
          ))}
        </Stack>
      </CardContent></Card>
    </Box>
  );
};

export default HealthCareSharePage;
