import { Currency } from "src/models/shared/Currency";
import {
  HistoryType,
  InsuranceStatus,
  MaintenanceStatus,
  type ServiceHistoryRecord,
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
});
