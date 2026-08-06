import { useEffect, useMemo, useState } from "react";
import { useTranslation } from "react-i18next";
import {
  Box,
  ButtonBase,
  Chip,
  Stack,
  ToggleButton,
  ToggleButtonGroup,
  Typography,
} from "@mui/material";
import PersonOutlineIcon from "@mui/icons-material/PersonOutlineOutlined";
import PendingOutlinedIcon from "@mui/icons-material/PendingOutlined";
import BlockOutlinedIcon from "@mui/icons-material/BlockOutlined";

import type { TravelEvent, TravelParticipantStatus } from "src/api/travelCalendar";

type InvitationFilter = Extract<TravelParticipantStatus, "Pending" | "Declined">;

const statusIcon = (status: InvitationFilter) => status === "Pending"
  ? <PendingOutlinedIcon fontSize="small" />
  : <BlockOutlinedIcon fontSize="small" />;

export const InvitationPanel = ({ events, onOpen }: { events: TravelEvent[]; onOpen: (eventId: string) => void }) => {
  const { t, i18n } = useTranslation();
  const invitations = useMemo(() => events.filter((event) =>
    !event.canEdit && (event.myParticipationStatus === "Pending" || event.myParticipationStatus === "Declined"),
  ), [events]);
  const hasPending = invitations.some((event) => event.myParticipationStatus === "Pending");
  const hasDeclined = invitations.some((event) => event.myParticipationStatus === "Declined");
  const [filter, setFilter] = useState<InvitationFilter>("Pending");
  const filtered = invitations.filter((event) => event.myParticipationStatus === filter);
  const locale = i18n.language === "ru" ? "ru-RU" : "en-US";

  useEffect(() => {
    setFilter((current) => {
      if (current === "Pending" ? hasPending : hasDeclined) return current;
      return hasDeclined && !hasPending ? "Declined" : "Pending";
    });
  }, [hasPending, hasDeclined]);

  return (
    <Box>
      <Stack direction="row" spacing={1} sx={{ alignItems: "center", justifyContent: "space-between", mb: 1 }}>
        <Box>
          <Typography component="h3" sx={{ fontWeight: 900 }}>
            {t("travelCalendar:invitations.title")}
          </Typography>
          <Typography sx={{ fontSize: 12, color: "var(--tc-muted)" }}>
            {t("travelCalendar:invitations.subtitle")}
          </Typography>
        </Box>
        <Chip size="small" label={invitations.length} />
      </Stack>
      <ToggleButtonGroup
        exclusive
        fullWidth
        size="small"
        value={filter}
        onChange={(_, value: InvitationFilter | null) => value && setFilter(value)}
        aria-label={t("travelCalendar:invitations.filterLabel")}
        sx={{ mb: 1.25 }}
      >
        <ToggleButton value="Pending">{t("travelCalendar:statuses.Pending")}</ToggleButton>
        <ToggleButton value="Declined">{t("travelCalendar:statuses.Declined")}</ToggleButton>
      </ToggleButtonGroup>
      <Stack spacing={0.75}>
        {filtered.map((event) => {
          const status = event.myParticipationStatus as InvitationFilter;
          return (
            <ButtonBase
              key={event.id}
              onClick={() => onOpen(event.id)}
              aria-label={t("travelCalendar:invitations.open", { title: event.title })}
              sx={{
                width: "100%",
                textAlign: "left",
                alignItems: "stretch",
                border: "1px solid var(--tc-border)",
                borderLeft: `4px solid ${status === "Pending" ? "var(--tc-accent)" : "var(--tc-muted)"}`,
                borderRadius: 2.5,
                opacity: status === "Declined" ? 0.68 : 1,
                overflow: "hidden",
              }}
            >
              <Stack direction="row" spacing={1} sx={{ width: "100%", p: 1.15, alignItems: "center" }}>
                <Box sx={{ color: status === "Pending" ? "var(--tc-accent)" : "var(--tc-muted)", display: "grid" }}>
                  {statusIcon(status)}
                </Box>
                <Box sx={{ minWidth: 0, flex: 1 }}>
                  <Typography noWrap sx={{ fontWeight: 800 }}>{event.title}</Typography>
                  <Stack direction="row" spacing={0.5} sx={{ alignItems: "center", color: "var(--tc-muted)" }}>
                    <PersonOutlineIcon sx={{ fontSize: 14 }} />
                    <Typography noWrap sx={{ fontSize: 12 }}>
                      {event.ownerDisplayName || t("travelCalendar:drawer.organizerFallback")}
                    </Typography>
                  </Stack>
                </Box>
                <Chip
                  size="small"
                  color={status === "Pending" ? "warning" : "default"}
                  label={event.startDate ? new Intl.DateTimeFormat(locale, { day: "numeric", month: "short" }).format(new Date(`${event.startDate}T12:00:00`)) : t(`travelCalendar:statuses.${status}`)}
                />
              </Stack>
            </ButtonBase>
          );
        })}
        {filtered.length === 0 && (
          <Typography sx={{ py: 1, fontSize: 13, color: "var(--tc-muted)" }}>
            {t("travelCalendar:invitations.empty", { status: t(`travelCalendar:statuses.${filter}`) })}
          </Typography>
        )}
      </Stack>
    </Box>
  );
};
