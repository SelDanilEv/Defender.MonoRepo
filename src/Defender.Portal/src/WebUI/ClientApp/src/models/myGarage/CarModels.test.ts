import { Currency } from "src/models/shared/Currency";
import {
  HistoryType,
  InsuranceStatus,
  MaintenanceStatus,
  type MaintenanceItem,
  type ServiceHistoryRecord,
  type Vehicle,
  type VehiclePage,
  type VehicleSummary,
} from "src/models/myGarage/CarModels";

describe("My Garage model contracts", () => {
  test("enumValues_WhenSerialized_MatchTheBackendStringEnums", () => {
    expect(Object.values(HistoryType)).toEqual(["Maintenance", "Repair", "Tire", "Other"]);
    expect(Object.values(MaintenanceStatus)).toEqual(["Upcoming", "DueSoon", "Overdue", "NotStarted"]);
    expect(Object.values(InsuranceStatus)).toEqual(["Active", "ExpiringSoon", "Expired"]);
  });

  test("historyCost_WhenAbsent_PreservesNullableMinorUnitPair", () => {
    const history: ServiceHistoryRecord = {
      id: "history-1",
      vehicleId: "vehicle-1",
      date: "2026-09-14",
      odometerKm: 1000,
      type: HistoryType.Tire,
      title: "Tyre rotation",
      notes: null,
      linkedMaintenanceItemIds: [],
      costAmountMinor: null,
      costCurrency: null,
    };

    expect(history.date).toBe("2026-09-14");
    expect(history.costAmountMinor).toBeNull();
    expect(history.costCurrency).toBeNull();
    expect(Currency.PLN).toBe("PLN");
  });

  test("garageResponses_WhenNullableOdometersAreOmitted_AcceptOptionalFields", () => {
    const vehicle: Vehicle = {
      id: "vehicle-1",
      displayName: "Daily",
      make: "Ford",
      model: "Focus",
      year: 2020,
      plate: "ABC",
      vin: null,
      archived: false,
      version: 1,
      createdAtUtc: "2026-01-01T00:00:00Z",
      updatedAtUtc: "2026-01-01T00:00:00Z",
    };
    const maintenance: MaintenanceItem = {
      id: "maintenance-1",
      vehicleId: vehicle.id,
      name: "Oil",
      intervalMonths: 12,
      intervalThousandKm: null,
      lastDate: null,
      manualBaselineDate: null,
      nextDate: null,
      status: MaintenanceStatus.NotStarted,
      hasLinkedHistory: false,
    };

    expect(vehicle.currentOdometerKm).toBeUndefined();
    expect(maintenance.lastOdometerKm).toBeUndefined();
    expect(maintenance.manualBaselineOdometerKm).toBeUndefined();
    expect(maintenance.nextOdometerKm).toBeUndefined();
  });

  test("vehiclePage_WhenParsed_PreservesItemsAndPageMetadata", () => {
    const summary: VehicleSummary = {
      id: "vehicle-1",
      displayName: "Daily",
      make: "Ford",
      model: "Focus",
      year: 2020,
      plate: "ABC",
      vin: null,
      archived: false,
      maintenanceCounts: { overdue: 0, dueSoon: 0, upcoming: 0, notStarted: 0 },
      insuranceStatus: null,
    };
    const page: VehiclePage = {
      items: [summary],
      totalItemsCount: 1,
      currentPage: 0,
      pageSize: 25,
      totalPagesCount: 1,
    };

    expect(page.items).toHaveLength(1);
    expect(page.items[0].displayName).toBe("Daily");
    expect(page.totalItemsCount).toBe(1);
    expect(page.currentPage).toBe(0);
    expect(page.pageSize).toBe(25);
    expect(page.totalPagesCount).toBe(1);
  });
});
