import { useEffect, useState } from "react";
import { Box, Card, CardContent, Chip, Stack, Typography } from "@mui/material";
import LocalHospitalIcon from "@mui/icons-material/LocalHospital";
import { useParams } from "react-router";
import { healthCareApi, HealthEvent } from "src/api/healthCare";
import useUtils from "src/appUtils";
import LanguageSwitcher from "src/components/LanguageSwitcher";
import HealthCareChart from "./HealthCareChart";

const formatEvent = (event: HealthEvent, t: (key: string, options?: object) => string) => {
  if (event.type === "Temperature") return `${event.temperatureCelsius} C`;
  if (event.type === "Medication") return `${event.medicationName || t("healthCare:medication_fallback")} ${event.medicationAmount || ""} ${event.medicationUnit || ""}`;
  const time = new Date(event.endedAt || event.startedAt).toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" });
  return t("healthCare:sleep_until", { time });
};

const HealthCareSharePage = () => {
  const u = useUtils();
  const { token } = useParams();
  const [events, setEvents] = useState<HealthEvent[]>([]);

  useEffect(() => {
    if (!token) {
      setEvents([]);
      return;
    }

    healthCareApi.getPublicShare(token, u).then((share) => {
      setEvents(share?.events || []);
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
        <Typography variant="h4" mb={1}>{u.t("healthCare:events_chart")}</Typography>
        <HealthCareChart events={events} />
        <Stack direction="row" flexWrap="wrap" gap={1} mt={2}>
          {events.map((event) => (
            <Chip key={event.id} label={`${new Date(event.startedAt).toLocaleString([], { day: "2-digit", month: "2-digit", hour: "2-digit", minute: "2-digit" })}: ${formatEvent(event, u.t)}`} color={event.type === "Sleep" ? "info" : event.type === "Medication" ? "secondary" : "primary"} variant="outlined" />
          ))}
        </Stack>
      </CardContent></Card>
    </Box>
  );
};

export default HealthCareSharePage;
