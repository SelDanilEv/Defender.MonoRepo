import { DatePickerProps } from "@mui/x-date-pickers/DatePicker";

export default interface LockedDatePickerProps
  extends DatePickerProps {
  isLoading?: boolean;
  dispatch?: any;
}
