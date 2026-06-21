import DeviceThermostatIcon from "@mui/icons-material/DeviceThermostat";
import { Box, Slider, Stack, Typography } from "@mui/material";

interface TemperatureSliderProps {
  label: string;
  value: string;
  onChange: (value: string) => void;
}

export const minTemperature = 36.4;
export const maxTemperature = 40.5;

export const normalizeTemperature = (value: string) => {
  const parsed = Number(value);

  if (Number.isNaN(parsed)) {
    return 37;
  }

  return Math.max(minTemperature, Math.min(maxTemperature, parsed));
};

const TemperatureSlider = ({ label, value, onChange }: TemperatureSliderProps) => {
  const temperature = normalizeTemperature(value);

  return (
    <Box>
      <Stack direction="row" alignItems="center" spacing={1.5}>
        <DeviceThermostatIcon color="primary" />
        <Box flex={1}>
          <Typography variant="caption" color="text.secondary">
            {label}
          </Typography>
          <Slider
            aria-label={label}
            value={temperature}
            min={minTemperature}
            max={maxTemperature}
            step={0.1}
            valueLabelDisplay="auto"
            onChange={(_, nextValue) => {
              onChange((nextValue as number).toFixed(1));
            }}
          />
        </Box>
        <Typography
          variant="body2"
          sx={{ minWidth: 48, textAlign: "right", fontWeight: 600 }}
        >
          {`${temperature.toFixed(1)} \u00b0C`}
        </Typography>
      </Stack>
    </Box>
  );
};

export default TemperatureSlider;
