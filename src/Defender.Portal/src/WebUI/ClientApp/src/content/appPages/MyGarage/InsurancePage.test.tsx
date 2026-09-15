import { MemoryRouter, Route, Routes } from "react-router";
import { render, screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, test, vi } from "vitest";

import ThemeProvider from "src/theme/ThemeProvider";
import { InsuranceStatus } from "src/models/myGarage/CarModels";
import "src/localization/i18n";

import InsurancePage from "./Insurance";

const api = vi.hoisted(() => ({
  getVehicle: vi.fn(),
  getInsurancePolicies: vi.fn(),
  createInsurancePolicy: vi.fn(),
  updateInsurancePolicy: vi.fn(),
}));

vi.mock("src/api/myGarage", () => api);

describe("My Garage insurance page", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    api.getVehicle.mockResolvedValue({
      vehicle: { id: "vehicle-1", displayName: "Daily", make: "Ford", model: "Focus", year: 2020, plate: "ABC", vin: null, archived: false, currentOdometerKm: null, version: 1, createdAtUtc: "2026-01-01T00:00:00Z", updatedAtUtc: "2026-01-01T00:00:00Z" },
      maintenanceItems: [],
      insurancePolicies: [],
    });
    api.getInsurancePolicies.mockResolvedValue([
      { id: "policy-1", vehicleId: "vehicle-1", provider: "Safe Cover", policyNumber: "P-1", coverageType: "OC", startDate: "2026-01-01", endDate: "2026-12-31", notes: null, status: InsuranceStatus.Active },
    ]);
  });

  test("insurance_WhenLoaded_ShowsStatusAndNoDeleteControl", async () => {
    render(
      <ThemeProvider>
        <MemoryRouter initialEntries={["/my-garage/vehicles/vehicle-1/insurance"]}>
          <Routes>
            <Route path="/my-garage/vehicles/:vehicleId/insurance" element={<InsurancePage />} />
          </Routes>
        </MemoryRouter>
      </ThemeProvider>,
    );

    await waitFor(() => expect(screen.getByText("Safe Cover")).toBeTruthy());
    expect(screen.getAllByText(/Active/i).length).toBeGreaterThan(0);
    expect(screen.queryByRole("button", { name: /delete/i })).toBeNull();
  });
});
