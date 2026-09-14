import APICallWrapper from "src/api/APIWrapper/APICallWrapper";
import apiUrls from "src/api/apiUrls";
import {
  myGarageApi,
} from "src/api/myGarage";
import { HistoryType } from "src/models/myGarage/CarModels";

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
});
