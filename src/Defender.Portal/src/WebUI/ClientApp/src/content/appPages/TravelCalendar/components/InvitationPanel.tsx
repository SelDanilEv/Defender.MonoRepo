import { useEffect, useMemo, useState } from "react";
import { useTranslation } from "react-i18next";
import { useTheme } from "@mui/material/styles";
import {
  Box,
  Button,
  ButtonBase,
  Chip,
  IconButton,
  Stack,
  Tab,
  Tabs,
  ToggleButton,
  ToggleButtonGroup,
  Typography,
} from "@mui/material";
import PersonOutlineIcon from "@mui/icons-material/PersonOutlineOutlined";
import PendingOutlinedIcon from "@mui/icons-material/PendingOutlined";
import CheckCircleOutlineIcon from "@mui/icons-material/CheckCircleOutlined";
import BlockOutlinedIcon from "@mui/icons-material/BlockOutlined";
import CloseIcon from "@mui/icons-material/Close";

import type { TravelEvent, TravelParticipantStatus } from "src/api/travelCalendar";
import { compactIconButtonLayout } from "src/components/Buttons/buttonLayouts";
import { toLocalDate } from "../calendarMath";
import { compareByRelevance, isPast, participantSummary, receivedInvitations, sentInvitations } from "./invitationFeed";
import { participantColor } from "./participantStatus";

type ReceivedStatus = Extract<TravelParticipantStatus, "Pending" | "Accepted" | "Declined">;
type Direction = "Received" | "Sent";

const STATUS_ORDER: ReceivedStatus[] = ["Pending", "Accepted", "Declined"];

export const InvitationPanel = ({
  events,
  onOpen,
  onRespond,
  onRemoveParticipant,
  busy,
}: {
  events: TravelEvent[];
  onOpen: (eventId: string) => void;
  onRespond: (eventId: string, status: TravelParticipantStatus) => Promise<any>;
  onRemoveParticipant: (eventId: string, participantUserId: string) => Promise<any>;
  busy: boolean;
}) => {
  const { t, i18n } = useTranslation();
  const theme = useTheme();
  const locale = i18n.language === "ru" ? "ru-RU" : "en-US";
  const today = toLocalDate(new Date());

  const received = useMemo(() => receivedInvitations(events, today), [events, today]);
  const sent = useMemo(() => sentInvitations(events, today), [events, today]);
  const counts = useMemo(() => ({
    Pending: received.filter((event) => event.myParticipationStatus === "Pending").length,
    Accepted: received.filter((event) => event.myParticipationStatus === "Accepted").length,
    Declined: received.filter((event) => event.myParticipationStatus === "Declined").length,
  }), [received]);

  const [direction, setDirection] = useState<Direction>("Received");
  const [filter, setFilter] = useState<ReceivedStatus>("Pending");

  useEffect(() => {
    setDirection((current) => {
      const currentCount = current === "Received" ? received.length : sent.length;
      if (currentCount > 0) {
        return current;
      }

      return received.length > 0 ? "Received" : sent.length > 0 ? "Sent" : current;
    });
    // `direction` is deliberately absent from the deps below: once the user manually
    // picks a tab, a refetch that doesn't change which tabs are empty must not bounce
    // them back to the other one.
  }, [received.length, sent.length]);

  useEffect(() => {
    setFilter((current) => {
      if (counts[current] > 0) {
        return current;
      }

      return STATUS_ORDER.find((status) => counts[status] > 0) ?? current;
    });
    // `filter` deliberately absent, same reasoning as the direction effect above (this
    // generalizes the pre-existing 2-status version of this trick to 3 statuses).
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [counts.Pending, counts.Accepted, counts.Declined]);

  const formatShortDate = (date: string) =>
    new Intl.DateTimeFormat(locale, { day: "numeric", month: "short" }).format(new Date(`${date}T12:00:00`));
  const receivedAccentColor = (status: ReceivedStatus) =>
    status === "Accepted" ? theme.colors.success.main : status === "Declined" ? "var(--tc-muted)" : "var(--tc-accent)";
  const receivedChipColor = (status: ReceivedStatus): "warning" | "success" | "default" =>
    status === "Accepted" ? "success" : status === "Declined" ? "default" : "warning";
  const receivedStatusIcon = (status: ReceivedStatus) => {
    if (status === "Accepted") return <CheckCircleOutlineIcon fontSize="small" />;
    if (status === "Declined") return <BlockOutlinedIcon fontSize="small" />;
    return <PendingOutlinedIcon fontSize="small" />;
  };

  const visibleReceived = useMemo(
    () => received.filter((event) => event.myParticipationStatus === filter).slice().sort(compareByRelevance(today)),
    [received, filter, today],
  );
  const visibleSent = useMemo(() => sent.slice().sort(compareByRelevance(today)), [sent, today]);
  const activeCount = direction === "Received" ? visibleReceived.length : visibleSent.length;

  return (
    <Box>
      <Stack direction="row" spacing={1} sx={{ alignItems: "center", justifyContent: "space-between", mb: 1 }}>
        <Box>
          <Typography component="h3" sx={{ fontWeight: 900 }}>
            {t("travelCalendar:invitations.title")}
          </Typography>
          <Typography sx={{ fontSize: 12, color: "var(--tc-muted)" }}>
            {t(direction === "Received" ? "travelCalendar:invitations.subtitleReceived" : "travelCalendar:invitations.subtitleSent")}
          </Typography>
        </Box>
        <Chip size="small" label={activeCount} />
      </Stack>

      <Tabs
        value={direction}
        onChange={(_, value: Direction) => setDirection(value)}
        variant="fullWidth"
        textColor="primary"
        indicatorColor="primary"
        aria-label={t("travelCalendar:invitations.title")}
        sx={{ minHeight: 36, mb: 1, "& .MuiTab-root": { minHeight: 36 } }}
      >
        <Tab value="Received" label={t("travelCalendar:invitations.tabs.received")} />
        <Tab value="Sent" label={t("travelCalendar:invitations.tabs.sent")} />
      </Tabs>

      {direction === "Received" && (
        <ToggleButtonGroup
          exclusive
          fullWidth
          value={filter}
          onChange={(_, value: ReceivedStatus | null) => value && setFilter(value)}
          aria-label={t("travelCalendar:invitations.filterLabel")}
          sx={{ mb: 1.25, "& .MuiToggleButton-root": { textTransform: "none", fontSize: 11, px: 0.5 } }}
        >
          {STATUS_ORDER.map((status) => (
            <ToggleButton key={status} value={status}>{t(`travelCalendar:statuses.${status}`)}</ToggleButton>
          ))}
        </ToggleButtonGroup>
      )}

      <Stack spacing={0.75} sx={{ maxHeight: 360, overflow: "auto", pr: 0.5 }}>
        {direction === "Received" && visibleReceived.map((event) => {
          const status = event.myParticipationStatus as ReceivedStatus;
          const pastEvent = isPast(event, today);
          return (
            <Box
              key={event.id}
              sx={{
                position: "relative",
                border: "1px solid var(--tc-border)",
                borderRadius: 1.25,
                flexShrink: 0,
                opacity: status === "Declined" ? 0.68 : 1,
                overflow: "hidden",
              }}
            >
              <Box sx={{ position: "absolute", left: 0, top: 0, bottom: 0, width: 4, bgcolor: receivedAccentColor(status) }} />
              <ButtonBase
                onClick={() => onOpen(event.id)}
                aria-label={t("travelCalendar:invitations.open", { title: event.title })}
                sx={{ width: "100%", textAlign: "left", alignItems: "stretch" }}
              >
                <Stack direction="row" spacing={1} sx={{ width: "100%", p: 1.15, alignItems: "center" }}>
                  <Box sx={{ color: receivedAccentColor(status), display: "grid" }}>
                    {receivedStatusIcon(status)}
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
                  <Stack spacing={0.5} sx={{ alignItems: "flex-end" }}>
                    <Chip
                      size="small"
                      color={receivedChipColor(status)}
                      label={event.startDate ? formatShortDate(event.startDate) : t(`travelCalendar:statuses.${status}`)}
                    />
                    {pastEvent && <Chip size="small" label={t("travelCalendar:invitations.past")} />}
                  </Stack>
                </Stack>
              </ButtonBase>
              {event.canRespond && (
                <Stack direction="row" spacing={1} sx={{ px: 1.15, pb: 1.15, flexWrap: "wrap" }}>
                  <Button variant="contained" color="success" disabled={busy} onClick={() => onRespond(event.id, "Accepted")}>
                    {t("travelCalendar:drawer.accept")}
                  </Button>
                  <Button variant="outlined" color="warning" disabled={busy} onClick={() => onRespond(event.id, "Declined")}>
                    {t("travelCalendar:drawer.decline")}
                  </Button>
                </Stack>
              )}
            </Box>
          );
        })}

        {direction === "Received" && visibleReceived.length === 0 && (
          <Typography sx={{ py: 1, fontSize: 13, color: "var(--tc-muted)" }}>
            {t("travelCalendar:invitations.empty", { status: t(`travelCalendar:statuses.${filter}`) })}
          </Typography>
        )}

        {direction === "Sent" && visibleSent.map((event) => {
          const summary = participantSummary(event);
          return (
            <Box
              key={event.id}
              sx={{
                position: "relative",
                border: "1px solid var(--tc-border)",
                borderRadius: 1.25,
                flexShrink: 0,
                overflow: "hidden",
              }}
            >
              <Box sx={{ position: "absolute", left: 0, top: 0, bottom: 0, width: 4, bgcolor: summary.pending > 0 ? "var(--tc-accent)" : "var(--tc-muted)" }} />
              <ButtonBase
                onClick={() => onOpen(event.id)}
                aria-label={t("travelCalendar:invitations.openSent", { title: event.title })}
                sx={{ width: "100%", textAlign: "left", alignItems: "stretch" }}
              >
                <Stack direction="row" spacing={1} sx={{ width: "100%", p: 1.15, alignItems: "center" }}>
                  <Box sx={{ color: "var(--tc-accent)", display: "grid" }}>
                    <PersonOutlineIcon fontSize="small" />
                  </Box>
                  <Box sx={{ minWidth: 0, flex: 1 }}>
                    <Typography noWrap sx={{ fontWeight: 800 }}>{event.title}</Typography>
                  </Box>
                  <Chip
                    size="small"
                    label={event.startDate ? formatShortDate(event.startDate) : t(`travelCalendar:types.${event.type}`)}
                  />
                </Stack>
              </ButtonBase>
              {summary.pending > 0 && (
                <Box sx={{ px: 1.15, pb: 0.75 }}>
                  <Chip size="small" color="warning" label={t("travelCalendar:invitations.awaiting", { count: summary.pending })} />
                </Box>
              )}
              <Stack component="ul" spacing={0.5} sx={{ listStyle: "none", m: 0, px: 1.15, pb: 1.15 }}>
                {event.participants.map((participant) => (
                  <Stack component="li" key={participant.userId} direction="row" spacing={0.75} sx={{ alignItems: "center" }}>
                    <Chip
                      size="small"
                      label={participant.displayName}
                      color={participantColor(participant.status)}
                      aria-label={t("travelCalendar:invitations.participantStatus", {
                        name: participant.displayName,
                        status: t(`travelCalendar:statuses.${participant.status}`),
                      })}
                    />
                    <IconButton
                      aria-label={t("travelCalendar:drawer.removeParticipant")}
                      disabled={busy}
                      onClick={() => onRemoveParticipant(event.id, participant.userId)}
                      sx={compactIconButtonLayout}
                    >
                      <CloseIcon />
                    </IconButton>
                  </Stack>
                ))}
              </Stack>
            </Box>
          );
        })}

        {direction === "Sent" && visibleSent.length === 0 && (
          <Typography sx={{ py: 1, fontSize: 13, color: "var(--tc-muted)" }}>
            {t("travelCalendar:invitations.emptySent")}
          </Typography>
        )}
      </Stack>
    </Box>
  );
};
