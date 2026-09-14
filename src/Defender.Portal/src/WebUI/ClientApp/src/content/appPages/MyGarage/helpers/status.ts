import type { APICallFailure } from "src/api/APIWrapper/interfaces/APICallProps";
import { HistoryType, InsuranceStatus, MaintenanceStatus } from "src/models/myGarage/CarModels";

export type GarageStatusColor = "default" | "error" | "warning" | "success" | "info";

export const getStatusLabelKey = (status: MaintenanceStatus | InsuranceStatus | HistoryType): string =>
  status in HistoryType ? `types.${status}` : `statuses.${status}`;

export const getStatusColor = (status: MaintenanceStatus): GarageStatusColor => {
  switch (status) {
    case MaintenanceStatus.Overdue:
      return "error";
    case MaintenanceStatus.DueSoon:
      return "warning";
    case MaintenanceStatus.Upcoming:
      return "success";
    default:
      return "default";
  }
};

export const getInsuranceStatusColor = (status: InsuranceStatus): GarageStatusColor => {
  switch (status) {
    case InsuranceStatus.Active:
      return "success";
    case InsuranceStatus.ExpiringSoon:
      return "warning";
    default:
      return "default";
  }
};

export const getGarageFailureMessage = (
  failure: Partial<APICallFailure> | null | undefined,
  translate: (key: string) => string,
): string => {
  const code = failure?.code;
  if (code) {
    const localized = translate(`errors.${code}`);
    if (localized && localized !== `errors.${code}`) return localized;
  }

  const detail = failure?.detail;
  if (detail) {
    const localized = translate(`errors.${detail}`);
    if (localized && localized !== `errors.${detail}`) return localized;
  }

  return translate("errors.generic");
};
