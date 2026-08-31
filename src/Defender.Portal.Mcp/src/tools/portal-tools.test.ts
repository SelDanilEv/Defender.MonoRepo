import assert from "node:assert/strict";
import test from "node:test";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { PortalMcpScopes } from "../constants.js";
import {
  calendarOperationNames,
  buildPaginationQuery,
  getCalendarOperationPath,
  getRegularExpenseOperationPath,
  normalizeRegularExpenseMonth,
  registerPortalTools,
  regularExpenseMonthSchema,
  regularExpenseOperationNames,
  regularExpensePageSchema,
  regularExpensePageSizeSchema,
} from "./portal-tools.js";
import type { PortalBffClient } from "../services/portal-bff-client.js";

const expenseId = "00000000-0000-0000-0000-000000000001";
const reviewId = "00000000-0000-0000-0000-000000000002";

interface BffCall {
  path: string;
  init: RequestInit;
  accessToken: string;
}

interface ToolResult {
  content?: Array<{ type: string; text: string }>;
  isError?: boolean;
}

type RegisteredToolHandler = (input: Record<string, unknown>) => Promise<unknown>;

function createRegularExpenseToolHarness(
  scopes: readonly string[] = [PortalMcpScopes.BudgetWrite, PortalMcpScopes.BudgetDelete],
) {
  const calls: BffCall[] = [];
  let handler: RegisteredToolHandler | undefined;
  const fakeBff = {
    request: async (path: string, init: RequestInit, accessToken: string) => {
      calls.push({ path, init, accessToken });
      return { ok: true };
    },
    get: async () => ({ ok: true }),
  } as unknown as PortalBffClient;
  const fakeServer = {
    registerTool: (name: string, _config: unknown, callback: RegisteredToolHandler) => {
      if (name === "defender_portal_regular_expenses_mutate") handler = callback;
    },
  } as unknown as McpServer;

  registerPortalTools(fakeServer, {
    bff: fakeBff,
    bffToken: "test-bff-token",
    scopes: new Set(scopes),
  });

  assert.ok(handler, "regular expense mutation tool should be registered");
  return { calls, invoke: handler };
}

function resultOf(value: unknown): ToolResult {
  return value as ToolResult;
}

test("calendarOperationNames_WhenComparedToPortalController_CoversEveryMutationRoute", () => {
  assert.deepEqual(calendarOperationNames, [
    "update_theme", "create_queued_trip", "create_event_from_date", "create_event", "update_event", "delete_event", "auto_schedule_event", "add_point", "update_point", "delete_point", "add_participant", "delete_participant", "update_my_participation", "add_packing_item", "update_packing_item", "delete_packing_item",
  ]);
});

test("getCalendarOperationPath_WhenEventPointIsUpdated_UsesPortalControllerRoute", () => {
  const path = getCalendarOperationPath("update_point", {
    eventId: "00000000-0000-0000-0000-000000000001",
    pointId: "00000000-0000-0000-0000-000000000002",
  });

  assert.equal(path, "/api/travelcalendar/events/00000000-0000-0000-0000-000000000001/points/00000000-0000-0000-0000-000000000002");
});

test("getCalendarOperationPath_WhenDeleteParticipantLacksIdentifier_ReturnsUndefined", () => {
  assert.equal(getCalendarOperationPath("delete_participant", { eventId: "00000000-0000-0000-0000-000000000001" }), undefined);
});

test("regularExpenseOperationNames_WhenComparedToPortalController_CoversEveryMutationRoute", () => {
  assert.deepEqual(regularExpenseOperationNames, [
    "create_expense",
    "update_expense",
    "delete_expense",
    "save_review",
    "delete_review",
    "update_diagram_setup",
  ]);
});

test("getRegularExpenseOperationPath_WhenRepresentativeOperationsAreRequested_UsesExactBudgetTrackerRoutes", () => {
  const expenseId = "00000000-0000-0000-0000-000000000001";
  const reviewId = "00000000-0000-0000-0000-000000000002";

  assert.equal(
    getRegularExpenseOperationPath("create_expense", {}),
    "/api/BudgetTracker/regular-expenses/expense",
  );
  assert.equal(
    getRegularExpenseOperationPath("update_expense", {}),
    "/api/BudgetTracker/regular-expenses/expense",
  );
  assert.equal(
    getRegularExpenseOperationPath("delete_expense", { expenseId }),
    `/api/BudgetTracker/regular-expenses/expense/${expenseId}`,
  );
  assert.equal(
    getRegularExpenseOperationPath("save_review", {}),
    "/api/BudgetTracker/regular-expenses/review",
  );
  assert.equal(
    getRegularExpenseOperationPath("delete_review", { reviewId }),
    `/api/BudgetTracker/regular-expenses/review/${reviewId}`,
  );
  assert.equal(
    getRegularExpenseOperationPath("update_diagram_setup", {}),
    "/api/BudgetTracker/regular-expenses/diagram-setup",
  );
});

test("getRegularExpenseOperationPath_WhenDeleteIdentifierIsMissing_ReturnsUndefined", () => {
  assert.equal(getRegularExpenseOperationPath("delete_expense", {}), undefined);
  assert.equal(getRegularExpenseOperationPath("delete_review", {}), undefined);
});

test("regularExpenseMonthSchema_WhenMonthIsNotCalendarMonth_RejectsInput", () => {
  assert.equal(regularExpenseMonthSchema.safeParse("2026-08").success, true);
  assert.equal(regularExpenseMonthSchema.safeParse("2026-13").success, false);
  assert.equal(regularExpenseMonthSchema.safeParse("2026-08-01").success, false);
});

test("regularExpensePaginationSchemas_WhenBoundsAreOutsideContract_RejectInput", () => {
  assert.equal(regularExpensePageSchema.safeParse(0).success, true);
  assert.equal(regularExpensePageSchema.safeParse(-1).success, false);
  assert.equal(regularExpensePageSizeSchema.safeParse(100).success, true);
  assert.equal(regularExpensePageSizeSchema.safeParse(0).success, false);
  assert.equal(regularExpensePageSizeSchema.safeParse(101).success, false);
});

test("buildPaginationQuery_WhenValuesAreOmitted_UsesContractDefaults", () => {
  assert.equal(buildPaginationQuery(), "?page=0&pageSize=10");
});

test("normalizeRegularExpenseMonth_WhenMonthIsValid_AppendsFirstDay", () => {
  assert.equal(normalizeRegularExpenseMonth("2026-08"), "2026-08-01");
});

test("regularExpensesMutate_WhenUpdateBodyLacksId_ReturnsErrorWithoutCallingBff", async () => {
  const harness = createRegularExpenseToolHarness();

  const result = resultOf(await harness.invoke({
    operation: "update_expense",
    body: { name: "Rent" },
  }));

  assert.equal(result.isError, true);
  assert.match(result.content?.[0]?.text ?? "", /update_expense/i);
  assert.equal(harness.calls.length, 0);
});

test("regularExpensesMutate_WhenReviewBodyLacksMonth_ReturnsErrorWithoutCallingBff", async () => {
  const harness = createRegularExpenseToolHarness();

  const result = resultOf(await harness.invoke({
    operation: "save_review",
    body: { expenses: [{ regularExpenseId: expenseId, amount: 1250 }] },
  }));

  assert.equal(result.isError, true);
  assert.match(result.content?.[0]?.text ?? "", /save_review/i);
  assert.equal(harness.calls.length, 0);
});

test("regularExpensesMutate_WhenCreateBodyLacksRequiredFields_ReturnsErrorWithoutCallingBff", async () => {
  const harness = createRegularExpenseToolHarness();

  const result = resultOf(await harness.invoke({
    operation: "create_expense",
    body: { name: "Rent" },
  }));

  assert.equal(result.isError, true);
  assert.match(result.content?.[0]?.text ?? "", /create_expense/i);
  assert.equal(harness.calls.length, 0);
});

test("regularExpensesMutate_WhenDiagramBodyLacksMainCurrency_ReturnsErrorWithoutCallingBff", async () => {
  const harness = createRegularExpenseToolHarness();

  const result = resultOf(await harness.invoke({
    operation: "update_diagram_setup",
    body: { lastMonths: 12 },
  }));

  assert.equal(result.isError, true);
  assert.match(result.content?.[0]?.text ?? "", /update_diagram_setup/i);
  assert.equal(harness.calls.length, 0);
});

test("regularExpensesMutate_WhenReviewBodyIsValid_NormalizesMonthAndMapsBffRequest", async () => {
  const harness = createRegularExpenseToolHarness();
  const body = {
    id: reviewId,
    month: "2026-08",
    expenses: [{ regularExpenseId: expenseId, amount: 1250 }],
  };

  const result = resultOf(await harness.invoke({ operation: "save_review", body }));

  assert.equal(result.isError, undefined);
  assert.deepEqual(harness.calls, [{
    path: "/api/BudgetTracker/regular-expenses/review",
    init: {
      method: "POST",
      headers: { "content-type": "application/json" },
      body: JSON.stringify({
        id: reviewId,
        month: "2026-08-01",
        expenses: [{ regularExpenseId: expenseId, amount: 1250 }],
      }),
    },
    accessToken: "test-bff-token",
  }]);
});

test("regularExpensesMutate_WhenCreateBodyIsValid_MapsExactPortalRequest", async () => {
  const harness = createRegularExpenseToolHarness();
  const body = {
    name: "Rent",
    type: 1,
    currency: 4,
    defaultAmount: 1250,
    orderPriority: 2,
  };

  const result = resultOf(await harness.invoke({ operation: "create_expense", body }));

  assert.equal(result.isError, undefined);
  assert.equal(harness.calls.length, 1);
  assert.equal(harness.calls[0]?.path, "/api/BudgetTracker/regular-expenses/expense");
  assert.equal(harness.calls[0]?.init.method, "POST");
  assert.equal(harness.calls[0]?.init.body, JSON.stringify(body));
});

test("regularExpensesMutate_WhenUpdateBodyIsValid_MapsExactPortalRequest", async () => {
  const harness = createRegularExpenseToolHarness();
  const body = { id: expenseId, defaultAmount: 1250 };

  const result = resultOf(await harness.invoke({ operation: "update_expense", body }));

  assert.equal(result.isError, undefined);
  assert.equal(harness.calls.length, 1);
  assert.equal(harness.calls[0]?.path, "/api/BudgetTracker/regular-expenses/expense");
  assert.equal(harness.calls[0]?.init.method, "PUT");
  assert.equal(harness.calls[0]?.init.body, JSON.stringify(body));
});

test("regularExpensesMutate_WhenWriteScopeIsMissing_ReturnsErrorWithoutCallingBff", async () => {
  const harness = createRegularExpenseToolHarness([PortalMcpScopes.Read]);

  const result = resultOf(await harness.invoke({
    operation: "create_expense",
    body: { name: "Rent", type: 1, currency: 4, defaultAmount: 1250 },
  }));

  assert.equal(result.isError, true);
  assert.match(result.content?.[0]?.text ?? "", /mcp:budget:write/);
  assert.equal(harness.calls.length, 0);
});

test("regularExpensesMutate_WhenDeleteConfirmationIsMissing_ReturnsErrorWithoutCallingBff", async () => {
  const harness = createRegularExpenseToolHarness();

  const result = resultOf(await harness.invoke({
    operation: "delete_expense",
    expenseId,
    confirm: false,
  }));

  assert.equal(result.isError, true);
  assert.match(result.content?.[0]?.text ?? "", /confirm=true/);
  assert.equal(harness.calls.length, 0);
});

test("regularExpensesMutate_WhenReviewItemIsInvalid_ReturnsErrorWithoutCallingBff", async () => {
  const harness = createRegularExpenseToolHarness();

  const result = resultOf(await harness.invoke({
    operation: "save_review",
    body: {
      month: "2026-08",
      expenses: [{ regularExpenseId: "not-a-uuid", amount: -1 }],
    },
  }));

  assert.equal(result.isError, true);
  assert.match(result.content?.[0]?.text ?? "", /save_review/i);
  assert.equal(harness.calls.length, 0);
});

test("regularExpensesMutate_WhenDeleteBodyIsProvided_ReturnsErrorWithoutCallingBff", async () => {
  const harness = createRegularExpenseToolHarness();

  const result = resultOf(await harness.invoke({
    operation: "delete_expense",
    expenseId,
    confirm: true,
    body: {},
  }));

  assert.equal(result.isError, true);
  assert.match(result.content?.[0]?.text ?? "", /body/i);
  assert.equal(harness.calls.length, 0);
});

test("regularExpensesMutate_WhenDiagramEndMonthIsFullDate_NormalizesToFirstDay", async () => {
  const harness = createRegularExpenseToolHarness();

  const result = resultOf(await harness.invoke({
    operation: "update_diagram_setup",
    body: { mainCurrency: "PLN", lastMonths: 12, endMonth: "2026-08-17" },
  }));

  assert.equal(result.isError, undefined);
  assert.equal(harness.calls[0]?.init.body, JSON.stringify({
    mainCurrency: "PLN",
    lastMonths: 12,
    endMonth: "2026-08-01",
  }));
});

test("regularExpensesMutate_WhenUpdateHasNoMutableFields_ReturnsErrorWithoutCallingBff", async () => {
  const harness = createRegularExpenseToolHarness();

  const result = resultOf(await harness.invoke({
    operation: "update_expense",
    body: { id: expenseId },
  }));

  assert.equal(result.isError, true);
  assert.match(result.content?.[0]?.text ?? "", /update_expense/i);
  assert.equal(harness.calls.length, 0);
});
