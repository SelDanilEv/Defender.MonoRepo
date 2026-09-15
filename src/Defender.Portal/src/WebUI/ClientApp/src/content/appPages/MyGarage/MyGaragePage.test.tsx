import { MemoryRouter, Route, Routes } from "react-router";
import { render, screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, test, vi } from "vitest";

import ThemeProvider from "src/theme/ThemeProvider";
import "src/localization/i18n";

import MyGaragePage from "./index";

const api = vi.hoisted(() => ({
  getVehicles: vi.fn(),
  createVehicle: vi.fn(),
  updateVehicle: vi.fn(),
  archiveVehicle: vi.fn(),
  unarchiveVehicle: vi.fn(),
  getVehicle: vi.fn(),
  getMaintenanceItems: vi.fn(),
  getHistory: vi.fn(),
  getInsurancePolicies: vi.fn(),
}));

vi.mock("src/api/myGarage", () => api);

describe("My Garage page", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    api.getVehicles.mockResolvedValue([]);
  });

  test("vehicles_WhenEmpty_ShowsCreateFirstVehicleState", async () => {
    render(
      <ThemeProvider>
        <MemoryRouter initialEntries={["/my-garage/vehicles"]}>
          <Routes>
            <Route path="/my-garage/*" element={<MyGaragePage />} />
          </Routes>
        </MemoryRouter>
      </ThemeProvider>,
    );

    await waitFor(() => expect(screen.getByText(/No vehicles yet/i)).toBeTruthy());
    expect(screen.getByRole("button", { name: /Add vehicle/i })).toBeTruthy();
  });
});
