import { Box, Button, Stack, Typography } from "@mui/material";

import CustomDialog from "src/components/Dialog";

interface DishRatingDialogProps {
  open: boolean;
  dishName: string | null;
  ratingValue: number;
  submitting?: boolean;
  title: string;
  submitLabel: string;
  cancelLabel: string;
  lowHint: string;
  highHint: string;
  onChange: (value: number) => void;
  onClose: () => void;
  onSubmit: () => void;
}

const DishRatingDialog = ({
  open,
  dishName,
  ratingValue,
  submitting = false,
  title,
  submitLabel,
  cancelLabel,
  lowHint,
  highHint,
  onChange,
  onClose,
  onSubmit,
}: DishRatingDialogProps) => {
  return (
    <CustomDialog title={title} open={open} onClose={onClose}>
      <Stack spacing={2.5} sx={{ minWidth: { xs: 280, sm: 420 } }}>
        <Box>
          <Typography variant="subtitle1" sx={{ fontWeight: 700 }}>
            {dishName}
          </Typography>
          <Typography variant="body2" color="text.secondary">
            {lowHint} | {highHint}
          </Typography>
        </Box>
        <Stack direction="row" spacing={1} useFlexGap flexWrap="wrap">
          {[1, 2, 3, 4, 5].map((value) => (
            <Button
              key={value}
              size="large"
              variant={ratingValue === value ? "contained" : "outlined"}
              onClick={() => onChange(value)}
              sx={{ minWidth: 56 }}
            >
              {value}
            </Button>
          ))}
        </Stack>
        <Box display="flex" gap={1} justifyContent="flex-end">
          <Button onClick={onClose}>{cancelLabel}</Button>
          <Button variant="contained" onClick={onSubmit} disabled={submitting}>
            {submitLabel}
          </Button>
        </Box>
      </Stack>
    </CustomDialog>
  );
};

export default DishRatingDialog;
