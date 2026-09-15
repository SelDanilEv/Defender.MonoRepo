import { MemoryRouter, Route, Routes } from "react-router";
import { render, screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, test, vi } from "vitest";

import ThemeProvider from "src/theme/ThemeProvider";
import { Currency, HistoryType } from "src/models/myGarage/CarModels";
import "src/localization/i18n";

import HistoryPage from "./History";

const api = vi.hoisted(() => ({
  getVehicle: vi.fn(),
  getMaintenanceItems: vi.fn(),
  getHistory: vi.fn(),
  createHistory: vi.fn(),
  updateHistory: vi.fn(),
  deleteHistory: vi.fn(),
}));

vi.mock("src/api/myGarage", () => api);

describe("My Garage history page", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    api.getVehicle.mockResolvedValue({
      vehicle: { id: "vehicle-1", displayName: "Daily", make: "Ford", model: "Focus", year: 2020, plate: "ABC", vin: null, archived: false, currentOdometerKm: 10000, version: 1, createdAtUtc: "2026-01-01T00:00:00Z", updatedAtUtc: "2026-01-01T00:00:00Z" },
      maintenanceItems: [{ id: "maint-1", vehicleId: "vehicle-1", name: "Oil", intervalMonths: 12, intervalThousandKm: null, lastDate: null, lastOdometerKm: null, nextDate: null, nextOdometerKm: null, status: "NotStarted" }],
      insurancePolicies: [],
    });
    api.getMaintenanceItems.mockResolvedValue([{ id: "maint-1", vehicleId: "vehicle-1", name: "Oil", intervalMonths: 12, intervalThousandKm: null, lastDate: null, lastOdometerKm: null, nextDate: null, nextOdometerKm: null, status: "NotStarted" }]);
    api.getHistory.mockResolvedValue({
      items: [{ id: "history-1", vehicleId: "vehicle-1", date: "2026-01-02", odometerKm: 10000, type: HistoryType.Repair, title: "Brake repair", notes: null, linkedMaintenanceItemIds: ["maint-1"], costAmountMinor: 1234, costCurrency: Currency.PLN }],
      totalItemsCount: 1,
      currentPage: 0,
      pageSize: 25,
      totalPagesCount: 1,
    });
  });

  test("history_WhenLoaded_ShowsLinkedMaintenanceAndMinorCost", async () => {
    render(
      <ThemeProvider>
        <MemoryRouter initialEntries={["/my-garage/vehicles/vehicle-1/history"]}>
          <Routes>
            <Route path="/my-garage/vehicles/:vehicleId/history" element={<HistoryPage />} />
          </Routes>
        </MemoryRouter>
      </ThemeProvider>,
    );

    await waitFor(() => expect(screen.getByText("Brake repair")).toBeTruthy());
    expect(screen.getByText("Oil")).toBeTruthy();
    expect(screen.getByText(/12.34 zł/)).toBeTruthy();
  });
});
