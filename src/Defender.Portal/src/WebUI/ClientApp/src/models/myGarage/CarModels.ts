import { Currency } from "src/models/shared/Currency";

export { Currency };

export type DateOnly = string;

export enum HistoryType {
  Maintenance = "Maintenance",
  Repair = "Repair",
  Tire = "Tire",
  Other = "Other",
}

export enum MaintenanceStatus {
  Upcoming = "Upcoming",
  DueSoon = "DueSoon",
  Overdue = "Overdue",
  NotStarted = "NotStarted",
}

export enum InsuranceStatus {
  Active = "Active",
  ExpiringSoon = "ExpiringSoon",
  Expired = "Expired",
}

export interface Vehicle {
  id: string;
  displayName: string;
  make: string;
  model: string;
  year: number;
  plate: string;
  vin: string | null;
  archived: boolean;
  currentOdometerKm: number | null;
  version: number;
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface MaintenanceStatusCounts {
  overdue: number;
  dueSoon: number;
  upcoming: number;
  notStarted: number;
}

export interface VehicleSummary {
  id: string;
  displayName: string;
  make: string;
  model: string;
  year: number;
  plate: string;
  vin: string | null;
  archived: boolean;
  currentOdometerKm: number | null;
  maintenanceCounts: MaintenanceStatusCounts;
  insuranceStatus: InsuranceStatus | null;
}

export interface VehicleDetail {
  vehicle: Vehicle;
  maintenanceItems: MaintenanceItem[];
  insurancePolicies: InsurancePolicy[];
}

export interface MaintenanceItem {
  id: string;
  vehicleId: string;
  name: string;
  intervalMonths: number | null;
  intervalThousandKm: number | null;
  lastDate: DateOnly | null;
  lastOdometerKm: number | null;
  nextDate: DateOnly | null;
  nextOdometerKm: number | null;
  status: MaintenanceStatus;
}

export interface ServiceHistoryRecord {
  id: string;
  vehicleId: string;
  date: DateOnly;
  odometerKm: number;
  type: HistoryType;
  title: string;
  notes: string | null;
  linkedMaintenanceItemIds: string[];
  costAmountMinor: number | null;
  costCurrency: Currency | null;
}

export interface ServiceHistoryPage {
  items: ServiceHistoryRecord[];
  totalItemsCount: number;
  currentPage: number;
  pageSize: number;
  totalPagesCount: number;
}

export interface InsurancePolicy {
  id: string;
  vehicleId: string;
  provider: string;
  policyNumber: string | null;
  coverageType: string | null;
  startDate: DateOnly;
  endDate: DateOnly;
  notes: string | null;
  status: InsuranceStatus;
}

export type VehicleDto = Vehicle;
export type VehicleSummaryDto = VehicleSummary;
export type VehicleDetailDto = VehicleDetail;
export type MaintenanceStatusCountsDto = MaintenanceStatusCounts;
export type MaintenanceItemDto = MaintenanceItem;
export type ServiceHistoryRecordDto = ServiceHistoryRecord;
export type ServiceHistoryPageDto = ServiceHistoryPage;
export type InsurancePolicyDto = InsurancePolicy;
