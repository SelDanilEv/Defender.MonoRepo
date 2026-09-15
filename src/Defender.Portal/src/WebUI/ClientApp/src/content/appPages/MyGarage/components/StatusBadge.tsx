import { Chip } from "@mui/material";
import { useTranslation } from "react-i18next";

import { HistoryType, InsuranceStatus, MaintenanceStatus } from "src/models/myGarage/CarModels";

import { getInsuranceStatusColor, getStatusColor, getStatusLabelKey } from "../helpers/status";

type Status = MaintenanceStatus | InsuranceStatus | HistoryType;

interface StatusBadgeProps {
  status: Status;
  size?: "small" | "medium";
}

export default function StatusBadge({ status, size = "small" }: StatusBadgeProps) {
  const { t } = useTranslation("myGarage");
  const color = Object.values(MaintenanceStatus).includes(status as MaintenanceStatus)
    ? getStatusColor(status as MaintenanceStatus)
    : Object.values(InsuranceStatus).includes(status as InsuranceStatus)
      ? getInsuranceStatusColor(status as InsuranceStatus)
      : "info";

  return <Chip color={color} label={t(getStatusLabelKey(status))} size={size} />;
}
