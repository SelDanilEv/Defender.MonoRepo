import { expect, test, type Locator, type Page, type Route } from "@playwright/test";

const baseUrl = process.env.PLAYWRIGHT_BASE_URL?.trim();
const email = process.env.MY_GARAGE_E2E_EMAIL?.trim();
const password = process.env.MY_GARAGE_E2E_PASSWORD;
const allowUnavailableSkip = process.env.MY_GARAGE_E2E_ALLOW_SKIP === "1";

const runId = Date.now().toString(36);
const vehicleAName = "Task 10 garage A " + runId;
const vehicleAEditedName = vehicleAName + " edited";
const vehicleBName = "Task 10 garage B " + runId;
const vehicleBEditedName = vehicleBName + " edited";
const maintenanceAName = "Task 10 oil " + runId;
const maintenanceBName = "Task 10 tires " + runId;
const maintenanceDate = new Date().toISOString().slice(0, 10);

const createdVehicleIds: string[] = [];
let trackingEnabled = false;
let unexpectedApiRequests: string[] = [];
let directCarServiceRequests: string[] = [];
let myGarageRequests: string[] = [];

const localApiProblem = (code: string) => ({
  title: code,
  detail: code,
  code,
  traceId: "task-10-e2e",
});

const apiPath = (requestUrl: string) => new URL(requestUrl).pathname;
const isAllowedPortalInfrastructurePath = (path: string) =>
  path.startsWith("/api/home/") || path.startsWith("/api/authorization/");

const getVehicleIdFromUrl = (page: Page) => {
  const match = page.url().match(/\/my-garage\/vehicles\/([0-9a-f-]+)(?:\/|$)/i);
  if (!match) throw new Error("My Garage vehicle URL did not contain a vehicle id.");
  return match[1];
};

const selectOption = async (container: Locator, label: string, option: string) => {
  await container.getByRole("combobox", { name: label }).click();
  await container.getByRole("option", { name: new RegExp("^" + option + "\\s*$") }).click();
};

const selectLanguage = async (page: Page, language: "en" | "ru") => {
  const languageSelect = page.getByRole("combobox", { name: "Language" });
  await expect(languageSelect).toBeVisible();
  await languageSelect.click();
  await page.getByRole("option", { name: language === "en" ? /^EN\s*$/ : /^RU\s*$/ }).click();
};

const expectToast = async (page: Page, text: string) => {
  await expect(page.getByText(text, { exact: true })).toBeVisible({ timeout: 10_000 });
};

const openVehicles = async (page: Page) => {
  await page.goto("/my-garage/vehicles");
  await expect(page.getByRole("heading", { name: "My Garage", exact: true })).toBeVisible();
  await expect(page.locator('[role="progressbar"]')).toHaveCount(0);
};

const openMaintenance = async (page: Page, vehicleId: string) => {
  await page.goto("/my-garage/vehicles/" + vehicleId + "/maintenance");
  await expect(page.getByRole("heading", { name: "Add maintenance item", exact: true })).toBeVisible();
  await expect(page.locator('[role="progressbar"]')).toHaveCount(0);
};

const openHistory = async (page: Page, vehicleId: string) => {
  await page.goto("/my-garage/vehicles/" + vehicleId + "/history");
  await expect(page.getByRole("heading", { name: "Record service", exact: true })).toBeVisible();
  await expect(page.locator('[role="progressbar"]')).toHaveCount(0);
};

const openInsurance = async (page: Page, vehicleId: string) => {
  await page.goto("/my-garage/vehicles/" + vehicleId + "/insurance");
  await expect(page.getByRole("heading", { name: "Add insurance policy", exact: true })).toBeVisible();
  await expect(page.locator('[role="progressbar"]')).toHaveCount(0);
};

const createVehicle = async (page: Page, data: { displayName: string; make: string; model: string; year: string; plate: string }) => {
  await page.getByRole("button", { name: "Add vehicle", exact: true }).click();
  const dialog = page.getByRole("dialog");
  await dialog.getByRole("textbox", { name: "Display name" }).fill(data.displayName);
  await dialog.getByRole("textbox", { name: "Make" }).fill(data.make);
  await dialog.getByRole("textbox", { name: "Model" }).fill(data.model);
  await dialog.getByRole("spinbutton", { name: "Year" }).fill(data.year);
  await dialog.getByRole("textbox", { name: "Plate" }).fill(data.plate);
  await dialog.getByRole("button", { name: "Save", exact: true }).click();
  await expect(dialog).toBeHidden();
  await expectToast(page, "Vehicle added.");
};

const editVehicle = async (page: Page, oldName: string, newName: string) => {
  const row = page.getByRole("row").filter({ hasText: oldName }).last();
  await row.getByRole("button", { name: "Edit vehicle: " + oldName, exact: true }).click();
  const dialog = page.getByRole("dialog");
  await dialog.getByRole("textbox", { name: "Display name" }).fill(newName);
  await dialog.getByRole("button", { name: "Save", exact: true }).click();
  await expect(dialog).toBeHidden();
  await expectToast(page, "Vehicle updated.");
};

const openVehicle = async (page: Page, displayName: string) => {
  const row = page.getByRole("row").filter({ hasText: displayName }).last();
  await row.getByRole("button", { name: "Open vehicle: " + displayName, exact: true }).click();
  await expect(page).toHaveURL(/\/my-garage\/vehicles\/[0-9a-f-]+$/i);
  await expect(page.getByRole("heading", { name: displayName, exact: true })).toBeVisible();
  return getVehicleIdFromUrl(page);
};

const createMaintenance = async (page: Page, data: { name: string; months?: string; thousandKm?: string }) => {
  await page.getByRole("button", { name: "Add maintenance item", exact: true }).click();
  const dialog = page.getByRole("dialog");
  await dialog.getByRole("textbox", { name: "Name" }).fill(data.name);
  if (data.months) await dialog.getByRole("spinbutton", { name: "Interval, months" }).fill(data.months);
  if (data.thousandKm) await dialog.getByRole("spinbutton", { name: "Interval, thousand km" }).fill(data.thousandKm);
  await dialog.getByRole("button", { name: "Save", exact: true }).click();
  await expect(dialog).toBeHidden();
  await expectToast(page, "Maintenance item added.");
};

const inspectBlankMaintenanceBaseline = async (page: Page, name: string) => {
  const row = page.getByRole("row").filter({ hasText: name }).last();
  await row.getByRole("button", { name: "Edit maintenance item: " + name, exact: true }).click();
  const dialog = page.getByRole("dialog");
  await expect(dialog.locator('input[type="date"]')).toHaveValue("");
  await expect(dialog.getByRole("spinbutton", { name: "Last service odometer" })).toHaveValue("");
  await dialog.getByRole("button", { name: "Cancel", exact: true }).click();
};

const createHistory = async (
  page: Page,
  data: {
    type: "Maintenance" | "Repair" | "Tire";
    title: string;
    odometer: string;
    linkedMaintenance?: string[];
    cost?: string;
  },
) => {
  await page.getByRole("button", { name: "Record service", exact: true }).click();
  const dialog = page.getByRole("dialog");
  await dialog.locator('input[type="date"]').fill(maintenanceDate);
  await dialog.getByRole("spinbutton", { name: "Odometer" }).fill(data.odometer);
  await selectOption(dialog, "Type", data.type);
  await dialog.getByRole("textbox", { name: "Title" }).fill(data.title);

  for (const maintenanceName of data.linkedMaintenance ?? []) {
    await dialog.getByRole("checkbox", { name: maintenanceName, exact: true }).check();
  }

  if (data.cost) {
    await dialog.getByRole("spinbutton", { name: "Amount" }).fill(data.cost);
    await selectOption(dialog, "Currency", "PLN");
  }

  const requestPromise = page.waitForRequest((request) =>
    request.method() === "POST" && apiPath(request.url()).endsWith("/history"),
  );
  await dialog.getByRole("button", { name: "Save", exact: true }).click();
  const request = await requestPromise;
  const body = JSON.parse(request.postData() ?? "{}") as Record<string, unknown>;
  await expect(dialog).toBeHidden();
  await expectToast(page, "Service record added.");
  return body;
};

const createInsurance = async (
  page: Page,
  data: { provider: string; startDate: string; endDate: string },
) => {
  await page.getByRole("button", { name: "Add insurance policy", exact: true }).click();
  const dialog = page.getByRole("dialog");
  await dialog.getByRole("textbox", { name: "Provider" }).fill(data.provider);
  const dates = dialog.locator('input[type="date"]');
  await dates.nth(0).fill(data.startDate);
  await dates.nth(1).fill(data.endDate);
  await dialog.getByRole("button", { name: "Save", exact: true }).click();
  await expect(dialog).toBeHidden();
  await expectToast(page, "Insurance policy added.");
};

const fetchVehicleDetailThroughUi = async (page: Page, vehicleId: string) => {
  const responsePromise = page.waitForResponse((response) =>
    response.request().method() === "GET" &&
    apiPath(response.url()) === "/api/my-garage/vehicles/" + vehicleId,
  );
  await openMaintenance(page, vehicleId);
  const response = await responsePromise;
  expect(response.ok()).toBe(true);
  return await response.json() as {
    vehicle: { currentOdometerKm: number | null };
    maintenanceItems: Array<{
      name: string;
      lastDate: string | null;
      lastOdometerKm: number | null;
      status: string;
    }>;
  };
};

test.describe("My Garage local integration", () => {
  test.describe.configure({ mode: "serial" });
  test.setTimeout(180_000);

  test.beforeEach(async ({ page }, testInfo) => {
    createdVehicleIds.length = 0;
    trackingEnabled = false;
    unexpectedApiRequests = [];
    directCarServiceRequests = [];
    myGarageRequests = [];

    if (!baseUrl) {
      testInfo.skip(true, "Live run not requested. Set PLAYWRIGHT_BASE_URL plus local credentials.");
      return;
    }

    if (!email || !password) {
      throw new Error("Set MY_GARAGE_E2E_EMAIL and MY_GARAGE_E2E_PASSWORD for local My Garage e2e.");
    }

    try {
      const health = await page.request.get(new URL("/health", baseUrl).toString(), { timeout: 5_000 });
      if (!health.ok()) {
        if (allowUnavailableSkip) {
          testInfo.skip(true, "Local Portal health returned HTTP " + health.status() + ".");
          return;
        }
        throw new Error("Local Portal health returned HTTP " + health.status() + ".");
      }
    } catch (error) {
      if (allowUnavailableSkip) {
        testInfo.skip(true, "Local Portal health unavailable. " + String(error));
        return;
      }
      throw new Error("Local Portal health unavailable. Set MY_GARAGE_E2E_ALLOW_SKIP=1 only for an explicit unavailable-runtime skip.");
    }

    page.on("request", (request) => {
      if (!trackingEnabled) return;
      const path = apiPath(request.url());
      const url = new URL(request.url());
      if (!path.startsWith("/api/")) return;

      if (/47065|49065|internal-car|carservice/i.test(url.host + path)) {
        directCarServiceRequests.push(request.method() + " " + url.host + path);
      }

      if (path.startsWith("/api/my-garage")) {
        myGarageRequests.push(request.method() + " " + path);
      } else if (!isAllowedPortalInfrastructurePath(path)) {
        unexpectedApiRequests.push(request.method() + " " + path);
      }
    });

    await page.goto("/welcome/login");
    await expect(page.getByRole("textbox", { name: "Login" })).toBeVisible();
    await page.getByRole("textbox", { name: "Login" }).fill(email);
    await page.getByRole("textbox", { name: "Password" }).fill(password);
    await page.getByRole("button", { name: /sign in/i }).click();
    await expect(page).not.toHaveURL(/\/welcome\/login$/);
    trackingEnabled = true;
    await openVehicles(page);
    await selectLanguage(page, "en");
    await openVehicles(page);
  });

  test.afterEach(async ({ page }) => {
    if (!baseUrl) return;

    for (const vehicleId of createdVehicleIds) {
      try {
        const response = await page.request.post(new URL("/api/my-garage/vehicles/" + vehicleId + "/archive", baseUrl).toString());
        if (![200, 404, 409].includes(response.status())) {
          throw new Error("Cleanup archive returned HTTP " + response.status() + ".");
        }
      } catch {
        // Runtime cleanup is best effort. No secrets or response bodies are logged.
      }
    }
  });

  test("completes vehicle, maintenance, history, insurance, error, localization, and responsive journey", async ({ page }) => {
    await expect(page.getByText("No vehicles yet.", { exact: false })).toHaveCount(0);

    await page.getByRole("button", { name: "Add vehicle", exact: true }).click();
    const invalidVehicleDialog = page.getByRole("dialog");
    await invalidVehicleDialog.getByRole("button", { name: "Save", exact: true }).click();
    await expect(invalidVehicleDialog.getByRole("alert")).toContainText("This field is required.");
    await invalidVehicleDialog.getByRole("button", { name: "Cancel", exact: true }).click();

    await createVehicle(page, {
      displayName: vehicleAName,
      make: "TaskMake",
      model: "TaskModel A",
      year: "2020",
      plate: "TASK-A-" + runId,
    });
    await createVehicle(page, {
      displayName: vehicleBName,
      make: "TaskMake",
      model: "TaskModel B",
      year: "2021",
      plate: "TASK-B-" + runId,
    });

    await expect(page.getByRole("row").filter({ hasText: vehicleAName })).toBeVisible();
    await expect(page.getByRole("row").filter({ hasText: vehicleBName })).toBeVisible();

    await editVehicle(page, vehicleAName, vehicleAEditedName);
    const vehicleAId = await openVehicle(page, vehicleAEditedName);
    createdVehicleIds.push(vehicleAId);
    await page.goto("/my-garage/vehicles");
    await expect(page.getByRole("heading", { name: "My Garage", exact: true })).toBeVisible();

    await editVehicle(page, vehicleBName, vehicleBEditedName);
    const vehicleBId = await openVehicle(page, vehicleBEditedName);
    createdVehicleIds.push(vehicleBId);
    await page.goto("/my-garage/vehicles");
    await expect(page.getByRole("row").filter({ hasText: vehicleAEditedName })).toBeVisible();
    await expect(page.getByRole("row").filter({ hasText: vehicleBEditedName })).toBeVisible();

    const vehicleARow = page.getByRole("row").filter({ hasText: vehicleAEditedName }).last();
    await page.once("dialog", (dialog) => dialog.accept());
    await vehicleARow.getByRole("button", { name: "Archive vehicle: " + vehicleAEditedName, exact: true }).click();
    await expectToast(page, "Vehicle archived.");

    const includeArchived = page.getByRole("checkbox", { name: "Include archived vehicles" });
    await includeArchived.check();
    const archivedRow = page.getByRole("row").filter({ hasText: vehicleAEditedName }).last();
    await expect(archivedRow.getByText("Include archived vehicles", { exact: true })).toBeVisible();
    await page.once("dialog", (dialog) => dialog.accept());
    await archivedRow.getByRole("button", { name: "Restore vehicle: " + vehicleAEditedName, exact: true }).click();
    await expectToast(page, "Vehicle restored.");

    await openMaintenance(page, vehicleAId);
    await expect(page.getByText("No maintenance items yet.", { exact: true })).toBeVisible();
    await createMaintenance(page, { name: maintenanceAName, months: "12" });
    await createMaintenance(page, { name: maintenanceBName, thousandKm: "10" });
    await inspectBlankMaintenanceBaseline(page, maintenanceAName);
    await expect(page.getByRole("row").filter({ hasText: maintenanceAName }).getByText("Not started", { exact: true })).toBeVisible();
    await expect(page.getByRole("row").filter({ hasText: maintenanceBName }).getByText("Not started", { exact: true })).toBeVisible();

    await openHistory(page, vehicleAId);
    await expect(page.getByText("No service history yet.", { exact: true })).toBeVisible();
    const maintenanceBody = await createHistory(page, {
      type: "Maintenance",
      title: "Task 10 scheduled maintenance " + runId,
      odometer: "15000",
      linkedMaintenance: [maintenanceAName, maintenanceBName],
      cost: "123.45",
    });
    expect(maintenanceBody.costAmountMinor).toBe(12345);
    expect(maintenanceBody.costCurrency).toBe("PLN");
    expect(maintenanceBody.linkedMaintenanceItemIds).toHaveLength(2);

    await createHistory(page, {
      type: "Repair",
      title: "Task 10 repair " + runId,
      odometer: "18000",
    });
    await createHistory(page, {
      type: "Tire",
      title: "Task 10 tire service " + runId,
      odometer: "22000",
    });

    const maintenanceRow = page.getByRole("row").filter({ hasText: "Task 10 scheduled maintenance " + runId }).last();
    await expect(maintenanceRow.getByText(maintenanceAName, { exact: true })).toBeVisible();
    await expect(maintenanceRow.getByText(maintenanceBName, { exact: true })).toBeVisible();
    await expect(page.getByText("Repair", { exact: true })).toBeVisible();
    await expect(page.getByText("Tire", { exact: true })).toBeVisible();

    const detailAfterHistory = await fetchVehicleDetailThroughUi(page, vehicleAId);
    expect(detailAfterHistory.vehicle.currentOdometerKm).toBe(22000);
    expect(detailAfterHistory.maintenanceItems).toHaveLength(2);
    for (const item of detailAfterHistory.maintenanceItems) {
      expect(item.lastDate).toBe(maintenanceDate);
      expect(item.lastOdometerKm).toBe(15000);
      expect(item.status).toBe("Upcoming");
    }

    await openHistory(page, vehicleAId);
    const tireRow = page.getByRole("row").filter({ hasText: "Task 10 tire service " + runId }).last();
    await page.once("dialog", (dialog) => dialog.accept());
    await tireRow.getByRole("button", { name: "Delete history record: Task 10 tire service " + runId, exact: true }).click();
    await expectToast(page, "Service record deleted.");

    const detailAfterDelete = await fetchVehicleDetailThroughUi(page, vehicleAId);
    expect(detailAfterDelete.vehicle.currentOdometerKm).toBe(18000);

    await openInsurance(page, vehicleAId);
    await createInsurance(page, { provider: "Task 10 long cover " + runId, startDate: "2026-01-01", endDate: "2027-12-31" });
    await createInsurance(page, { provider: "Task 10 overlapping cover " + runId, startDate: "2026-06-01", endDate: "2026-12-31" });
    await createInsurance(page, { provider: "Task 10 expired cover " + runId, startDate: "2024-01-01", endDate: "2024-12-31" });
    await expect(page.getByRole("row").filter({ hasText: "Task 10 long cover " + runId }).getByText("Active", { exact: true })).toBeVisible();
    await expect(page.getByRole("row").filter({ hasText: "Task 10 overlapping cover " + runId }).getByText("Active", { exact: true })).toBeVisible();
    await expect(page.getByRole("row").filter({ hasText: "Task 10 expired cover " + runId }).getByText("Expired", { exact: true })).toBeVisible();
    await expect(page.getByRole("button", { name: /delete/i })).toHaveCount(0);

    await openVehicles(page);
    const conflictRow = page.getByRole("row").filter({ hasText: vehicleBEditedName }).last();
    await conflictRow.getByRole("button", { name: "Edit vehicle: " + vehicleBEditedName, exact: true }).click();
    const conflictDialog = page.getByRole("dialog");
    await conflictDialog.getByRole("textbox", { name: "Model" }).fill("Conflict attempt");
    const conflictPattern = new RegExp("/api/my-garage/vehicles/" + vehicleBId + "$");
    const conflictHandler = async (route: Route) => {
      if (route.request().method() === "PUT") {
        await new Promise<void>((resolve) => setTimeout(resolve, 250));
        await route.fulfill({ status: 409, contentType: "application/problem+json", body: JSON.stringify(localApiProblem("CAR_CONCURRENCY_CONFLICT")) });
        return;
      }
      await route.continue();
    };
    await page.route(conflictPattern, conflictHandler);
    const conflictSave = conflictDialog.getByRole("button", { name: "Save", exact: true });
    const conflictClick = conflictSave.click();
    await expect(conflictSave).toBeDisabled();
    await conflictClick;
    await expect(conflictDialog.getByRole("alert")).toContainText("record changed elsewhere");
    await page.unroute(conflictPattern, conflictHandler);
    await conflictDialog.getByRole("button", { name: "Cancel", exact: true }).click();

    const unavailablePattern = new RegExp("/api/my-garage/vehicles\\?includeArchived=false$");
    const unavailableHandler = async (route: Route) => {
      await route.fulfill({
        status: 503,
        contentType: "application/problem+json",
        body: JSON.stringify(localApiProblem("CAR_DATABASE_UNAVAILABLE")),
      });
    };
    await page.route(unavailablePattern, unavailableHandler);
    await page.reload();
    await expect(page.getByRole("alert")).toContainText("temporarily unavailable");
    await page.unroute(unavailablePattern, unavailableHandler);
    await openVehicles(page);

    await page.goto("/my-garage/vehicles/00000000-0000-0000-0000-000000000000");
    await expect(page.getByRole("alert")).toContainText("Vehicle was not found.");
    await openVehicles(page);

    await page.setViewportSize({ width: 390, height: 844 });
    await openHistory(page, vehicleAId);
    const mobileTableContainer = page.locator(".MuiTableContainer-root").first();
    const mobileTableMetrics = await mobileTableContainer.evaluate((element) => ({
      overflowX: getComputedStyle(element).overflowX,
      clientWidth: element.clientWidth,
      scrollWidth: element.scrollWidth,
    }));
    expect(mobileTableMetrics.overflowX).toBe("auto");
    expect(mobileTableMetrics.scrollWidth).toBeGreaterThanOrEqual(mobileTableMetrics.clientWidth);
    await page.setViewportSize({ width: 1280, height: 900 });

    await openVehicles(page);
    await expect(page.getByRole("heading", { name: "My Garage", exact: true })).toBeVisible();
    await selectLanguage(page, "ru");
    await expect(page.getByRole("heading", { name: "Мой гараж", exact: true })).toBeVisible();
    await selectLanguage(page, "en");
    await expect(page.getByRole("heading", { name: "My Garage", exact: true })).toBeVisible();

    const visibleMutationButtons = await page.locator("button[aria-label]").count();
    expect(visibleMutationButtons).toBeGreaterThan(0);
    expect(await page.locator("button").evaluateAll((buttons) =>
      buttons.filter((button) => !button.getAttribute("aria-label") && !button.textContent?.trim()).length,
    )).toBe(0);

    expect(myGarageRequests.length).toBeGreaterThan(0);
    expect(unexpectedApiRequests).toEqual([]);
    expect(directCarServiceRequests).toEqual([]);
  });
});
