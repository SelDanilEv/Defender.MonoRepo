import { Box, Paper, Tooltip, Typography } from "@mui/material";
import { alpha, useTheme } from "@mui/material/styles";
import { useTranslation } from "react-i18next";
import { TravelEvent } from "src/api/travelCalendar";
import { buildMonthDays, eventForDate, isToday } from "../calendarMath";

const capitalize = (value: string) => value.length > 0 ? value[0].toLocaleUpperCase() + value.slice(1) : value;

export const MonthGrid = ({ year, month, events, holidays, onDate, onEvent }: { year: number; month: number; events: TravelEvent[]; holidays: { date: string; flag: string }[]; onDate: (date: string) => void; onEvent: (id: string) => void; }) => {
  const { t, i18n } = useTranslation();
  const theme = useTheme();
  const locale = i18n.language === "ru" ? "ru-RU" : "en-US";
  const monthName = capitalize(new Intl.DateTimeFormat(locale, { month: "long" }).format(new Date(year, month, 1)));
  const week = Array.from({ length: 7 }, (_, index) => new Intl.DateTimeFormat(locale, { weekday: "short" }).format(new Date(Date.UTC(2024, 0, index + 1))));
  const colors: Record<string, string> = {
    OvernightTrip: theme.colors.success.main,
    DayTrip: theme.colors.warning.main,
    Event: theme.colors.primary.main,
    Rest: theme.colors.info.main,
    Family: "#ef719c",
  };

  return (
    <Paper elevation={0} sx={{ p: 2, borderRadius: "20px", background: "var(--tc-panel)", border: "1px solid var(--tc-border)", backdropFilter: "blur(18px)" }}>
      <Typography
        component="h2"
        variant="h5"
        sx={{
          fontWeight: 800,
          textTransform: "capitalize",
          mb: 1.5
        }}>{monthName} <Box component="span" sx={{ opacity: .45, fontWeight: 500 }}>{year}</Box></Typography>
      <Box sx={{ display: "grid", gridTemplateColumns: "repeat(7, minmax(0,1fr))", gap: .6 }}>
        {week.map((day) => <Typography
          key={day}
          sx={{
            textAlign: "center",
            fontSize: 11,
            fontWeight: 800,
            color: "var(--tc-muted)"
          }}>{day}</Typography>)}
        {buildMonthDays(year, month).map((date, index) => {
          if (!date) {
            return <Box key={`empty-${index}`} />;
          }

          const event = eventForDate(events, date);
          const holiday = holidays.find((item) => item.date === date);
          const today = isToday(date);
          const day = Number(date.slice(-2));
          const weekend = index % 7 > 4;
          const label = `${date}${today ? `, ${t("travelCalendar:monthGrid.today")}` : ""}${event ? `, ${event.title}, ${event.type}` : ""}${holiday ? `, ${holiday.flag}` : ""}`;

          return (
            <Tooltip key={date} title={event?.title ?? (holiday ? holiday.flag : t("travelCalendar:monthGrid.addEvent"))}>
              <Box
                component="button"
                type="button"
                aria-label={label}
                onClick={() => event ? onEvent(event.id) : onDate(date)}
                sx={{
                  minWidth: 0,
                  minHeight: 52,
                  border: today ? `2px solid ${theme.colors.primary.main}` : event ? `1px solid ${alpha(colors[event.type], 0.5)}` : "1px solid transparent",
                  borderRadius: "10px",
                  background: event ? alpha(colors[event.type], 0.12) : weekend ? "var(--tc-weekend)" : "transparent",
                  color: "inherit",
                  cursor: "pointer",
                  position: "relative",
                  p: .6,
                  textAlign: "left",
                  transition: ".2s",
                  "&:hover,&:focus-visible": { transform: "translateY(-1px)", borderColor: today ? theme.colors.primary.main : event ? colors[event.type] : "var(--tc-accent)", outline: "none" },
                }}
              >
                <Typography
                  component="span"
                  sx={{
                    fontSize: 12,
                    fontWeight: event ? 800 : 600
                  }}>{day}</Typography>
                {today && <Typography component="span" sx={{ display: "block", color: theme.colors.primary.main, fontSize: 8, fontWeight: 800, lineHeight: 1 }}>{t("travelCalendar:monthGrid.today")}</Typography>}
                {holiday && <Typography
                  component="span"
                  sx={{
                    fontSize: 12,
                    ml: .4
                  }}>{holiday.flag}</Typography>}
                {event && <>
                  <Box sx={{ width: 6, height: 6, borderRadius: "50%", bgcolor: colors[event.type], position: "absolute", top: 7, right: 7 }} />
                  <Typography sx={{ display: { xs: "none", sm: "-webkit-box" }, WebkitLineClamp: 2, WebkitBoxOrient: "vertical", overflow: "hidden", fontSize: 9.5, lineHeight: 1.15, mt: .25 }}>{event.title}</Typography>
                </>}
              </Box>
            </Tooltip>
          );
        })}
      </Box>
    </Paper>
  );
};
