import { Box, Chip, Stack, Typography } from "@mui/material";
import { HealthEvent } from "src/api/healthCare";
import {
  ChartTimeRange,
  getLatestWellbeingEvent,
  wellbeingScoreToEmoji,
} from "./chartData";
import { formatEventDateTime } from "./dateFormat";

interface WellbeingSummaryProps {
  events: HealthEvent[];
  timeRange: ChartTimeRange;
  title: string;
  scoreLabel: (score: number) => string;
  language: string;
}

const WellbeingSummary = ({
  events,
  timeRange,
  title,
  scoreLabel,
  language,
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
      <Stack
        direction="row"
        spacing={1.5}
        sx={{
          alignItems: "center",
          flexWrap: "wrap"
        }}>
        <Typography variant="subtitle2">{title}</Typography>
        <Typography variant="h4" component="span" sx={{
          lineHeight: 1
        }}>
          {wellbeingScoreToEmoji(wellbeingEvent.wellbeingScore)}
        </Typography>
        <Chip
          size="small"
          color="primary"
          variant="outlined"
          label={scoreLabel(wellbeingEvent.wellbeingScore)}
        />
        <Typography variant="body2" sx={{
          color: "text.secondary"
        }}>
          {formatEventDateTime(new Date(wellbeingEvent.startedAt), language)}
        </Typography>
      </Stack>
    </Box>
  );
};

export default WellbeingSummary;
