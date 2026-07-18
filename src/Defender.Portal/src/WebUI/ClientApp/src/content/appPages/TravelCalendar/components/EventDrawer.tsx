import { useEffect, useMemo, useRef, useState } from "react";
import {
  Autocomplete,
  Box,
  Button,
  Checkbox,
  Chip,
  Divider,
  Drawer,
  IconButton,
  Stack,
  TextField,
  ToggleButton,
  ToggleButtonGroup,
  Typography,
  useTheme,
} from "@mui/material";
import CloseIcon from "@mui/icons-material/Close";
import DeleteOutlineIcon from "@mui/icons-material/DeleteOutlined";
import AddIcon from "@mui/icons-material/Add";
import PersonAddAlt1Icon from "@mui/icons-material/PersonAddAlt1";
import { useTranslation } from "react-i18next";
import { TravelCalendar, TravelCalendarUserOption, TravelEvent, TravelParticipantStatus, TravelEventType, UpdateEventRequest } from "src/api/travelCalendar";
import { liveTotal } from "../budgetMath";
import { getEventDrawerPaperSx } from "./eventDrawerStyles";

const participantColor = (status?: TravelParticipantStatus) => {
  switch (status) {
    case "Accepted":
      return "success";
    case "Declined":
      return "default";
    default:
      return "warning";
  }
};

export const EventDrawer = ({
  event,
  calendar,
  open,
  busy,
  onClose,
  onSave,
  onRemove,
  onAddPoint,
  onUpdatePoint,
  onRemovePoint,
  onSearchUsers,
  onAddParticipant,
  onRemoveParticipant,
  onRespond,
}: {
  event: TravelEvent | null;
  calendar: TravelCalendar;
  open: boolean;
  busy: boolean;
  onClose: () => void;
  onSave: (request: UpdateEventRequest) => Promise<any>;
  onRemove: () => void;
  onAddPoint: (text: string) => void;
  onUpdatePoint: (id: string, patch: { text?: string; isChecked?: boolean }) => void;
  onRemovePoint: (id: string) => void;
  onSearchUsers: (query: string) => Promise<TravelCalendarUserOption[]>;
  onAddParticipant: (userId: string, displayName: string, avatarUrl?: string) => Promise<any>;
  onRemoveParticipant: (participantUserId: string) => Promise<any>;
  onRespond: (status: TravelParticipantStatus) => Promise<any>;
}) => {
  const { t, i18n } = useTranslation();
  const theme = useTheme();
  const locale = i18n.language === "ru" ? "ru-RU" : "en-US";
  const formatMoney = (value: number) => value.toLocaleString(locale);
  const types: { value: TravelEventType; label: string; icon: string }[] = [
    { value: "OvernightTrip", label: t("travelCalendar:types.OvernightTrip"), icon: "🚗" },
    { value: "DayTrip", label: t("travelCalendar:types.DayTrip"), icon: "☀️" },
    { value: "Event", label: t("travelCalendar:types.Event"), icon: "🎉" },
    { value: "Rest", label: t("travelCalendar:types.Rest"), icon: "🌿" },
    { value: "Family", label: t("travelCalendar:types.Family"), icon: "💗" },
  ];

  const [draft, setDraft] = useState<any>(null);
  const [point, setPoint] = useState("");
  const [userQuery, setUserQuery] = useState("");
  const [userOptions, setUserOptions] = useState<TravelCalendarUserOption[]>([]);
  const [selectedUser, setSelectedUser] = useState<TravelCalendarUserOption | null>(null);
  const draftEventId = useRef<string | null>(null);

  useEffect(() => {
    if (!event) {
      draftEventId.current = null;
      setDraft(null);
      return;
    }

    if (draftEventId.current === event.id) {
      return;
    }

    draftEventId.current = event.id;

    setDraft({
      title: event.title,
      type: event.type,
      startDate: event.startDate || "",
      endDate: event.endDate || "",
      notes: event.notes || "",
      hotelBooked: event.hotel?.isBooked || false,
      hotelName: event.hotel?.name || "",
      hotelAddress: event.hotel?.address || "",
      hotelBookingUrl: event.hotel?.bookingUrl || "",
      hotelCostPln: event.hotel?.costPln || 0,
      distanceKm: event.distanceKm || 0,
      mainPoint: event.mainPoint || "",
      otherCostPln: event.otherCostPln || 0,
    });
  }, [event]);

  useEffect(() => {
    let active = true;
    if (!event?.canEdit || !userQuery.trim()) {
      setUserOptions([]);
      return () => {
        active = false;
      };
    }

    onSearchUsers(userQuery).then((items) => {
      if (active) {
        const used = new Set([event.ownerUserId, ...event.participants.map((item) => item.userId)]);
        setUserOptions(items.filter((item) => !used.has(item.userId)));
      }
    });

    return () => {
      active = false;
    };
  }, [event, onSearchUsers, userQuery]);

  const isTrip = draft?.type === "OvernightTrip" || draft?.type === "DayTrip";
  const overnight = draft?.type === "OvernightTrip";
  const total = useMemo(() => draft
    ? liveTotal(draft.type, Number(draft.hotelCostPln), Number(draft.distanceKm), Number(draft.otherCostPln), calendar.vehicle.fuelConsumptionLitersPer100Km, calendar.vehicle.fuelPricePlnPerLiter)
    : 0, [draft, calendar]);
  const set = (name: string, value: any) => setDraft((current: any) => ({ ...current, [name]: value }));

  if (!draft || !event) {
    return null;
  }

  return (
    <Drawer anchor="right" open={open} onClose={onClose} slotProps={{
      paper: { sx: getEventDrawerPaperSx(theme) }
    }}>
      <Stack
        direction="row"
        sx={{
          justifyContent: "space-between",
          alignItems: "flex-start"
        }}>
        <Box>
          <Typography component="h2" variant="h5" sx={{
            fontWeight: 900
          }}>{t("travelCalendar:drawer.title")}</Typography>
          <Typography
            sx={{
              color: "var(--tc-muted)",
              fontSize: 13
            }}>
            {event.canEdit ? t("travelCalendar:drawer.ownerMode") : t("travelCalendar:drawer.sharedMode")}
          </Typography>
        </Box>
        <IconButton aria-label={t("travelCalendar:drawer.close")} onClick={onClose} sx={{ color: "inherit" }}><CloseIcon /></IconButton>
      </Stack>
      <Divider sx={{ my: 2, borderColor: "var(--tc-border)" }} />
      <Stack spacing={2}>
        <Box>
          <Stack direction="row" spacing={1} sx={{
            flexWrap: "wrap"
          }}>
            <Chip size="small" label={event.canEdit ? t("travelCalendar:drawer.organizer") : t("travelCalendar:drawer.organizerNamed", { name: event.ownerDisplayName || t("travelCalendar:drawer.organizerFallback") })} />
            {!event.canEdit && event.myParticipationStatus && <Chip size="small" color={participantColor(event.myParticipationStatus)} label={t("travelCalendar:drawer.statusNamed", { status: t(`travelCalendar:statuses.${event.myParticipationStatus}`) })} />}
          </Stack>
        </Box>

        <ToggleButtonGroup exclusive fullWidth value={draft.type} onChange={(_, value) => value && event.canEdit && set("type", value)} sx={{ display: "grid", gridTemplateColumns: "repeat(5,1fr)", gap: .5 }}>
          {types.map((type) => <ToggleButton key={type.value} value={type.value} disabled={!event.canEdit} sx={{ px: .5, py: 1, border: "1px solid var(--tc-border)!important", borderRadius: "9px!important", color: "inherit", display: "flex", flexDirection: "column", fontSize: 9, "&.Mui-selected": { bgcolor: "var(--tc-accent-soft)", color: "var(--tc-accent)" } }}><span style={{ fontSize: 17 }}>{type.icon}</span>{type.label}</ToggleButton>)}
        </ToggleButtonGroup>

        <TextField label={t("travelCalendar:drawer.fields.title")} value={draft.title} disabled={!event.canEdit} onChange={(e) => set("title", e.target.value)} fullWidth />
        <Stack direction={{ xs: "column", sm: "row" }} spacing={1.5}>
          <TextField label={t("travelCalendar:drawer.fields.startDate")} type="date" value={draft.startDate} disabled={!event.canEdit} onChange={(e) => set("startDate", e.target.value)} fullWidth slotProps={{
            inputLabel: { shrink: true }
          }} />
          <TextField label={t("travelCalendar:drawer.fields.endDate")} type="date" value={draft.endDate} disabled={!event.canEdit} onChange={(e) => set("endDate", e.target.value)} fullWidth slotProps={{
            inputLabel: { shrink: true }
          }} />
        </Stack>
        <TextField label={t("travelCalendar:drawer.fields.notes")} value={draft.notes} disabled={!event.canEdit} onChange={(e) => set("notes", e.target.value)} multiline minRows={2} />

        <Box sx={{ p: 2, border: "1px solid var(--tc-border)", borderRadius: 3 }}>
          <Stack
            direction="row"
            spacing={1}
            sx={{
              alignItems: "center",
              mb: 1
            }}>
            <PersonAddAlt1Icon fontSize="small" />
            <Typography sx={{
              fontWeight: 800
            }}>{t("travelCalendar:drawer.participants")}</Typography>
          </Stack>
          <Stack spacing={1}>
            {event.participants.map((participant) => <Stack key={participant.userId} direction="row" spacing={1} sx={{
              alignItems: "center"
            }}>
              <Chip size="small" label={participant.displayName} color={participantColor(participant.status)} />
              <Typography
                sx={{
                  fontSize: 12,
                  color: "var(--tc-muted)"
                }}>{t(`travelCalendar:statuses.${participant.status}`)}</Typography>
              {event.canEdit && <Button size="small" color="inherit" onClick={() => onRemoveParticipant(participant.userId)}>{t("travelCalendar:drawer.removeParticipant")}</Button>}
            </Stack>)}
          </Stack>
          {event.canEdit && <Autocomplete
            sx={{ mt: 1.5 }}
            value={selectedUser}
            onChange={(_, value) => setSelectedUser(value)}
            inputValue={userQuery}
            onInputChange={(_, value) => setUserQuery(value)}
            options={userOptions}
            noOptionsText={t("travelCalendar:drawer.noUsers")}
            getOptionLabel={(option) => `${option.displayName}${option.email ? ` (${option.email})` : ""}`}
            renderInput={(params) => <TextField {...params} label={t("travelCalendar:drawer.addUser")} placeholder={t("travelCalendar:drawer.searchUser")} />}
          />}
          {event.canEdit && <Button
            sx={{ mt: 1 }}
            startIcon={<AddIcon />}
            disabled={!selectedUser || busy}
            onClick={async () => {
              if (!selectedUser) {
                return;
              }

              const result = await onAddParticipant(selectedUser.userId, selectedUser.displayName, selectedUser.avatarUrl);
              if (result) {
                setSelectedUser(null);
                setUserQuery("");
              }
            }}
          >
            {t("travelCalendar:drawer.invite")}
          </Button>}
        </Box>

        {overnight && <Box sx={{ p: 2, border: "1px solid var(--tc-border)", borderRadius: 3 }}>
          <Typography sx={{
            fontWeight: 800
          }}>🏨 {t("travelCalendar:drawer.hotel")}</Typography>
          <Stack spacing={1.5} sx={{
            mt: 1
          }}>
            <TextField label={t("travelCalendar:drawer.fields.hotelName")} value={draft.hotelName} disabled={!event.canEdit} onChange={(e) => set("hotelName", e.target.value)} />
            <TextField label={t("travelCalendar:drawer.fields.hotelAddress")} value={draft.hotelAddress} disabled={!event.canEdit} onChange={(e) => set("hotelAddress", e.target.value)} />
            <TextField label={t("travelCalendar:drawer.fields.hotelBookingUrl")} value={draft.hotelBookingUrl} disabled={!event.canEdit} onChange={(e) => set("hotelBookingUrl", e.target.value)} />
          </Stack>
        </Box>}

        {isTrip && <Box sx={{ p: 2, border: "1px solid var(--tc-border)", borderRadius: 3 }}>
          <Typography
            sx={{
              fontWeight: 800,
              mb: 1
            }}>📍 {t("travelCalendar:drawer.points")}</Typography>
          <TextField fullWidth label={t("travelCalendar:drawer.fields.mainPoint")} value={draft.mainPoint} disabled={!event.canEdit} onChange={(e) => set("mainPoint", e.target.value)} />
          {event.points.map((item) => <Stack key={item.id} direction="row" sx={{
            alignItems: "center"
          }}>
            <Checkbox checked={item.isChecked} disabled={!event.canEdit} onChange={(e) => onUpdatePoint(item.id, { isChecked: e.target.checked })} />
            <Typography sx={{ flex: 1, textDecoration: item.isChecked ? "line-through" : "none" }}>{item.text}</Typography>
            {event.canEdit && <IconButton size="small" aria-label={t("travelCalendar:drawer.removePoint")} onClick={() => onRemovePoint(item.id)}><CloseIcon fontSize="small" /></IconButton>}
          </Stack>)}
          {event.canEdit && <Stack direction="row" sx={{
            mt: 1
          }}>
            <TextField size="small" fullWidth placeholder={t("travelCalendar:drawer.addPointPlaceholder")} value={point} onChange={(e) => setPoint(e.target.value)} onKeyDown={(e) => { if (e.key === "Enter" && point.trim()) { onAddPoint(point); setPoint(""); } }} />
            <IconButton aria-label={t("travelCalendar:drawer.addPoint")} onClick={() => { if (point.trim()) { onAddPoint(point); setPoint(""); } }}><AddIcon /></IconButton>
          </Stack>}
        </Box>}

        <Box sx={{ p: 2, borderRadius: 3, bgcolor: "var(--tc-accent-soft)" }}>
          <Typography
            sx={{
              fontWeight: 900,
              mb: 1
            }}>{t("travelCalendar:drawer.eventBudget")}</Typography>
          <Stack spacing={1.5}>
            {overnight && <TextField label={t("travelCalendar:drawer.fields.hotelCost")} type="number" value={draft.hotelCostPln} disabled={!event.canEdit} onChange={(e) => set("hotelCostPln", Number(e.target.value))} />}
            {isTrip && <TextField label={t("travelCalendar:drawer.fields.distanceKm")} type="number" value={draft.distanceKm} disabled={!event.canEdit} onChange={(e) => set("distanceKm", Number(e.target.value))} />}
            <TextField label={t(isTrip ? "travelCalendar:drawer.fields.otherCost" : "travelCalendar:drawer.fields.eventCost")} type="number" value={draft.otherCostPln} disabled={!event.canEdit} onChange={(e) => set("otherCostPln", Number(e.target.value))} />
          </Stack>
          <Stack
            direction="row"
            sx={{
              justifyContent: "space-between",
              mt: 2
            }}><Typography sx={{
            fontWeight: 800
          }}>{t("travelCalendar:drawer.total")}</Typography><Typography
            sx={{
              fontWeight: 900,
              color: "var(--tc-accent)"
            }}>{formatMoney(total)} PLN</Typography></Stack>
        </Box>

        <Stack direction="row" spacing={1} sx={{
          flexWrap: "wrap"
        }}>
          {event.canEdit && <Button color="error" variant="outlined" startIcon={<DeleteOutlineIcon />} onClick={onRemove}>{t("travelCalendar:drawer.delete")}</Button>}
          {!event.canEdit && event.canRespond && <>
            <Button variant="contained" color="success" onClick={() => onRespond("Accepted")}>{t("travelCalendar:drawer.accept")}</Button>
            <Button variant="outlined" color="warning" onClick={() => onRespond("Declined")}>{t("travelCalendar:drawer.decline")}</Button>
          </>}
          <Box sx={{
            flex: 1
          }} />
          <Button onClick={onClose}>{t("travelCalendar:drawer.close")}</Button>
          {event.canEdit && <Button disabled={busy || !draft.title.trim() || !draft.startDate || !draft.endDate} variant="contained" onClick={async () => { const result = await onSave(draft); if (result) { onClose(); } }}>{t("travelCalendar:drawer.save")}</Button>}
        </Stack>
      </Stack>
    </Drawer>
  );
};
