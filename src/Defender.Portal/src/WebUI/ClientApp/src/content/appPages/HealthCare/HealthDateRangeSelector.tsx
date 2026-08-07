import { useEffect, useMemo, useState } from "react";
import { Button, FormHelperText, MenuItem, Stack, TextField } from "@mui/material";
import { DateTimePicker } from "@mui/x-date-pickers/DateTimePicker";
import useUtils from "src/appUtils";
import {
  DateRangeBounds,
  HealthDateRangePreset,
  HealthDateRangeSelection,
  validateHealthDateRange,
} from "./dateRange";

interface HealthDateRangeSelectorProps {
  value: HealthDateRangeSelection;
  onChange: (value: HealthDateRangeSelection) => void;
  allowedBounds?: DateRangeBounds;
  disabled?: boolean;
  showApply?: boolean;
  applyLabel?: string;
  applyDisabled?: boolean;
  onApply?: () => void;
}

const presets: HealthDateRangePreset[] = ["day", "week", "month", "all"];

const snapDate = (value: Date) => {
  const snapped = new Date(value);
  snapped.setSeconds(0, 0);
  snapped.setMinutes(snapped.getMinutes() < 30 ? 0 : 30);

  return snapped;
};

const clampToAllowedBounds = (value: Date, bounds?: DateRangeBounds) => {
  const timestamp = Math.max(
    bounds?.from?.getTime() ?? Number.MIN_SAFE_INTEGER,
    Math.min(bounds?.to?.getTime() ?? Number.MAX_SAFE_INTEGER, value.getTime())
  );

  return new Date(timestamp);
};

const getInitialCustomDates = (allowedBounds?: DateRangeBounds) => {
  const to = clampToAllowedBounds(
    allowedBounds?.to ? new Date(allowedBounds.to) : snapDate(new Date()),
    allowedBounds
  );
  const from = clampToAllowedBounds(
    allowedBounds?.from
      ? new Date(allowedBounds.from)
      : new Date(to.getTime() - 24 * 60 * 60 * 1000),
    allowedBounds
  );

  return { from, to };
};

const HealthDateRangeSelector = ({
  value,
  onChange,
  allowedBounds,
  disabled = false,
  showApply = false,
  applyLabel,
  applyDisabled = false,
  onApply,
}: HealthDateRangeSelectorProps) => {
  const u = useUtils();
  const initialDates = useMemo(
    () =>
      value.kind === "custom"
        ? { from: new Date(value.from), to: new Date(value.to) }
        : getInitialCustomDates(allowedBounds),
    [value, allowedBounds]
  );
  const [customFrom, setCustomFrom] = useState<Date | null>(initialDates.from);
  const [customTo, setCustomTo] = useState<Date | null>(initialDates.to);

  useEffect(() => {
    setCustomFrom(initialDates.from);
    setCustomTo(initialDates.to);
  }, [initialDates]);

  const customValidationError =
    value.kind === "custom"
      ? validateHealthDateRange(customFrom, customTo)
      : null;
  const selectedValue = value.kind === "custom" ? "custom" : value.preset;
  const fromMaxDateTime =
    customTo && allowedBounds?.to
      ? new Date(Math.min(customTo.getTime(), allowedBounds.to.getTime()))
      : customTo ?? allowedBounds?.to;
  const toMinDateTime =
    customFrom && allowedBounds?.from
      ? new Date(Math.max(customFrom.getTime(), allowedBounds.from.getTime()))
      : customFrom ?? allowedBounds?.from;

  const commitCustomDates = (from: Date | null, to: Date | null) => {
    const validationError = validateHealthDateRange(from, to);
    if (validationError || !from || !to) {
      return;
    }

    onChange({
      kind: "custom",
      from: new Date(from),
      to: new Date(to),
    });
  };

  const handlePresetChange = (preset: string) => {
    if (preset === "custom") {
      const dates = getInitialCustomDates(allowedBounds);
      setCustomFrom(dates.from);
      setCustomTo(dates.to);
      commitCustomDates(dates.from, dates.to);
      return;
    }

    onChange({ kind: "preset", preset: preset as HealthDateRangePreset });
  };

  const handleFromChange = (nextFrom: Date | null) => {
    setCustomFrom(nextFrom);
    commitCustomDates(nextFrom, customTo);
  };

  const handleToChange = (nextTo: Date | null) => {
    setCustomTo(nextTo);
    commitCustomDates(customFrom, nextTo);
  };

  return (
    <Stack
      direction={{ xs: "column", sm: "row" }}
      spacing={1}
      sx={{ alignItems: { xs: "stretch", sm: "flex-start" }, flexWrap: "wrap" }}
    >
      <TextField
        select
        fullWidth
        label={u.t("healthCare:chart_time_range")}
        value={selectedValue}
        onChange={(event) => handlePresetChange(event.target.value)}
        size="small"
        disabled={disabled}
        sx={{ width: { xs: "100%", sm: 220 } }}
      >
        {presets.map((preset) => (
          <MenuItem key={preset} value={preset}>
            {u.t(`healthCare:range_${preset}`)}
          </MenuItem>
        ))}
        <MenuItem value="custom">{u.t("healthCare:range_custom")}</MenuItem>
      </TextField>
      {value.kind === "custom" && (
        <Stack
          direction={{ xs: "column", sm: "row" }}
          spacing={1}
          sx={{ flex: 1, minWidth: 0 }}
        >
          <DateTimePicker
            label={u.t("healthCare:custom_range_from")}
            value={customFrom}
            onChange={handleFromChange}
            minDateTime={allowedBounds?.from}
            maxDateTime={fromMaxDateTime}
            minutesStep={30}
            timeSteps={{ minutes: 30 }}
            skipDisabled
            disabled={disabled}
            slotProps={{
              textField: {
                size: "small",
                fullWidth: true,
                error: Boolean(customValidationError),
              },
            }}
          />
          <DateTimePicker
            label={u.t("healthCare:custom_range_to")}
            value={customTo}
            onChange={handleToChange}
            minDateTime={toMinDateTime}
            maxDateTime={allowedBounds?.to}
            minutesStep={30}
            timeSteps={{ minutes: 30 }}
            skipDisabled
            disabled={disabled}
            slotProps={{
              textField: {
                size: "small",
                fullWidth: true,
                error: Boolean(customValidationError),
              },
            }}
          />
        </Stack>
      )}
      {customValidationError && (
        <FormHelperText error sx={{ width: "100%", mt: "0 !important" }}>
          {customValidationError === "Both dates are required."
            ? u.t("healthCare:custom_range_required")
            : u.t("healthCare:custom_range_invalid_order")}
        </FormHelperText>
      )}
      {showApply && (
        <Button
          variant="outlined"
          size="small"
          disabled={disabled || applyDisabled || Boolean(customValidationError)}
          onClick={onApply}
          sx={{ alignSelf: { xs: "stretch", sm: "center" } }}
        >
          {applyLabel ?? u.t("healthCare:update_shared_range")}
        </Button>
      )}
    </Stack>
  );
};

export type { HealthDateRangeSelectorProps };
export default HealthDateRangeSelector;
