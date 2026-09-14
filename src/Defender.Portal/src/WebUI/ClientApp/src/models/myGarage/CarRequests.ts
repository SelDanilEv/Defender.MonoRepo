import type { Currency, DateOnly, HistoryType } from "./CarModels";

export interface CreateVehicleRequest {
  displayName: string;
  make: string;
  model: string;
  year: number;
  plate: string;
  vin?: string | null;
}

export type UpdateVehicleRequest = CreateVehicleRequest;

export interface CreateMaintenanceItemRequest {
  name: string;
  intervalMonths?: number | null;
  intervalThousandKm?: number | null;
  lastDate?: DateOnly | null;
  lastOdometerKm?: number | null;
  manualBaselineDate?: DateOnly | null;
  manualBaselineOdometerKm?: number | null;
}

export type UpdateMaintenanceItemRequest = CreateMaintenanceItemRequest;

export interface CreateServiceHistoryRequest {
  date: DateOnly;
  odometerKm: number;
  type: HistoryType;
  title: string;
  notes?: string | null;
  linkedMaintenanceItemIds?: string[];
  costAmountMinor: number | null;
  costCurrency: Currency | null;
}

export type UpdateServiceHistoryRequest = CreateServiceHistoryRequest;

export interface CreateInsurancePolicyRequest {
  provider: string;
  policyNumber?: string | null;
  coverageType?: string | null;
  startDate: DateOnly;
  endDate: DateOnly;
  notes?: string | null;
}

export type UpdateInsurancePolicyRequest = CreateInsurancePolicyRequest;
