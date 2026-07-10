import { useState } from "react";
import { useTranslation } from "react-i18next";
import { alpha, useTheme } from "@mui/material/styles";
import {
  Alert,
  Box,
  Button,
  Checkbox,
  Chip,
  Collapse,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Divider,
  IconButton,
  LinearProgress,
  Paper,
  Skeleton,
  Stack,
  TextField,
  Typography,
} from "@mui/material";
import AddIcon from "@mui/icons-material/Add";
import DeleteOutlineIcon from "@mui/icons-material/DeleteOutline";
import CalendarMonthIcon from "@mui/icons-material/CalendarMonth";

import { MonthGrid } from "./components/MonthGrid";
import { EventDrawer } from "./components/EventDrawer";
import { useTravelCalendar } from "./hooks/useTravelCalendar";

const Panel = ({ children, ...props }: any) => (
  <Paper
    elevation={0}
    {...props}
    sx={{
      p: 2.2,
      borderRadius: "20px",
      bgcolor: "var(--tc-panel)",
      border: "1px solid var(--tc-border)",
      backdropFilter: "blur(18px)",
      ...(props.sx || {}),
    }}
  >
    {children}
  </Paper>
);

export default function TravelCalendarPage() {
  const { t, i18n } = useTranslation();
  const theme = useTheme();
  const locale = i18n.language === "ru" ? "ru-RU" : "en-US";
  const formatMoney = (value: number) => value.toLocaleString(locale);
  const state = useTravelCalendar();
  const { calendar } = state;
  const [tripDialog, setTripDialog] = useState(false);
  const [tripTitle, setTripTitle] = useState("");
  const [packing, setPacking] = useState("");
  const [budgetOpen, setBudgetOpen] = useState(false);

  if (state.loading) {
    return (
      <Box p={4}>
        <Skeleton height={130} sx={{ borderRadius: 5 }} />
        <Box
          display="grid"
          gridTemplateColumns={{ md: "360px 1fr" }}
          gap={2}
          mt={2}
        >
          <Skeleton height={500} />
          <Skeleton height={500} />
        </Box>
      </Box>
    );
  }

  if (!calendar) {
    return (
      <Box p={4}>
        <Alert
          severity="error"
          action={<Button onClick={state.retry}>{t("travelCalendar:retry")}</Button>}
        >
          {state.error || t("travelCalendar:unavailable")}
        </Alert>
      </Box>
    );
  }

  const dark = theme.palette.mode === "dark";
  const queued = calendar.events.filter(
    (item) => item.isMustVisit && !item.startDate && item.canEdit
  );
  const scheduled = calendar.events.filter((item) => item.startDate);
  const pendingInvites = calendar.events.filter(
    (item) => item.myParticipationStatus === "Pending"
  ).length;
  const eventTypes = [
    {
      type: "OvernightTrip",
      color: theme.colors.success.main,
      icon: "🚗",
      title: t("travelCalendar:legend.OvernightTrip.title"),
      note: t("travelCalendar:legend.OvernightTrip.note"),
    },
    {
      type: "DayTrip",
      color: theme.colors.warning.main,
      icon: "☀️",
      title: t("travelCalendar:legend.DayTrip.title"),
      note: t("travelCalendar:legend.DayTrip.note"),
    },
    {
      type: "Event",
      color: theme.colors.primary.main,
      icon: "🎉",
      title: t("travelCalendar:legend.Event.title"),
      note: t("travelCalendar:legend.Event.note"),
    },
    {
      type: "Rest",
      color: theme.colors.info.main,
      icon: "🌿",
      title: t("travelCalendar:legend.Rest.title"),
      note: t("travelCalendar:legend.Rest.note"),
    },
    {
      type: "Family",
      color: "#ef719c",
      icon: "💗",
      title: t("travelCalendar:legend.Family.title"),
      note: t("travelCalendar:legend.Family.note"),
    },
  ];
  const variables: any = dark
    ? {
        "--tc-bg": theme.palette.background.default,
        "--tc-panel": alpha(theme.palette.background.paper, 0.78),
        "--tc-drawer": alpha(theme.palette.background.paper, 0.92),
        "--tc-text": theme.palette.text.primary,
        "--tc-muted": theme.palette.text.secondary,
        "--tc-border": alpha(theme.palette.common.white, 0.1),
        "--tc-accent": theme.colors.primary.main,
        "--tc-accent-soft": alpha(theme.colors.primary.main, 0.16),
        "--tc-weekend": alpha(theme.palette.common.white, 0.035),
      }
    : {
        "--tc-bg": theme.palette.background.default,
        "--tc-panel": alpha(theme.palette.background.paper, 0.92),
        "--tc-drawer": theme.palette.background.paper,
        "--tc-text": theme.palette.text.primary,
        "--tc-muted": theme.palette.text.secondary,
        "--tc-border": alpha(theme.palette.text.primary, 0.1),
        "--tc-accent": theme.colors.primary.main,
        "--tc-accent-soft": alpha(theme.colors.primary.main, 0.12),
        "--tc-weekend": alpha(theme.colors.warning.main, 0.08),
      };

  return (
    <Box
      data-calendar-theme={dark ? "dark" : "light"}
      sx={{
        ...variables,
        minHeight: "100%",
        bgcolor: "var(--tc-bg)",
        color: "var(--tc-text)",
        p: { xs: 2, sm: 3, lg: 4 },
        position: "relative",
        overflow: "hidden",
        transition: ".3s",
        "&:before": {
          content: '""',
          position: "absolute",
          width: 500,
          height: 500,
          borderRadius: "50%",
          background: dark
            ? alpha(theme.colors.primary.main, 0.18)
            : alpha(theme.colors.primary.main, 0.14),
          filter: "blur(90px)",
          top: -260,
          right: -120,
        },
        "& .MuiTypography-root": { color: "var(--tc-text)" },
        "& .MuiButton-root": { color: "var(--tc-accent)" },
        "& .MuiButton-contained": {
          bgcolor: "var(--tc-accent)",
          color: "#fff",
          "&:hover": { bgcolor: theme.colors.primary.dark },
        },
        "& .MuiInputBase-root,& .MuiInputLabel-root,& .MuiIconButton-root,& .MuiCheckbox-root":
          {
            color: "var(--tc-text)",
          },
        "& .MuiOutlinedInput-notchedOutline": {
          borderColor: "var(--tc-border)",
        },
      }}
    >
      <Box position="relative" zIndex={1}>
        <Panel sx={{ p: { xs: 2, md: 3 }, mb: 2.5, borderRadius: "24px" }}>
          <Stack
            direction={{ xs: "column", md: "row" }}
            alignItems={{ md: "center" }}
            gap={2}
          >
            <Stack direction="row" alignItems="center" spacing={1.5} flex={1}>
              <Box
                sx={{
                  width: 54,
                  height: 54,
                  borderRadius: 3,
                  display: "grid",
                  placeItems: "center",
                  bgcolor: "var(--tc-accent)",
                  color: "white",
                  boxShadow: `0 12px 30px ${alpha(theme.colors.primary.main, 0.28)}`,
                }}
              >
                <CalendarMonthIcon fontSize="large" />
              </Box>
              <Box>
                <Typography component="h1" variant="h4" fontWeight={900}>
                  {t("travelCalendar:title")}
                </Typography>
                <Typography color="var(--tc-muted)">
                  {t("travelCalendar:subtitle", { city: calendar.baseCity })}
                </Typography>
              </Box>
            </Stack>
            <Stack direction="row" spacing={1} flexWrap="wrap">
              <Chip
                label={t("travelCalendar:hero.overnightTrips", {
                  count: calendar.summary.overnightTripCount,
                })}
                sx={{ bgcolor: "var(--tc-accent-soft)", color: "var(--tc-text)" }}
              />
              <Chip
                label={t("travelCalendar:hero.queuedTrips", {
                  count: queued.length,
                })}
                sx={{ bgcolor: "var(--tc-accent-soft)", color: "var(--tc-text)" }}
              />
              <Chip
                label={t("travelCalendar:hero.pendingInvites", {
                  count: pendingInvites,
                })}
                sx={{
                  bgcolor: alpha(theme.colors.warning.main, 0.18),
                  color: "var(--tc-text)",
                }}
              />
            </Stack>
          </Stack>
        </Panel>

        {state.error && (
          <Alert severity="warning" sx={{ mb: 2 }} onClose={() => undefined}>
            {state.error}
          </Alert>
        )}
        {state.mutating && <LinearProgress sx={{ mb: 1, borderRadius: 2 }} />}

        <Box
          sx={{
            display: "grid",
            gridTemplateColumns: { xs: "1fr", lg: "360px minmax(0,1fr)" },
            gap: 2.5,
            alignItems: "start",
          }}
        >
          <Stack spacing={2}>
            <Panel>
              <Stack
                direction="row"
                alignItems="center"
                justifyContent="space-between"
              >
                <Box>
                  <Typography component="h3" fontWeight={900}>
                    {t("travelCalendar:wishlist.title")}
                  </Typography>
                  <Typography fontSize={12} color="var(--tc-muted)">
                    {t("travelCalendar:wishlist.subtitle")}
                  </Typography>
                </Box>
                <Chip size="small" label={queued.length} />
              </Stack>
              <Stack
                spacing={1}
                mt={1.5}
                sx={{ maxHeight: 310, overflow: "auto", pr: 0.5 }}
              >
                {queued.map((item, index) => (
                  <Box
                    key={item.id}
                    sx={{
                      p: 1.2,
                      border: "1px solid var(--tc-border)",
                      borderRadius: 2.5,
                      bgcolor: "var(--tc-accent-soft)",
                    }}
                  >
                    <Stack direction="row" alignItems="center">
                      <Box flex={1}>
                        <Typography fontWeight={800}>{item.title}</Typography>
                        <Typography fontSize={11} color="var(--tc-muted)">
                          {t("travelCalendar:wishlist.queuePosition", {
                            position: index + 1,
                          })}
                        </Typography>
                      </Box>
                      <Button
                        size="small"
                        disabled={state.mutating}
                        onClick={() => state.autoSchedule(item.id)}
                      >
                        {t("travelCalendar:wishlist.schedule")}
                      </Button>
                    </Stack>
                  </Box>
                ))}
              </Stack>
              <Button
                fullWidth
                startIcon={<AddIcon />}
                sx={{ mt: 1.5 }}
                onClick={() => setTripDialog(true)}
              >
                {t("travelCalendar:wishlist.addPlace")}
              </Button>
            </Panel>

            <Panel>
              <Stack direction="row" justifyContent="space-between">
                <Typography component="h3" fontWeight={900}>
                  {t("travelCalendar:budget.title")}
                </Typography>
                <Typography fontWeight={900} color="var(--tc-accent)">
                  {formatMoney(calendar.summary.grandTotalPln)} PLN
                </Typography>
              </Stack>
              <Box display="flex" height={8} borderRadius={5} overflow="hidden" my={1.5}>
                <Box
                  bgcolor={theme.colors.primary.main}
                  width={`${
                    calendar.summary.grandTotalPln
                      ? (calendar.summary.hotelTotalPln /
                          calendar.summary.grandTotalPln) *
                        100
                      : 0
                  }%`}
                />
                <Box
                  bgcolor={theme.colors.warning.main}
                  width={`${
                    calendar.summary.grandTotalPln
                      ? (calendar.summary.transportTotalPln /
                          calendar.summary.grandTotalPln) *
                        100
                      : 0
                  }%`}
                />
                <Box bgcolor={theme.colors.success.main} flex={1} />
              </Box>
              <Stack spacing={0.5}>
                <Stack direction="row" justifyContent="space-between">
                  <Typography color="var(--tc-muted)">
                    {t("travelCalendar:budget.hotels")}
                  </Typography>
                  <b>{formatMoney(calendar.summary.hotelTotalPln)} PLN</b>
                </Stack>
                <Stack direction="row" justifyContent="space-between">
                  <Typography color="var(--tc-muted)">
                    {t("travelCalendar:budget.transport")}
                  </Typography>
                  <b>{formatMoney(calendar.summary.transportTotalPln)} PLN</b>
                </Stack>
                <Stack direction="row" justifyContent="space-between">
                  <Typography color="var(--tc-muted)">
                    {t("travelCalendar:budget.other")}
                  </Typography>
                  <b>{formatMoney(calendar.summary.otherTotalPln)} PLN</b>
                </Stack>
              </Stack>
              <Button
                size="small"
                onClick={() => setBudgetOpen(!budgetOpen)}
                sx={{ mt: 1 }}
              >
                {budgetOpen
                  ? t("travelCalendar:budget.hideDetails")
                  : t("travelCalendar:budget.showDetails")}
              </Button>
              <Collapse in={budgetOpen}>
                <Divider sx={{ my: 1, borderColor: "var(--tc-border)" }} />
                {calendar.summary.details.map((item) => (
                  <Stack
                    key={item.eventId}
                    direction="row"
                    justifyContent="space-between"
                    py={0.5}
                  >
                    <Typography fontSize={12} noWrap maxWidth="70%">
                      {item.date} · {item.title}
                    </Typography>
                    <Typography fontSize={12} fontWeight={800}>
                      {formatMoney(item.totalPln)} PLN
                    </Typography>
                  </Stack>
                ))}
              </Collapse>
            </Panel>

            <Panel>
              <Typography component="h3" fontWeight={900}>
                {t("travelCalendar:packing.title")}
              </Typography>
              <Stack spacing={0.4} my={1}>
                {calendar.packingItems.map((item) => (
                  <Stack key={item.id} direction="row" alignItems="center">
                    <Checkbox
                      checked={item.isChecked}
                      onChange={(e) =>
                        state.updatePacking(item.id, {
                          isChecked: e.target.checked,
                        })
                      }
                    />
                    <Typography
                      flex={1}
                      sx={{
                        textDecoration: item.isChecked ? "line-through" : "none",
                        opacity: item.isChecked ? 0.55 : 1,
                      }}
                    >
                      {item.text}
                    </Typography>
                    <IconButton
                      size="small"
                      aria-label={t("travelCalendar:packing.removeItem")}
                      onClick={() => state.removePacking(item.id)}
                    >
                      <DeleteOutlineIcon fontSize="small" />
                    </IconButton>
                  </Stack>
                ))}
              </Stack>
              <Stack direction="row">
                <TextField
                  size="small"
                  fullWidth
                  placeholder={t("travelCalendar:packing.placeholder")}
                  value={packing}
                  onChange={(e) => setPacking(e.target.value)}
                  onKeyDown={(e) => {
                    if (e.key === "Enter" && packing.trim()) {
                      state.addPacking(packing);
                      setPacking("");
                    }
                  }}
                />
                <IconButton
                  aria-label={t("travelCalendar:packing.addItem")}
                  onClick={() => {
                    if (packing.trim()) {
                      state.addPacking(packing);
                      setPacking("");
                    }
                  }}
                >
                  <AddIcon />
                </IconButton>
              </Stack>
            </Panel>
          </Stack>

          <Stack spacing={2}>
            <Panel>
              <Stack
                direction={{ xs: "column", sm: "row" }}
                alignItems={{ sm: "center" }}
                gap={1}
              >
                <Typography component="h3" fontWeight={900} mr="auto">
                  {t("travelCalendar:legend.title")}
                </Typography>
                {eventTypes.map((type) => (
                  <Stack
                    key={type.type}
                    direction="row"
                    alignItems="center"
                    spacing={0.7}
                  >
                    <Box
                      sx={{
                        width: 9,
                        height: 9,
                        borderRadius: "50%",
                        bgcolor: type.color,
                      }}
                    />
                    <Box>
                      <Typography fontSize={11} fontWeight={800}>
                        {type.icon} {type.title}
                      </Typography>
                      <Typography fontSize={9} color="var(--tc-muted)">
                        {type.note}
                      </Typography>
                    </Box>
                  </Stack>
                ))}
              </Stack>
            </Panel>
            <Box
              sx={{
                display: "grid",
                gridTemplateColumns:
                  "repeat(auto-fit,minmax(min(340px,100%),1fr))",
                gap: 2,
              }}
            >
              {[6, 7, 8].map((month) => (
                <MonthGrid
                  key={month}
                  year={2026}
                  month={month}
                  events={scheduled}
                  holidays={calendar.holidays}
                  onDate={state.createFromDate}
                  onEvent={state.setActiveEventId}
                />
              ))}
            </Box>
            <Typography textAlign="center" color="var(--tc-muted)" fontSize={12}>
              {t("travelCalendar:hint")}
            </Typography>
          </Stack>
        </Box>
      </Box>

      <Dialog open={tripDialog} onClose={() => setTripDialog(false)}>
        <DialogTitle>{t("travelCalendar:addPlaceDialog.title")}</DialogTitle>
        <DialogContent>
          <TextField
            autoFocus
            fullWidth
            label={t("travelCalendar:addPlaceDialog.label")}
            value={tripTitle}
            onChange={(e) => setTripTitle(e.target.value)}
            sx={{ mt: 1 }}
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setTripDialog(false)}>
            {t("travelCalendar:addPlaceDialog.cancel")}
          </Button>
          <Button
            variant="contained"
            disabled={!tripTitle.trim()}
            onClick={async () => {
              const result = await state.addTrip(tripTitle);
              if (result) {
                setTripDialog(false);
                setTripTitle("");
              }
            }}
          >
            {t("travelCalendar:addPlaceDialog.add")}
          </Button>
        </DialogActions>
      </Dialog>

      <EventDrawer
        event={state.activeEvent}
        calendar={calendar}
        open={!!state.activeEvent}
        busy={state.mutating}
        onClose={() => state.setActiveEventId(null)}
        onSave={(request) => state.saveEvent(state.activeEvent!.id, request)}
        onRemove={() => state.removeEvent(state.activeEvent!.id)}
        onAddPoint={(text) => state.addPoint(state.activeEvent!.id, text)}
        onUpdatePoint={(id, patch) =>
          state.updatePoint(state.activeEvent!.id, id, patch)
        }
        onRemovePoint={(id) => state.removePoint(state.activeEvent!.id, id)}
        onSearchUsers={state.searchUsers}
        onAddParticipant={(userId, displayName, avatarUrl) =>
          state.addParticipant(
            state.activeEvent!.id,
            userId,
            displayName,
            avatarUrl
          )
        }
        onRemoveParticipant={(participantUserId) =>
          state.removeParticipant(state.activeEvent!.id, participantUserId)
        }
        onRespond={(status) =>
          state.updateMyParticipation(state.activeEvent!.id, status)
        }
      />
    </Box>
  );
}
