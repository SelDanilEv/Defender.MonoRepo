import { TextFieldProps } from "@mui/material";

type LockedChipListProps = Omit<TextFieldProps, "onChange" | "value"> & {
  isLoading?: boolean;
  dispatch?: unknown;
  initialChips?: string[];
  onChange?: (chips: string[]) => void;
};

export default LockedChipListProps;
