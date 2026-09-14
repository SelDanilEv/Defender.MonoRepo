import { act, fireEvent, render, screen, waitFor, within } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router";
import { beforeEach, describe, expect, test, vi } from "vitest";

import ThemeProvider from "src/theme/ThemeProvider";
import { Currency, HistoryType, InsuranceStatus, MaintenanceStatus } from "src/models/myGarage/CarModels";
import "src/localization/i18n";

import MaintenancePage from "./Maintenance";
import VehicleOverviewPage from "./VehicleOverview";
import VehiclesPage from "./Vehicles";
import HistoryPage from "./History";
import InsurancePage from "./Insurance";

const api = vi.hoisted(() => ({
  getVehicles: vi.fn(),
  createVehicle: vi.fn(),
  updateVehicle: vi.fn(),
  archiveVehicle: vi.fn(),
  unarchiveVehicle: vi.fn(),
  getVehicle: vi.fn(),
  getMaintenanceItems: vi.fn(),
  createMaintenanceItem: vi.fn(),
  updateMaintenanceItem: vi.fn(),
  deleteMaintenanceItem: vi.fn(),
  getHistory: vi.fn(),
  createHistory: vi.fn(),
  updateHistory: vi.fn(),
  deleteHistory: vi.fn(),
  getInsurancePolicies: vi.fn(),
  createInsurancePolicy: vi.fn(),
  updateInsurancePolicy: vi.fn(),
}));

vi.mock("src/api/myGarage", () => api);
vi.mock("src/appUtils", () => ({
  default: () => ({
    isMobile: true,
    isLargeScreen: false,
    searchParams: new URLSearchParams(),
    react: {},
  }),
}));

const vehicle = {
  id: "vehicle-1",
  displayName: "Daily",
  make: "Ford",
  model: "Focus",
  year: 2020,
  plate: "ABC",
  vin: null,
  archived: false,
  currentOdometerKm: 50_000,
  version: 1,
  createdAtUtc: "2026-01-01T00:00:00Z",
  updatedAtUtc: "2026-01-01T00:00:00Z",
};

const maintenance = {
  id: "maintenance-1",
  vehicleId: "vehicle-1",
  name: "Oil",
  intervalMonths: 12,
  intervalThousandKm: null,
  manualBaselineDate: "2025-01-01",
  manualBaselineOdometerKm: 40_000,
  lastDate: "2026-01-01",
  lastOdometerKm: 50_000,
  nextDate: "2027-01-01",
  nextOdometerKm: null,
  status: MaintenanceStatus.Upcoming,
  hasLinkedHistory: true,
};

const historyPage = {
  items: [{
    id: "history-1",
    vehicleId: "vehicle-1",
    date: "2026-01-02",
    odometerKm: 50_000,
    type: HistoryType.Repair,
    title: "Brake repair",
    notes: null,
    linkedMaintenanceItemIds: [maintenance.id],
    costAmountMinor: 1234,
    costCurrency: Currency.PLN,
  }],
  totalItemsCount: 1,
  currentPage: 0,
  pageSize: 25,
  totalPagesCount: 1,
};

const detail = {
  vehicle,
  maintenanceItems: [
    maintenance,
    { ...maintenance, id: "maintenance-2", name: "Brakes", status: MaintenanceStatus.Overdue, hasLinkedHistory: false },
    { ...maintenance, id: "maintenance-3", name: "Tires", status: MaintenanceStatus.DueSoon, hasLinkedHistory: false },
    { ...maintenance, id: "maintenance-4", name: "Inspection", status: MaintenanceStatus.NotStarted, hasLinkedHistory: false },
  ],
  insurancePolicies: [],
};

const renderRoute = (path: string, element: React.ReactNode) => render(
  <ThemeProvider>
    <MemoryRouter initialEntries={[path.replace(":vehicleId", "vehicle-1")]}>
      <Routes><Route path={path} element={element} /></Routes>
    </MemoryRouter>
  </ThemeProvider>,
);

describe("My Garage review round 1", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    api.getVehicle.mockResolvedValue(detail);
    api.getHistory.mockResolvedValue(historyPage);
    api.getVehicles.mockResolvedValue([{ ...vehicle, maintenanceCounts: { overdue: 1, dueSoon: 1, upcoming: 1, notStarted: 1 }, insuranceStatus: InsuranceStatus.Active }]);
    api.createVehicle.mockResolvedValue(vehicle);
    api.updateVehicle.mockResolvedValue(vehicle);
    api.archiveVehicle.mockResolvedValue(vehicle);
    api.unarchiveVehicle.mockResolvedValue(vehicle);
    api.createMaintenanceItem.mockResolvedValue(maintenance);
    api.updateMaintenanceItem.mockResolvedValue(maintenance);
    api.createHistory.mockResolvedValue(historyPage.items[0]);
    api.updateHistory.mockResolvedValue(historyPage.items[0]);
    api.deleteHistory.mockResolvedValue(undefined);
    api.getInsurancePolicies.mockResolvedValue([]);
    api.createInsurancePolicy.mockResolvedValue({ id: "policy-1" });
    api.updateInsurancePolicy.mockResolvedValue({ id: "policy-1" });
  });

  test("maintenance_WhenLinkedEditSaves_PreservesManualBaselineInsteadOfEffectiveHistoryBaseline", async () => {
    renderRoute("/my-garage/vehicles/:vehicleId/maintenance", <MaintenancePage />);

    await waitFor(() => expect(screen.getByText("Oil")).toBeTruthy());
    fireEvent.click(screen.getByRole("button", { name: /Edit maintenance item: Oil/i }));
    fireEvent.click(screen.getByRole("button", { name: "Save" }));

    await waitFor(() => expect(api.updateMaintenanceItem).toHaveBeenCalled());
    expect(api.updateMaintenanceItem.mock.calls[0][2]).toEqual(expect.objectContaining({
      manualBaselineDate: "2025-01-01",
      manualBaselineOdometerKm: 40_000,
    }));
    expect(api.updateMaintenanceItem.mock.calls[0][2]).not.toHaveProperty("lastDate");
  });

  test("maintenance_WhenServerReportsAllStatusesAndReference_ShowsStatusColumnAndDisablesDelete", async () => {
    renderRoute("/my-garage/vehicles/:vehicleId/maintenance", <MaintenancePage />);

    await waitFor(() => expect(screen.getByText("Oil")).toBeTruthy());
    expect(screen.getByText("Status")).toBeTruthy();
    expect(screen.getByText(/Not started/)).toBeTruthy();
    expect((screen.getByRole("button", { name: /Delete maintenance item: Oil/i }) as HTMLButtonElement).disabled).toBe(true);
  });

  test("maintenance_WhenCreatingWithoutBaseline_SendsBlankManualBaseline", async () => {
    api.createMaintenanceItem.mockResolvedValue(maintenance);
    api.deleteMaintenanceItem.mockResolvedValue(undefined);
    renderRoute("/my-garage/vehicles/:vehicleId/maintenance", <MaintenancePage />);

    await waitFor(() => expect(screen.getByText("Oil")).toBeTruthy());
    fireEvent.click(screen.getByRole("button", { name: "Add maintenance item" }));
    fireEvent.change(screen.getByRole("textbox", { name: "Name" }), { target: { value: "Filter" } });
    fireEvent.change(screen.getByRole("spinbutton", { name: "Interval, months" }), { target: { value: "12" } });
    fireEvent.click(screen.getByRole("button", { name: "Save" }));
    await waitFor(() => expect(api.createMaintenanceItem).toHaveBeenCalledWith("vehicle-1", expect.objectContaining({
      name: "Filter",
      intervalMonths: 12,
      manualBaselineDate: null,
      manualBaselineOdometerKm: null,
    }), null));
  });

  test("maintenance_WhenUnlinkedItemIsConfirmed_DeletesThroughBff", async () => {
    renderRoute("/my-garage/vehicles/:vehicleId/maintenance", <MaintenancePage />);

    await waitFor(() => expect(screen.getByText("Brakes")).toBeTruthy());
    fireEvent.click(screen.getByRole("button", { name: /Delete maintenance item: Brakes/i }));
    fireEvent.click(within(screen.getByRole("dialog")).getByRole("button", { name: "Delete maintenance item" }));
    await waitFor(() => expect(api.deleteMaintenanceItem).toHaveBeenCalledWith("vehicle-1", "maintenance-2", null));
  });

  test("overview_WhenLoaded_ShowsEveryServerMaintenanceStatusCount", async () => {
    renderRoute("/my-garage/vehicles/:vehicleId", <VehicleOverviewPage />);

    await waitFor(() => expect(screen.getByText("Daily")).toBeTruthy());
    expect(screen.getAllByText("Upcoming").length).toBeGreaterThan(0);
    expect(screen.getAllByText("Not started").length).toBeGreaterThan(0);
  });

  test("overview_WhenVehicleIsMissing_ShowsLocalizedNotFoundState", async () => {
    api.getVehicle.mockRejectedValue({ status: 404, code: "CAR_VEHICLE_NOT_FOUND" });
    api.getHistory.mockRejectedValue({ status: 404, code: "CAR_VEHICLE_NOT_FOUND" });
    renderRoute("/my-garage/vehicles/:vehicleId", <VehicleOverviewPage />);

    await waitFor(() => expect(screen.getByText("Vehicle was not found.")).toBeTruthy());
    expect(screen.getByRole("button", { name: "Retry" })).toBeTruthy();
  });

  test("vehicles_WhenLoaded_PreservesAllStatusCountsAndMobileVehicleDetails", async () => {
    renderRoute("/my-garage/vehicles", <VehiclesPage />);

    await waitFor(() => expect(screen.getByText("Daily")).toBeTruthy());
    expect(screen.getByText("Ford")).toBeTruthy();
    expect(screen.getByText("Focus")).toBeTruthy();
    expect(screen.getByText(/Not started: 1/)).toBeTruthy();
    expect(screen.getByText("Status")).toBeTruthy();
  });

  test("vehicles_WhenCreateEditArchiveActionsRun_UsesVehicleMutationEndpointsAndConfirmation", async () => {
    const confirm = vi.spyOn(window, "confirm").mockReturnValue(true);
    api.createVehicle.mockResolvedValue(vehicle);
    api.updateVehicle.mockResolvedValue(vehicle);
    api.archiveVehicle.mockResolvedValue({ ...vehicle, archived: true });
    renderRoute("/my-garage/vehicles", <VehiclesPage />);

    await waitFor(() => expect(screen.getByText("Daily")).toBeTruthy());
    fireEvent.click(screen.getByRole("button", { name: "Add vehicle" }));
    fireEvent.change(screen.getByRole("textbox", { name: "Display name" }), { target: { value: "Second" } });
    fireEvent.change(screen.getByRole("textbox", { name: "Make" }), { target: { value: "Ford" } });
    fireEvent.change(screen.getByRole("textbox", { name: "Model" }), { target: { value: "Focus" } });
    fireEvent.change(screen.getByRole("textbox", { name: "Plate" }), { target: { value: "XYZ" } });
    fireEvent.click(screen.getByRole("button", { name: "Save" }));
    await waitFor(() => expect(api.createVehicle).toHaveBeenCalledWith(expect.objectContaining({ displayName: "Second" }), null));
    await waitFor(() => expect(screen.queryByRole("dialog")).toBeNull());

    fireEvent.click(screen.getByRole("button", { name: "Edit vehicle: Daily" }));
    fireEvent.change(screen.getByRole("textbox", { name: "Display name" }), { target: { value: "Daily updated" } });
    fireEvent.click(screen.getByRole("button", { name: "Save" }));
    await waitFor(() => expect(api.updateVehicle).toHaveBeenCalledWith(vehicle.id, expect.objectContaining({ displayName: "Daily updated" }), null));
    await waitFor(() => expect(screen.queryByRole("dialog")).toBeNull());

    fireEvent.click(screen.getByRole("button", { name: "Archive vehicle: Daily" }));
    await waitFor(() => expect(api.archiveVehicle).toHaveBeenCalledWith(vehicle.id, null));
    expect(confirm).toHaveBeenCalledWith("Archive vehicle: Daily?");
    confirm.mockRestore();
  });

  test("vehicles_WhenUpdateIsPending_DisablesConflictingControls", async () => {
    let resolveUpdate: (value: typeof vehicle) => void = () => undefined;
    api.updateVehicle.mockReturnValue(new Promise<typeof vehicle>((resolve) => { resolveUpdate = resolve; }));
    renderRoute("/my-garage/vehicles", <VehiclesPage />);

    await waitFor(() => expect(screen.getByText("Daily")).toBeTruthy());
    const editButton = screen.getAllByRole("button").find((button) => button.getAttribute("aria-label") === "Edit vehicle: Daily");
    fireEvent.click(editButton!);
    fireEvent.click(screen.getByRole("button", { name: "Save" }));
    await waitFor(() => expect(api.updateVehicle).toHaveBeenCalled());
    expect((screen.getByRole("button", { name: "Save" }) as HTMLButtonElement).disabled).toBe(true);
    const underlyingEditButton = screen.getAllByRole("button", { hidden: true }).find((button) => button.getAttribute("aria-label") === "Edit vehicle: Daily");
    expect((underlyingEditButton as HTMLButtonElement).disabled).toBe(true);
    await act(async () => {
      resolveUpdate(vehicle);
      await Promise.resolve();
    });
  });

  test("history_WhenDialogOpens_UsesAccessibleLinkGroupAndRowsPerPageLabel", async () => {
    renderRoute("/my-garage/vehicles/:vehicleId/history", <HistoryPage />);

    await waitFor(() => expect(screen.getByText("Brake repair")).toBeTruthy());
    expect(screen.getByRole("combobox", { name: "Rows per page:" })).toBeTruthy();
    fireEvent.click(screen.getByRole("button", { name: /Record service/i }));
    expect(screen.getByRole("group", { name: "Linked maintenance" })).toBeTruthy();
    expect(screen.getByText("Select maintenance items linked to this service record.")).toBeTruthy();
  });

  test("history_WhenPageSizeChanges_RequestsTheSelectedPageSize", async () => {
    renderRoute("/my-garage/vehicles/:vehicleId/history", <HistoryPage />);

    await waitFor(() => expect(screen.getByText("Brake repair")).toBeTruthy());
    fireEvent.mouseDown(screen.getByRole("combobox", { name: "Rows per page:" }));
    fireEvent.click(await screen.findByRole("option", { name: "50" }));
    await waitFor(() => expect(api.getHistory).toHaveBeenCalledWith("vehicle-1", 0, 50, null, expect.anything()));
  });

  test("history_WhenUpdateConflicts_ShowsServerConflictAndReloads", async () => {
    api.updateHistory.mockRejectedValue({ status: 409, code: "CAR_CONCURRENCY_CONFLICT" });
    renderRoute("/my-garage/vehicles/:vehicleId/history", <HistoryPage />);

    await waitFor(() => expect(screen.getByText("Brake repair")).toBeTruthy());
    fireEvent.click(screen.getByRole("button", { name: "Edit history record: Brake repair" }));
    fireEvent.click(screen.getByRole("button", { name: "Save" }));
    await waitFor(() => expect(screen.getByText("The record changed elsewhere. Reload and try again.")).toBeTruthy());
    expect(api.getVehicle.mock.calls.length).toBeGreaterThan(1);
  });

  test("history_WhenUpdateIsPending_DisablesDialogControls", async () => {
    let resolveUpdate: (value: typeof historyPage.items[0]) => void = () => undefined;
    api.updateHistory.mockReturnValue(new Promise<typeof historyPage.items[0]>((resolve) => { resolveUpdate = resolve; }));
    renderRoute("/my-garage/vehicles/:vehicleId/history", <HistoryPage />);

    await waitFor(() => expect(screen.getByText("Brake repair")).toBeTruthy());
    fireEvent.click(screen.getByRole("button", { name: "Edit history record: Brake repair" }));
    fireEvent.click(screen.getByRole("button", { name: "Save" }));
    await waitFor(() => expect(api.updateHistory).toHaveBeenCalled());
    expect((screen.getByRole("button", { name: "Save" }) as HTMLButtonElement).disabled).toBe(true);
    expect((screen.getByRole("spinbutton", { name: "Odometer" }) as HTMLInputElement).disabled).toBe(true);
    await act(async () => {
      resolveUpdate(historyPage.items[0]);
      await Promise.resolve();
    });
  });

  test("insurance_WhenCreateRuns_UsesCreateEndpoint", async () => {
    const policy = {
      id: "policy-1",
      vehicleId: "vehicle-1",
      provider: "Safe Cover",
      policyNumber: "P-1",
      coverageType: "OC",
      startDate: "2026-01-01",
      endDate: "2026-12-31",
      notes: null,
      status: InsuranceStatus.Active,
    };
    api.getInsurancePolicies.mockResolvedValue([policy]);
    api.createInsurancePolicy.mockResolvedValue(policy);
    api.updateInsurancePolicy.mockResolvedValue(policy);
    renderRoute("/my-garage/vehicles/:vehicleId/insurance", <InsurancePage />);

    await waitFor(() => expect(screen.getByText("Safe Cover")).toBeTruthy());
    fireEvent.click(screen.getByRole("button", { name: "Add insurance policy" }));
    fireEvent.change(screen.getByRole("textbox", { name: "Provider" }), { target: { value: "New Cover" } });
    const insuranceDateInputs = screen.getByRole("dialog").querySelectorAll('input[type="date"]');
    fireEvent.change(insuranceDateInputs[0], { target: { value: "2026-01-01" } });
    fireEvent.change(insuranceDateInputs[1], { target: { value: "2026-12-31" } });
    fireEvent.click(screen.getByRole("button", { name: "Save" }));
    await waitFor(() => expect(api.createInsurancePolicy).toHaveBeenCalledWith("vehicle-1", expect.objectContaining({ provider: "New Cover" }), null));
  });

  test("insurance_WhenExistingPolicyIsEdited_UsesUpdateEndpoint", async () => {
    const policy = {
      id: "policy-1",
      vehicleId: "vehicle-1",
      provider: "Safe Cover",
      policyNumber: "P-1",
      coverageType: "OC",
      startDate: "2026-01-01",
      endDate: "2026-12-31",
      notes: null,
      status: InsuranceStatus.Active,
    };
    api.getInsurancePolicies.mockResolvedValue([policy]);
    api.updateInsurancePolicy.mockResolvedValue(policy);
    renderRoute("/my-garage/vehicles/:vehicleId/insurance", <InsurancePage />);

    await waitFor(() => expect(screen.getByText("Safe Cover")).toBeTruthy());
    const editButton = screen.getAllByRole("button").find((button) => button.getAttribute("aria-label") === "Edit insurance policy: Safe Cover");
    fireEvent.click(editButton!);
    fireEvent.change(screen.getByRole("textbox", { name: "Provider" }), { target: { value: "Updated Cover" } });
    fireEvent.click(screen.getByRole("button", { name: "Save" }));
    await waitFor(() => expect(api.updateInsurancePolicy).toHaveBeenCalledWith("vehicle-1", "policy-1", expect.objectContaining({ provider: "Updated Cover" }), null));
  });

  test("insurance_WhenUpdateConflicts_ShowsLocalizedConflict", async () => {
    const policy = {
      id: "policy-1",
      vehicleId: "vehicle-1",
      provider: "Safe Cover",
      policyNumber: null,
      coverageType: null,
      startDate: "2026-01-01",
      endDate: "2026-12-31",
      notes: null,
      status: InsuranceStatus.Active,
    };
    api.getInsurancePolicies.mockResolvedValue([policy]);
    api.updateInsurancePolicy.mockRejectedValue({ status: 409, code: "CAR_CONCURRENCY_CONFLICT" });
    renderRoute("/my-garage/vehicles/:vehicleId/insurance", <InsurancePage />);

    await waitFor(() => expect(screen.getByText("Safe Cover")).toBeTruthy());
    fireEvent.click(screen.getByRole("button", { name: "Edit insurance policy: Safe Cover" }));
    fireEvent.click(screen.getByRole("button", { name: "Save" }));
    await waitFor(() => expect(screen.getByText("The record changed elsewhere. Reload and try again.")).toBeTruthy());
  });
});
