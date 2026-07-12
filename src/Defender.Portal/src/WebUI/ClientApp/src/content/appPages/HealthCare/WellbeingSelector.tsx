import { Button, Stack, Tooltip, Typography } from "@mui/material";
import { wellbeingScoreToEmoji } from "./chartData";

interface WellbeingSelectorProps {
  label: string;
  score: number;
  descriptions: string[];
  onChange: (score: number) => void;
}

const wellbeingScores = [1, 2, 3, 4, 5];

const WellbeingSelector = ({
  label,
  score,
  descriptions,
  onChange,
}: WellbeingSelectorProps) => (
  <Stack spacing={1}>
    <Typography variant="caption" sx={{
      color: "text.secondary"
    }}>
      {label}
    </Typography>
    <Stack direction="row" spacing={1} useFlexGap sx={{
      flexWrap: "wrap"
    }}>
      {wellbeingScores.map((value) => (
        <Tooltip
          key={value}
          title={descriptions[value - 1] || `${value}/5`}
        >
          <Button
            variant={score === value ? "contained" : "outlined"}
            size="small"
            onClick={() => onChange(value)}
            sx={{ minWidth: 44, fontSize: "1.2rem", lineHeight: 1.2 }}
          >
            {wellbeingScoreToEmoji(value)}
          </Button>
        </Tooltip>
      ))}
    </Stack>
  </Stack>
);

export default WellbeingSelector;
