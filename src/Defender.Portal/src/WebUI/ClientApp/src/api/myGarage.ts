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
): Promise<T> =>
  new Promise<T>((resolve, reject) => {
    APICallWrapper({
      url,
      options: {
        method,
        ...(body === undefined ? {} : { body: RequestParamsBuilder.BuildBody(body) }),
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
): Promise<void> =>
  new Promise<void>((resolve, reject) => {
    APICallWrapper({
      url,
      options: { method },
      utils,
      showError: false,
      onSuccess: async () => resolve(),
      onFailure: async (failure: APICallFailure) => reject(failure),
    });
  });

const garageUrls = apiUrls.myGarage;

export const myGarageApi = {
  getVehicles: (includeArchived = false, utils?: IUtils | null): Promise<VehicleSummary[]> =>
    requestJson<VehicleSummary[]>(
      `${garageUrls.getVehicles}${RequestParamsBuilder.BuildQuery({ includeArchived })}`,
      "GET",
      undefined,
      utils,
    ),

  createVehicle: (request: CreateVehicleRequest, utils?: IUtils | null): Promise<Vehicle> =>
    requestJson<Vehicle>(garageUrls.createVehicle, "POST", request, utils),

  getVehicle: (vehicleId: string, utils?: IUtils | null): Promise<VehicleDetail> =>
    requestJson<VehicleDetail>(replacePath(garageUrls.getVehicle, { vehicleId }), "GET", undefined, utils),

  updateVehicle: (vehicleId: string, request: UpdateVehicleRequest, utils?: IUtils | null): Promise<Vehicle> =>
    requestJson<Vehicle>(replacePath(garageUrls.updateVehicle, { vehicleId }), "PUT", request, utils),

  archiveVehicle: (vehicleId: string, utils?: IUtils | null): Promise<Vehicle> =>
    requestJson<Vehicle>(replacePath(garageUrls.archiveVehicle, { vehicleId }), "POST", undefined, utils),

  unarchiveVehicle: (vehicleId: string, utils?: IUtils | null): Promise<Vehicle> =>
    requestJson<Vehicle>(replacePath(garageUrls.unarchiveVehicle, { vehicleId }), "POST", undefined, utils),

  getMaintenanceItems: (vehicleId: string, utils?: IUtils | null): Promise<MaintenanceItem[]> =>
    requestJson<MaintenanceItem[]>(replacePath(garageUrls.getMaintenanceItems, { vehicleId }), "GET", undefined, utils),

  createMaintenanceItem: (vehicleId: string, request: CreateMaintenanceItemRequest, utils?: IUtils | null): Promise<MaintenanceItem> =>
    requestJson<MaintenanceItem>(replacePath(garageUrls.createMaintenanceItem, { vehicleId }), "POST", request, utils),

  updateMaintenanceItem: (vehicleId: string, maintenanceId: string, request: UpdateMaintenanceItemRequest, utils?: IUtils | null): Promise<MaintenanceItem> =>
    requestJson<MaintenanceItem>(replacePath(garageUrls.updateMaintenanceItem, { vehicleId, maintenanceId }), "PUT", request, utils),

  deleteMaintenanceItem: (vehicleId: string, maintenanceId: string, utils?: IUtils | null): Promise<void> =>
    requestVoid(replacePath(garageUrls.deleteMaintenanceItem, { vehicleId, maintenanceId }), "DELETE", utils),

  getHistory: (vehicleId: string, page = 0, pageSize = 25, utils?: IUtils | null): Promise<ServiceHistoryPage> =>
    requestJson<ServiceHistoryPage>(
      `${replacePath(garageUrls.getHistory, { vehicleId })}${RequestParamsBuilder.BuildQuery({ page, pageSize })}`,
      "GET",
      undefined,
      utils,
    ),

  createHistory: (vehicleId: string, request: CreateServiceHistoryRequest, utils?: IUtils | null): Promise<ServiceHistoryRecord> =>
    requestJson<ServiceHistoryRecord>(replacePath(garageUrls.createHistory, { vehicleId }), "POST", request, utils),

  updateHistory: (vehicleId: string, historyId: string, request: UpdateServiceHistoryRequest, utils?: IUtils | null): Promise<ServiceHistoryRecord> =>
    requestJson<ServiceHistoryRecord>(replacePath(garageUrls.updateHistory, { vehicleId, historyId }), "PUT", request, utils),

  deleteHistory: (vehicleId: string, historyId: string, utils?: IUtils | null): Promise<void> =>
    requestVoid(replacePath(garageUrls.deleteHistory, { vehicleId, historyId }), "DELETE", utils),

  getInsurancePolicies: (vehicleId: string, utils?: IUtils | null): Promise<InsurancePolicy[]> =>
    requestJson<InsurancePolicy[]>(replacePath(garageUrls.getInsurancePolicies, { vehicleId }), "GET", undefined, utils),

  createInsurancePolicy: (vehicleId: string, request: CreateInsurancePolicyRequest, utils?: IUtils | null): Promise<InsurancePolicy> =>
    requestJson<InsurancePolicy>(replacePath(garageUrls.createInsurancePolicy, { vehicleId }), "POST", request, utils),

  updateInsurancePolicy: (vehicleId: string, insuranceId: string, request: UpdateInsurancePolicyRequest, utils?: IUtils | null): Promise<InsurancePolicy> =>
    requestJson<InsurancePolicy>(replacePath(garageUrls.updateInsurancePolicy, { vehicleId, insuranceId }), "PUT", request, utils),
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
