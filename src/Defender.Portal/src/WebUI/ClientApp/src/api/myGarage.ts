import APICallWrapper from "src/api/APIWrapper/APICallWrapper";
import RequestParamsBuilder from "src/api/APIWrapper/RequestParamsBuilder";
import type { APICallFailure } from "src/api/APIWrapper/interfaces/APICallProps";
import apiUrls from "src/api/apiUrls";
import type IUtils from "src/appUtils/interface";
import type {
  CreateInsurancePolicyRequest,
  CreateMaintenanceItemRequest,
  CreateServiceHistoryRequest,
  CreateVehicleRequest,
  UpdateInsurancePolicyRequest,
  UpdateMaintenanceItemRequest,
  UpdateServiceHistoryRequest,
  UpdateVehicleRequest,
} from "src/models/myGarage/CarRequests";
import type {
  InsurancePolicy,
  MaintenanceItem,
  ServiceHistoryPage,
  ServiceHistoryRecord,
  Vehicle,
  VehicleDetail,
  VehicleSummary,
} from "src/models/myGarage/CarModels";

const replacePath = (template: string, values: Record<string, string>): string =>
  Object.entries(values).reduce(
    (path, [key, value]) => path.replace(`:${key}`, encodeURIComponent(value)),
    template,
  );

const requestJson = <T>(
  url: string,
  method: string,
  body: unknown,
  utils?: IUtils | null,
  signal?: AbortSignal,
): Promise<T> =>
  new Promise<T>((resolve, reject) => {
    APICallWrapper({
      url,
      options: {
        method,
        ...(body === undefined ? {} : { body: RequestParamsBuilder.BuildBody(body) }),
        ...(signal === undefined ? {} : { signal }),
      },
      utils,
      showError: false,
      onSuccess: async (response) => resolve((await response.json()) as T),
      onFailure: async (failure: APICallFailure) => reject(failure),
    });
  });

const requestVoid = (
  url: string,
  method: string,
  utils?: IUtils | null,
  signal?: AbortSignal,
): Promise<void> =>
  new Promise<void>((resolve, reject) => {
    APICallWrapper({
      url,
      options: { method, ...(signal === undefined ? {} : { signal }) },
      utils,
      showError: false,
      onSuccess: async () => resolve(),
      onFailure: async (failure: APICallFailure) => reject(failure),
    });
  });

const garageUrls = apiUrls.myGarage;

export const myGarageApi = {
  getVehicles: (includeArchived = false, utils?: IUtils | null, signal?: AbortSignal): Promise<VehicleSummary[]> =>
    requestJson<VehicleSummary[]>(
      `${garageUrls.getVehicles}${RequestParamsBuilder.BuildQuery({ includeArchived })}`,
      "GET",
      undefined,
      utils,
      signal,
    ),

  createVehicle: (request: CreateVehicleRequest, utils?: IUtils | null, signal?: AbortSignal): Promise<Vehicle> =>
    requestJson<Vehicle>(garageUrls.createVehicle, "POST", request, utils, signal),

  getVehicle: (vehicleId: string, utils?: IUtils | null, signal?: AbortSignal): Promise<VehicleDetail> =>
    requestJson<VehicleDetail>(replacePath(garageUrls.getVehicle, { vehicleId }), "GET", undefined, utils, signal),

  updateVehicle: (vehicleId: string, request: UpdateVehicleRequest, utils?: IUtils | null, signal?: AbortSignal): Promise<Vehicle> =>
    requestJson<Vehicle>(replacePath(garageUrls.updateVehicle, { vehicleId }), "PUT", request, utils, signal),

  archiveVehicle: (vehicleId: string, utils?: IUtils | null, signal?: AbortSignal): Promise<Vehicle> =>
    requestJson<Vehicle>(replacePath(garageUrls.archiveVehicle, { vehicleId }), "POST", undefined, utils, signal),

  unarchiveVehicle: (vehicleId: string, utils?: IUtils | null, signal?: AbortSignal): Promise<Vehicle> =>
    requestJson<Vehicle>(replacePath(garageUrls.unarchiveVehicle, { vehicleId }), "POST", undefined, utils, signal),

  getMaintenanceItems: (vehicleId: string, utils?: IUtils | null, signal?: AbortSignal): Promise<MaintenanceItem[]> =>
    requestJson<MaintenanceItem[]>(replacePath(garageUrls.getMaintenanceItems, { vehicleId }), "GET", undefined, utils, signal),

  createMaintenanceItem: (vehicleId: string, request: CreateMaintenanceItemRequest, utils?: IUtils | null, signal?: AbortSignal): Promise<MaintenanceItem> =>
    requestJson<MaintenanceItem>(replacePath(garageUrls.createMaintenanceItem, { vehicleId }), "POST", request, utils, signal),

  updateMaintenanceItem: (vehicleId: string, maintenanceId: string, request: UpdateMaintenanceItemRequest, utils?: IUtils | null, signal?: AbortSignal): Promise<MaintenanceItem> =>
    requestJson<MaintenanceItem>(replacePath(garageUrls.updateMaintenanceItem, { vehicleId, maintenanceId }), "PUT", request, utils, signal),

  deleteMaintenanceItem: (vehicleId: string, maintenanceId: string, utils?: IUtils | null, signal?: AbortSignal): Promise<void> =>
    requestVoid(replacePath(garageUrls.deleteMaintenanceItem, { vehicleId, maintenanceId }), "DELETE", utils, signal),

  getHistory: (vehicleId: string, page = 0, pageSize = 25, utils?: IUtils | null, signal?: AbortSignal): Promise<ServiceHistoryPage> =>
    requestJson<ServiceHistoryPage>(
      `${replacePath(garageUrls.getHistory, { vehicleId })}${RequestParamsBuilder.BuildQuery({ page, pageSize })}`,
      "GET",
      undefined,
      utils,
      signal,
    ),

  createHistory: (vehicleId: string, request: CreateServiceHistoryRequest, utils?: IUtils | null, signal?: AbortSignal): Promise<ServiceHistoryRecord> =>
    requestJson<ServiceHistoryRecord>(replacePath(garageUrls.createHistory, { vehicleId }), "POST", request, utils, signal),

  updateHistory: (vehicleId: string, historyId: string, request: UpdateServiceHistoryRequest, utils?: IUtils | null, signal?: AbortSignal): Promise<ServiceHistoryRecord> =>
    requestJson<ServiceHistoryRecord>(replacePath(garageUrls.updateHistory, { vehicleId, historyId }), "PUT", request, utils, signal),

  deleteHistory: (vehicleId: string, historyId: string, utils?: IUtils | null, signal?: AbortSignal): Promise<void> =>
    requestVoid(replacePath(garageUrls.deleteHistory, { vehicleId, historyId }), "DELETE", utils, signal),

  getInsurancePolicies: (vehicleId: string, utils?: IUtils | null, signal?: AbortSignal): Promise<InsurancePolicy[]> =>
    requestJson<InsurancePolicy[]>(replacePath(garageUrls.getInsurancePolicies, { vehicleId }), "GET", undefined, utils, signal),

  createInsurancePolicy: (vehicleId: string, request: CreateInsurancePolicyRequest, utils?: IUtils | null, signal?: AbortSignal): Promise<InsurancePolicy> =>
    requestJson<InsurancePolicy>(replacePath(garageUrls.createInsurancePolicy, { vehicleId }), "POST", request, utils, signal),

  updateInsurancePolicy: (vehicleId: string, insuranceId: string, request: UpdateInsurancePolicyRequest, utils?: IUtils | null, signal?: AbortSignal): Promise<InsurancePolicy> =>
    requestJson<InsurancePolicy>(replacePath(garageUrls.updateInsurancePolicy, { vehicleId, insuranceId }), "PUT", request, utils, signal),
};

export const {
  getVehicles,
  createVehicle,
  getVehicle,
  updateVehicle,
  archiveVehicle,
  unarchiveVehicle,
  getMaintenanceItems,
  createMaintenanceItem,
  updateMaintenanceItem,
  deleteMaintenanceItem,
  getHistory,
  createHistory,
  updateHistory,
  deleteHistory,
  getInsurancePolicies,
  createInsurancePolicy,
  updateInsurancePolicy,
} = myGarageApi;
