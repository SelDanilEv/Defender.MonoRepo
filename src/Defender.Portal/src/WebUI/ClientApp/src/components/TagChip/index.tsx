import { alpha, Chip } from "@mui/material";
import type { ChipProps } from "@mui/material";

type TagChipTone = "neutral" | "positive";

interface TagChipProps extends Omit<ChipProps, "color"> {
  tone?: TagChipTone;
}

const TagChip = ({ tone = "neutral", sx, ...props }: TagChipProps) => {
  return (
    <Chip
      {...props}
      sx={[
        (theme) => {
          const isPositive = tone === "positive";
          const neutralBorder = theme.palette.mode === "dark"
            ? alpha(theme.palette.info.light, 0.28)
            : alpha(theme.palette.primary.main, 0.18);
          const neutralBackground = theme.palette.mode === "dark"
            ? alpha(theme.palette.info.light, 0.16)
            : alpha(theme.palette.primary.main, 0.12);

          return {
            borderRadius: 1.5,
            fontWeight: 600,
            border: `1px solid ${
              isPositive
                ? alpha(theme.palette.success.light, 0.75)
                : neutralBorder
            }`,
            backgroundColor: isPositive
              ? theme.palette.success.main
              : neutralBackground,
            color: isPositive ? theme.palette.common.white : theme.palette.text.primary,
            "& .MuiChip-deleteIcon": {
              color: isPositive
                ? alpha(theme.palette.common.white, 0.82)
                : alpha(theme.palette.text.primary, 0.6),
            },
            "& .MuiChip-deleteIcon:hover": {
              color: isPositive
                ? theme.palette.common.white
                : theme.palette.text.primary,
            },
          };
        },
        ...(Array.isArray(sx) ? sx : sx ? [sx] : []),
      ]}
    />
  );
};

export default TagChip;
