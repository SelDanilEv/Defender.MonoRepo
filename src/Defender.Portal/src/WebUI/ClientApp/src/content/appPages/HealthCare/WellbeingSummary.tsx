import { Box, Chip, Stack, Typography } from "@mui/material";
import { HealthEvent } from "src/api/healthCare";
import {
  ChartTimeRange,
  getLatestWellbeingEvent,
  wellbeingScoreToEmoji,
} from "./chartData";

interface WellbeingSummaryProps {
  events: HealthEvent[];
  timeRange: ChartTimeRange;
  title: string;
  scoreLabel: (score: number) => string;
}

const WellbeingSummary = ({
  events,
  timeRange,
  title,
  scoreLabel,
}: WellbeingSummaryProps) => {
  const wellbeingEvent = getLatestWellbeingEvent(events, timeRange);

  if (!wellbeingEvent?.wellbeingScore) {
    return null;
  }

  return (
    <Box
      sx={{
        border: (theme) => `1px solid ${theme.palette.divider}`,
        borderRadius: 1,
        p: 1.25,
        mb: 1.5,
      }}
    >
      <Stack direction="row" spacing={1.5} alignItems="center" flexWrap="wrap">
        <Typography variant="subtitle2">{title}</Typography>
        <Typography variant="h4" component="span" lineHeight={1}>
          {wellbeingScoreToEmoji(wellbeingEvent.wellbeingScore)}
        </Typography>
        <Chip
          size="small"
          color="primary"
          variant="outlined"
          label={scoreLabel(wellbeingEvent.wellbeingScore)}
        />
        <Typography variant="body2" color="text.secondary">
          {new Date(wellbeingEvent.startedAt).toLocaleString([], {
            day: "2-digit",
            month: "2-digit",
            hour: "2-digit",
            minute: "2-digit",
          })}
        </Typography>
      </Stack>
    </Box>
  );
};

export default WellbeingSummary;
