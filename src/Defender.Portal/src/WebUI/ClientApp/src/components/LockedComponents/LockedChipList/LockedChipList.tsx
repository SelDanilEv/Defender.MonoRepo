import { alpha, Box, InputBase, Typography } from "@mui/material";
import type { KeyboardEvent } from "react";
import { connect } from "react-redux";
import { useEffect, useState } from "react";

import LockedChipListProps from "./LockedChipListProps";
import TagChip from "src/components/TagChip";

const LockedChipList = ({
  isLoading,
  dispatch,
  initialChips: chips,
  onChange,
  ...restProps
}: LockedChipListProps) => {
  const [chipData, setChipData] = useState<string[]>(chips ?? []);
  const [inputValue, setInputValue] = useState<string>("");

  const handleKeyDown = (event: KeyboardEvent<HTMLInputElement>) => {
    if (event.key === "Enter" && inputValue.trim() !== "") {
      event.preventDefault();
      if (!chipData.includes(inputValue.trim())) {
        setChipData((currentChips) => [...currentChips, inputValue.trim()]);
      }
      setInputValue("");
    }
  };

  const handleDelete = (chipToDelete: string) => () => {
    setChipData((chips) => chips.filter((chip) => chip !== chipToDelete));
  };

  useEffect(() => {
    if (onChange) onChange(chipData);
  }, [chipData, onChange]);

  useEffect(() => {
    setChipData(chips ?? []);
  }, [chips]);

  const {
    disabled,
    label,
    helperText,
    placeholder,
    fullWidth,
    sx,
    variant,
    ...inputProps
  } = restProps;

  return (
    <Box
      sx={[
        {
          width: fullWidth ? "100%" : "auto",
        },
        ...(Array.isArray(sx) ? sx : sx ? [sx] : []),
      ]}
    >
      {label && (
        <Typography
          variant="caption"
          color="text.secondary"
          sx={{ display: "block", mb: 0.75, textTransform: "none" }}
        >
          {label}
        </Typography>
      )}
      <Box
        sx={(theme) => ({
          display: "flex",
          alignItems: "center",
          alignContent: "center",
          flexWrap: "wrap",
          gap: 0.75,
          width: "100%",
          minHeight: 38,
          py: variant === "standard" ? 0.25 : 0.5,
          pb: variant === "standard" ? 0.5 : 0.75,
          px: variant === "standard" ? 0 : 1,
          borderBottom: variant === "standard"
            ? `1px solid ${alpha(theme.palette.text.primary, 0.35)}`
            : undefined,
          borderRadius: variant === "outlined" ? 1 : 0,
          border: variant === "outlined"
            ? `1px solid ${alpha(theme.palette.text.primary, 0.23)}`
            : undefined,
          "&:focus-within": {
            borderColor: theme.palette.primary.main,
          },
        })}
      >
        {chipData.map((chip) => (
          <TagChip
            disabled={isLoading || disabled}
            key={chip}
            label={chip}
            onDelete={handleDelete(chip)}
          />
        ))}
        <InputBase
          {...(inputProps as any)}
          value={inputValue}
          onChange={(e) => setInputValue(e.target.value)}
          onKeyDown={handleKeyDown}
          disabled={isLoading || disabled}
          placeholder={placeholder}
          sx={{
            flex: "1 1 120px",
            minWidth: 96,
            "& input": {
              p: 0,
              height: "auto",
              lineHeight: 1.4,
            },
          }}
        />
      </Box>
      {helperText && (
        <Typography variant="caption" color="text.secondary" sx={{ mt: 0.75 }}>
          {helperText}
        </Typography>
      )}
    </Box>
  );
};

const mapStateToProps = (state: any) => {
  return {
    isLoading: state.loading.loading,
  };
};

export default connect(mapStateToProps)(LockedChipList);
