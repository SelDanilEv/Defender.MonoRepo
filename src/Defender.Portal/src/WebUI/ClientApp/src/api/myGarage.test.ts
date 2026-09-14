import APICallWrapper from "src/api/APIWrapper/APICallWrapper";
import apiUrls from "src/api/apiUrls";
import {
  myGarageApi,
} from "src/api/myGarage";
import { Currency, HistoryType } from "src/models/myGarage/CarModels";

vi.mock("src/api/APIWrapper/APICallWrapper", () => ({
  default: vi.fn(),
}));

const wrapper = vi.mocked(APICallWrapper);

const respondWith = (value: unknown, status = 200) => {
  wrapper.mockImplementationOnce(async ({ onSuccess }) => {
    await onSuccess?.(new Response(JSON.stringify(value), { status }));
  });
};

describe("myGarageApi", () => {
  beforeEach(() => {
    wrapper.mockReset();
  });

  test("getVehicles_WhenArchivedRequested_UsesTheExplicitBffRouteAndQuery", async () => {
    respondWith([]);

    await expect(myGarageApi.getVehicles(true)).resolves.toEqual([]);

    expect(wrapper).toHaveBeenCalledWith(expect.objectContaining({
      url: "/api/my-garage/vehicles?includeArchived=true",
      options: expect.objectContaining({ method: "GET" }),
    }));
  });

  test("getHistory_WhenPaged_UsesEncodedPageAndPageSizeQuery", async () => {
    respondWith({ items: [], totalItemsCount: 0, currentPage: 2, pageSize: 10, totalPagesCount: 0 });

    await expect(myGarageApi.getHistory("vehicle-1", 2, 10)).resolves.toMatchObject({ currentPage: 2 });

    expect(wrapper).toHaveBeenCalledWith(expect.objectContaining({
      url: "/api/my-garage/vehicles/vehicle-1/history?page=2&pageSize=10",
    }));
  });

  test("createHistory_WhenCostIsNull_PreservesDateEnumAndNullableCostPair", async () => {
    respondWith({ id: "history-1" });

    await myGarageApi.createHistory("vehicle-1", {
      date: "2026-09-14",
      odometerKm: 123456,
      type: HistoryType.Maintenance,
      title: "Oil change",
      notes: null,
      linkedMaintenanceItemIds: ["maintenance-1", "maintenance-2"],
      costAmountMinor: null,
      costCurrency: null,
    });

    const call = wrapper.mock.calls[0][0];
    expect(call.url).toBe("/api/my-garage/vehicles/vehicle-1/history");
    expect(call.options.method).toBe("POST");
    expect(JSON.parse(String(call.options.body))).toEqual({
      date: "2026-09-14",
      odometerKm: 123456,
      type: "Maintenance",
      title: "Oil change",
      notes: null,
      linkedMaintenanceItemIds: ["maintenance-1", "maintenance-2"],
      costAmountMinor: null,
      costCurrency: null,
    });
  });

  test("updateHistory_WhenServerReturnsConflict_RejectsWithTheUpstreamCode", async () => {
    wrapper.mockImplementationOnce(async ({ onFailure }) => {
      await onFailure?.({
        status: 409,
        code: "CAR_CONCURRENCY_CONFLICT",
        detail: "CAR_CONCURRENCY_CONFLICT",
      });
    });

    await expect(myGarageApi.updateHistory("vehicle-1", "history-1", {
      date: "2026-09-14",
      odometerKm: 123456,
      type: HistoryType.Repair,
      title: "Repair",
      notes: null,
      linkedMaintenanceItemIds: [],
      costAmountMinor: null,
      costCurrency: null,
    })).rejects.toMatchObject({
      status: 409,
      code: "CAR_CONCURRENCY_CONFLICT",
    });
  });

  test("allOperations_WhenCalled_StayOnThePortalBffAndUseExactSuffixes", async () => {
    const bffPaths = Object.values(apiUrls.myGarage).filter((value) => typeof value === "string") as string[];

    expect(bffPaths.length).toBe(17);
    expect(bffPaths.every((value) => value.startsWith("/api/my-garage"))).toBe(true);
    expect(bffPaths.some((value) => value.includes("Defender.CarService"))).toBe(false);
    expect(bffPaths).toEqual(expect.arrayContaining([
      "/api/my-garage/vehicles",
      "/api/my-garage/vehicles/:vehicleId",
      "/api/my-garage/vehicles/:vehicleId/archive",
      "/api/my-garage/vehicles/:vehicleId/unarchive",
      "/api/my-garage/vehicles/:vehicleId/maintenance",
      "/api/my-garage/vehicles/:vehicleId/maintenance/:maintenanceId",
      "/api/my-garage/vehicles/:vehicleId/history",
      "/api/my-garage/vehicles/:vehicleId/history/:historyId",
      "/api/my-garage/vehicles/:vehicleId/insurance",
      "/api/my-garage/vehicles/:vehicleId/insurance/:insuranceId",
    ]));
  });

  test("allOperations_WhenExecuted_UseExactMethodsPathsQueriesBodiesAndSignals", async () => {
    const calls: Array<{ url: string; options: RequestInit }> = [];
    wrapper.mockImplementation(async (props) => {
      calls.push({ url: props.url, options: props.options });
      const response = props.options.method === "DELETE"
        ? new Response(null, { status: 204 })
        : new Response("{}", { status: 200 });
      await props.onSuccess?.(response);
    });

    const controller = new AbortController();
    const vehicleId = "vehicle/one";
    const maintenanceId = "maintenance one";
    const historyId = "history/one";
    const insuranceId = "insurance one";
    const vehicleRequest = {
      displayName: "Daily car",
      make: "Defender",
      model: "Mono",
      year: 2026,
      plate: "W6 TEST",
      vin: null,
    };
    const maintenanceRequest = {
      name: "Oil change",
      intervalMonths: 12,
      intervalThousandKm: null,
      lastDate: null,
      lastOdometerKm: null,
    };
    const historyRequest = {
      date: "2026-09-14",
      odometerKm: 123456,
      type: HistoryType.Maintenance,
      title: "Oil change",
      notes: null,
      linkedMaintenanceItemIds: [maintenanceId],
      costAmountMinor: 12500,
      costCurrency: Currency.PLN,
    };
    const insuranceRequest = {
      provider: "Defender Insurance",
      policyNumber: "POL-1",
      coverageType: "OC",
      startDate: "2026-01-01",
      endDate: "2026-12-31",
      notes: null,
    };

    await myGarageApi.getVehicles(true, null, controller.signal);
    await myGarageApi.createVehicle(vehicleRequest, null, controller.signal);
    await myGarageApi.getVehicle(vehicleId, null, controller.signal);
    await myGarageApi.updateVehicle(vehicleId, vehicleRequest, null, controller.signal);
    await myGarageApi.archiveVehicle(vehicleId, null, controller.signal);
    await myGarageApi.unarchiveVehicle(vehicleId, null, controller.signal);
    await myGarageApi.getMaintenanceItems(vehicleId, null, controller.signal);
    await myGarageApi.createMaintenanceItem(vehicleId, maintenanceRequest, null, controller.signal);
    await myGarageApi.updateMaintenanceItem(vehicleId, maintenanceId, maintenanceRequest, null, controller.signal);
    await myGarageApi.deleteMaintenanceItem(vehicleId, maintenanceId, null, controller.signal);
    await myGarageApi.getHistory(vehicleId, 2, 10, null, controller.signal);
    await myGarageApi.createHistory(vehicleId, historyRequest, null, controller.signal);
    await myGarageApi.updateHistory(vehicleId, historyId, historyRequest, null, controller.signal);
    await myGarageApi.deleteHistory(vehicleId, historyId, null, controller.signal);
    await myGarageApi.getInsurancePolicies(vehicleId, null, controller.signal);
    await myGarageApi.createInsurancePolicy(vehicleId, insuranceRequest, null, controller.signal);
    await myGarageApi.updateInsurancePolicy(vehicleId, insuranceId, insuranceRequest, null, controller.signal);

    expect(calls).toHaveLength(17);
    expect(calls.map(({ options }) => options.method)).toEqual([
      "GET", "POST", "GET", "PUT", "POST", "POST", "GET", "POST", "PUT", "DELETE",
      "GET", "POST", "PUT", "DELETE", "GET", "POST", "PUT",
    ]);
    expect(calls.map(({ url }) => url)).toEqual([
      "/api/my-garage/vehicles?includeArchived=true",
      "/api/my-garage/vehicles",
      "/api/my-garage/vehicles/vehicle%2Fone",
      "/api/my-garage/vehicles/vehicle%2Fone",
      "/api/my-garage/vehicles/vehicle%2Fone/archive",
      "/api/my-garage/vehicles/vehicle%2Fone/unarchive",
      "/api/my-garage/vehicles/vehicle%2Fone/maintenance",
      "/api/my-garage/vehicles/vehicle%2Fone/maintenance",
      "/api/my-garage/vehicles/vehicle%2Fone/maintenance/maintenance%20one",
      "/api/my-garage/vehicles/vehicle%2Fone/maintenance/maintenance%20one",
      "/api/my-garage/vehicles/vehicle%2Fone/history?page=2&pageSize=10",
      "/api/my-garage/vehicles/vehicle%2Fone/history",
      "/api/my-garage/vehicles/vehicle%2Fone/history/history%2Fone",
      "/api/my-garage/vehicles/vehicle%2Fone/history/history%2Fone",
      "/api/my-garage/vehicles/vehicle%2Fone/insurance",
      "/api/my-garage/vehicles/vehicle%2Fone/insurance",
      "/api/my-garage/vehicles/vehicle%2Fone/insurance/insurance%20one",
    ]);
    expect(calls.every(({ options }) => options.signal === controller.signal)).toBe(true);
    expect(JSON.parse(String(calls[1].options.body))).toEqual(vehicleRequest);
    expect(JSON.parse(String(calls[7].options.body))).toEqual(maintenanceRequest);
    expect(JSON.parse(String(calls[11].options.body))).toEqual(historyRequest);
    expect(JSON.parse(String(calls[15].options.body))).toEqual(insuranceRequest);
    expect(calls[9].options.body).toBeUndefined();
    expect(calls[13].options.body).toBeUndefined();
  });
});
