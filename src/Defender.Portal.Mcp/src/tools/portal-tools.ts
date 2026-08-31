import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { z } from "zod";
import { PortalMcpScopes, PortalReadResources, type PortalReadResource } from "../constants.js";
import { PortalBffClient } from "../services/portal-bff-client.js";
import { errorResult, executePortalTool } from "../tool-results.js";

export interface PortalToolContext {
  bff: PortalBffClient;
  bffToken: string;
  scopes: Set<string>;
}

export function registerPortalTools(server: McpServer, context: PortalToolContext): void {
  registerCalendarTools(server, context);
  registerRegularExpenseTools(server, context);
  registerPortalReadTool(server, context);
}

function registerCalendarTools(server: McpServer, context: PortalToolContext): void {
  server.registerTool("defender_portal_calendar_get", {
    title: "Get Defender Portal travel calendar",
    description: "Read complete personal Travel Calendar through Defender Portal BFF. Optional dates use ISO-8601 YYYY-MM-DD boundaries.",
    inputSchema: {
      from: isoDate.optional().describe("Inclusive calendar start date."),
      to: isoDate.optional().describe("Inclusive calendar end date."),
    },
    outputSchema: { data: z.unknown() },
    annotations: readOnlyAnnotations,
  }, async ({ from, to }) => {
    const denied = requireScope(context, PortalMcpScopes.Read);
    if (denied) return denied;

    const query = new URLSearchParams();
    if (from) query.set("from", from);
    if (to) query.set("to", to);
    return executePortalTool(() => context.bff.get(`/api/travelcalendar${query.size ? `?${query}` : ""}`, context.bffToken));
  });

  server.registerTool("defender_portal_calendar_search_users", {
    title: "Search Defender Portal users for calendar participants",
    description: "Find up to ten Portal users by nickname or email before adding a Travel Calendar participant.",
    inputSchema: { query: z.string().trim().min(1).max(200).describe("Nickname or email fragment.") },
    outputSchema: { data: z.unknown() },
    annotations: readOnlyAnnotations,
  }, async ({ query }) => {
    const denied = requireScope(context, PortalMcpScopes.Read);
    if (denied) return denied;
    return executePortalTool(() => context.bff.get(`/api/travelcalendar/users?query=${encodeURIComponent(query)}`, context.bffToken));
  });

  server.registerTool("defender_portal_calendar_mutate", {
    title: "Mutate Defender Portal travel calendar",
    description: "Perform supported Travel Calendar mutation through Portal BFF. Operation names mirror Portal TravelCalendarController routes. Delete operations require confirm=true.",
    inputSchema: {
      operation: z.enum(calendarOperationNames).describe("Portal Travel Calendar operation."),
      eventId: guid.optional().describe("Required by event and event-point operations."),
      itemId: guid.optional().describe("Required by packing-item operations."),
      pointId: guid.optional().describe("Required by point operations."),
      participantUserId: guid.optional().describe("Required when removing a participant."),
      body: z.record(z.string(), z.unknown()).describe("Exact request body accepted by the corresponding Portal controller route."),
      confirm: z.boolean().optional().describe("Must be true for delete operations."),
    },
    outputSchema: { data: z.unknown() },
    annotations: {
      readOnlyHint: false,
      destructiveHint: true,
      idempotentHint: false,
      openWorldHint: false,
    },
  }, async (input) => {
    const operation = calendarOperations[input.operation];
    const denied = requireScope(context, operation.delete ? PortalMcpScopes.CalendarDelete : PortalMcpScopes.CalendarWrite);
    if (denied) return denied;
    if (operation.delete && input.confirm !== true) return errorResult("Deletion requires confirm=true.");

    const path = getCalendarOperationPath(input.operation, input);
    if (!path) return errorResult("Required resource identifier is missing for this Portal calendar operation.");

    return executePortalTool(() => context.bff.request(path, {
      method: operation.method,
      headers: { "content-type": "application/json" },
      body: JSON.stringify(input.body),
    }, context.bffToken));
  });
}

function registerRegularExpenseTools(server: McpServer, context: PortalToolContext): void {
  server.registerTool("defender_portal_regular_expenses_list", {
    title: "List Defender Portal regular expenses",
    description: "Read paged recurring expense definitions through the Defender Portal BFF. Page is zero-based; pageSize must be between 1 and 100.",
    inputSchema: {
      page: regularExpensePageSchema.describe("Zero-based page index, default 0."),
      pageSize: regularExpensePageSizeSchema.describe("Number of expenses per page, from 1 through 100; default 10."),
    },
    outputSchema: { data: z.unknown() },
    annotations: readOnlyAnnotations,
  }, async ({ page, pageSize }) => {
    const denied = requireScope(context, PortalMcpScopes.Read);
    if (denied) return denied;

    return executePortalTool(() => context.bff.get(
      `/api/BudgetTracker/regular-expenses/expenses${buildPaginationQuery(page, pageSize)}`,
      context.bffToken,
    ));
  });

  server.registerTool("defender_portal_regular_expenses_reviews_list", {
    title: "List Defender Portal regular expense reviews",
    description: "Read paged monthly regular expense reviews through the Defender Portal BFF. Page is zero-based; pageSize must be between 1 and 100.",
    inputSchema: {
      page: regularExpensePageSchema.describe("Zero-based page index, default 0."),
      pageSize: regularExpensePageSizeSchema.describe("Number of reviews per page, from 1 through 100; default 10."),
    },
    outputSchema: { data: z.unknown() },
    annotations: readOnlyAnnotations,
  }, async ({ page, pageSize }) => {
    const denied = requireScope(context, PortalMcpScopes.Read);
    if (denied) return denied;

    return executePortalTool(() => context.bff.get(
      `/api/BudgetTracker/regular-expenses/reviews${buildPaginationQuery(page, pageSize)}`,
      context.bffToken,
    ));
  });

  server.registerTool("defender_portal_regular_expenses_review_template", {
    title: "Get Defender Portal regular expense review template",
    description: "Read a regular expense review template through the Defender Portal BFF. Optional month uses ISO YYYY-MM and is sent to Portal as YYYY-MM-01.",
    inputSchema: {
      month: regularExpenseMonthSchema.optional().describe("Optional calendar month in YYYY-MM format."),
    },
    outputSchema: { data: z.unknown() },
    annotations: readOnlyAnnotations,
  }, async ({ month }) => {
    const denied = requireScope(context, PortalMcpScopes.Read);
    if (denied) return denied;

    const query = new URLSearchParams();
    if (month) query.set("month", normalizeRegularExpenseMonth(month));
    return executePortalTool(() => context.bff.get(
      `/api/BudgetTracker/regular-expenses/review/template${query.size ? `?${query}` : ""}`,
      context.bffToken,
    ));
  });

  server.registerTool("defender_portal_regular_expenses_reviews_by_date_range", {
    title: "List Defender Portal regular expense reviews by date range",
    description: "Read regular expense reviews between inclusive calendar months through the Defender Portal BFF. Months use ISO YYYY-MM and are sent to Portal as YYYY-MM-01.",
    inputSchema: {
      startMonth: regularExpenseMonthSchema.describe("Inclusive start month in YYYY-MM format."),
      endMonth: regularExpenseMonthSchema.describe("Inclusive end month in YYYY-MM format."),
    },
    outputSchema: { data: z.unknown() },
    annotations: readOnlyAnnotations,
  }, async ({ startMonth, endMonth }) => {
    const denied = requireScope(context, PortalMcpScopes.Read);
    if (denied) return denied;
    if (startMonth > endMonth) return errorResult("startMonth must be earlier than or equal to endMonth.");

    const query = new URLSearchParams({
      startMonth: normalizeRegularExpenseMonth(startMonth),
      endMonth: normalizeRegularExpenseMonth(endMonth),
    });
    return executePortalTool(() => context.bff.get(
      `/api/BudgetTracker/regular-expenses/reviews/by-date-range?${query}`,
      context.bffToken,
    ));
  });

  server.registerTool("defender_portal_regular_expenses_diagram_get", {
    title: "Get Defender Portal regular expense diagram setup",
    description: "Read regular expense diagram setup through the Defender Portal BFF. DateOnly endMonth values in the response are serialized as YYYY-MM-DD.",
    outputSchema: { data: z.unknown() },
    annotations: readOnlyAnnotations,
  }, async () => {
    const denied = requireScope(context, PortalMcpScopes.Read);
    if (denied) return denied;
    return executePortalTool(() => context.bff.get(
      "/api/BudgetTracker/regular-expenses/diagram-setup",
      context.bffToken,
    ));
  });

  server.registerTool("defender_portal_regular_expenses_mutate", {
    title: "Mutate Defender Portal regular expenses",
    description: "Create, update, or delete regular expenses; save or delete reviews; or update diagram setup through the Defender Portal BFF. All mutations require mcp:budget:write. Delete operations additionally require mcp:budget:delete and confirm=true.",
    inputSchema: {
      operation: z.enum(regularExpenseOperationNames).describe("Supported regular expense operation."),
      expenseId: guid.optional().describe("Required by delete_expense."),
      reviewId: guid.optional().describe("Required by delete_review."),
      body: z.unknown().optional().describe("Operation-specific JSON object. Required for create_expense, update_expense, save_review, and update_diagram_setup."),
      confirm: z.boolean().optional().describe("Must be true for delete_expense and delete_review."),
    },
    outputSchema: { data: z.unknown() },
    annotations: {
      readOnlyHint: false,
      destructiveHint: true,
      idempotentHint: false,
      openWorldHint: false,
    },
  }, async (input) => {
    const operation = regularExpenseOperations[input.operation];
    const writeDenied = requireScope(context, PortalMcpScopes.BudgetWrite);
    if (writeDenied) return writeDenied;
    if (operation.delete) {
      const deleteDenied = requireScope(context, PortalMcpScopes.BudgetDelete);
      if (deleteDenied) return deleteDenied;
      if (input.confirm !== true) return errorResult("Deletion requires confirm=true.");
    }

    const path = getRegularExpenseOperationPath(input.operation, input);
    if (!path) return errorResult("Required resource identifier is missing for this regular expense operation.");

    const body = prepareRegularExpenseBody(input.operation, input.body);
    if (body === null) {
      return errorResult(`Invalid request body for regular expense operation '${input.operation}'.`);
    }

    return executePortalTool(() => context.bff.request(path, {
      method: operation.method,
      ...(body === undefined
        ? {}
        : {
            headers: { "content-type": "application/json" },
            body: JSON.stringify(body),
          }),
    }, context.bffToken));
  });
}

function registerPortalReadTool(server: McpServer, context: PortalToolContext): void {
  server.registerTool("defender_portal_read", {
    title: "Read Defender Portal wallet and lottery data",
    description: "Read-only Portal BFF access. Supported resources mirror current Portal BankingController and LotteryController routes. No money movement or ticket purchase is exposed.",
    inputSchema: {
      resource: z.enum(Object.keys(PortalReadResources) as [PortalReadResource, ...PortalReadResource[]]).describe("Portal BFF read resource."),
      query: z.record(z.string().min(1).max(100), z.string().max(200)).optional().describe("Optional Portal query parameters."),
    },
    outputSchema: { data: z.unknown() },
    annotations: readOnlyAnnotations,
  }, async ({ resource, query }) => {
    const denied = requireScope(context, PortalMcpScopes.Read);
    if (denied) return denied;

    const parameters = new URLSearchParams(query ?? {});
    const path = PortalReadResources[resource];
    return executePortalTool(() => context.bff.get(`${path}${parameters.size ? `?${parameters}` : ""}`, context.bffToken));
  });
}

function requireScope(context: PortalToolContext, scope: string) {
  return context.scopes.has(scope) ? undefined : errorResult(`Missing required Portal OAuth scope: ${scope}.`);
}

interface CalendarMutationInput {
  eventId?: string;
  itemId?: string;
  pointId?: string;
  participantUserId?: string;
}

interface CalendarOperation {
  method: "POST" | "PUT" | "PATCH" | "DELETE";
  delete?: boolean;
  path: (input: CalendarMutationInput) => string | undefined;
}

interface RegularExpenseMutationInput {
  expenseId?: string;
  reviewId?: string;
  body?: unknown;
}

interface RegularExpenseOperation {
  method: "POST" | "PUT" | "DELETE";
  delete?: boolean;
  requiresBody?: boolean;
  path: (input: RegularExpenseMutationInput) => string | undefined;
}

export const calendarOperationNames = [
  "update_theme", "create_queued_trip", "create_event_from_date", "create_event", "update_event", "delete_event", "auto_schedule_event", "add_point", "update_point", "delete_point", "add_participant", "delete_participant", "update_my_participation", "add_packing_item", "update_packing_item", "delete_packing_item",
] as const;

const calendarOperations: Record<(typeof calendarOperationNames)[number], CalendarOperation> = {
  update_theme: { method: "PATCH", path: () => "/api/travelcalendar/theme" },
  create_queued_trip: { method: "POST", path: () => "/api/travelcalendar/queued-trips" },
  create_event_from_date: { method: "POST", path: () => "/api/travelcalendar/events/from-date" },
  create_event: { method: "POST", path: () => "/api/travelcalendar/events" },
  update_event: { method: "PUT", path: ({ eventId }) => eventId && `/api/travelcalendar/events/${eventId}` },
  delete_event: { method: "DELETE", delete: true, path: ({ eventId }) => eventId && `/api/travelcalendar/events/${eventId}` },
  auto_schedule_event: { method: "POST", path: ({ eventId }) => eventId && `/api/travelcalendar/events/${eventId}/auto-schedule` },
  add_point: { method: "POST", path: ({ eventId }) => eventId && `/api/travelcalendar/events/${eventId}/points` },
  update_point: { method: "PATCH", path: ({ eventId, pointId }) => eventId && pointId && `/api/travelcalendar/events/${eventId}/points/${pointId}` },
  delete_point: { method: "DELETE", delete: true, path: ({ eventId, pointId }) => eventId && pointId && `/api/travelcalendar/events/${eventId}/points/${pointId}` },
  add_participant: { method: "POST", path: ({ eventId }) => eventId && `/api/travelcalendar/events/${eventId}/participants` },
  delete_participant: { method: "DELETE", delete: true, path: ({ eventId, participantUserId }) => eventId && participantUserId && `/api/travelcalendar/events/${eventId}/participants/${participantUserId}` },
  update_my_participation: { method: "PATCH", path: ({ eventId }) => eventId && `/api/travelcalendar/events/${eventId}/my-participation` },
  add_packing_item: { method: "POST", path: () => "/api/travelcalendar/packing-items" },
  update_packing_item: { method: "PATCH", path: ({ itemId }) => itemId && `/api/travelcalendar/packing-items/${itemId}` },
  delete_packing_item: { method: "DELETE", delete: true, path: ({ itemId }) => itemId && `/api/travelcalendar/packing-items/${itemId}` },
};

export function getCalendarOperationPath(
  operationName: (typeof calendarOperationNames)[number],
  input: CalendarMutationInput,
): string | undefined {
  return calendarOperations[operationName].path(input);
}

export const regularExpenseOperationNames = [
  "create_expense",
  "update_expense",
  "delete_expense",
  "save_review",
  "delete_review",
  "update_diagram_setup",
] as const;

const regularExpenseOperations: Record<(typeof regularExpenseOperationNames)[number], RegularExpenseOperation> = {
  create_expense: {
    method: "POST",
    requiresBody: true,
    path: () => "/api/BudgetTracker/regular-expenses/expense",
  },
  update_expense: {
    method: "PUT",
    requiresBody: true,
    path: () => "/api/BudgetTracker/regular-expenses/expense",
  },
  delete_expense: {
    method: "DELETE",
    delete: true,
    path: ({ expenseId }) => toRegularExpenseResourcePath(
      "/api/BudgetTracker/regular-expenses/expense",
      expenseId,
    ),
  },
  save_review: {
    method: "POST",
    requiresBody: true,
    path: () => "/api/BudgetTracker/regular-expenses/review",
  },
  delete_review: {
    method: "DELETE",
    delete: true,
    path: ({ reviewId }) => toRegularExpenseResourcePath(
      "/api/BudgetTracker/regular-expenses/review",
      reviewId,
    ),
  },
  update_diagram_setup: {
    method: "POST",
    requiresBody: true,
    path: () => "/api/BudgetTracker/regular-expenses/diagram-setup",
  },
};

export function getRegularExpenseOperationPath(
  operationName: (typeof regularExpenseOperationNames)[number],
  input: RegularExpenseMutationInput,
): string | undefined {
  return regularExpenseOperations[operationName]?.path(input);
}

export function buildPaginationQuery(page?: number, pageSize?: number): string {
  const query = new URLSearchParams({
    page: String(page ?? 0),
    pageSize: String(pageSize ?? 10),
  });
  return `?${query}`;
}

export function normalizeRegularExpenseMonth(month: string): string {
  return `${month}-01`;
}

function prepareRegularExpenseBody(
  operationName: (typeof regularExpenseOperationNames)[number],
  body: unknown,
): Record<string, unknown> | undefined | null {
  if (!regularExpenseOperations[operationName].requiresBody) {
    return body === undefined ? undefined : null;
  }

  const schema = regularExpenseBodySchemas[operationName as keyof typeof regularExpenseBodySchemas];
  if (!schema) return null;

  const parsed = schema.safeParse(body);
  return parsed.success ? parsed.data : null;
}

function toRegularExpenseResourcePath(basePath: string, id: string | undefined): string | undefined {
  return id && guid.safeParse(id).success ? `${basePath}/${id}` : undefined;
}

const isoDate = z.string().regex(/^\d{4}-\d{2}-\d{2}$/, "Use ISO-8601 date YYYY-MM-DD.");
const guid = z.string().regex(
  /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i,
  "Use a UUID identifier.",
);
export const regularExpenseMonthSchema = z.string()
  .regex(/^\d{4}-(?:0[1-9]|1[0-2])$/, "Use calendar month YYYY-MM.")
  .refine((value) => value.slice(0, 4) !== "0000", "Use a year from 0001 through 9999.");
export const regularExpensePageSchema = z.number().int().min(0).default(0);
export const regularExpensePageSizeSchema = z.number().int().min(1).max(100).default(10);

const regularExpenseTypeSchema = z.union([
  z.literal(1),
  z.literal(2),
  z.literal(3),
  z.enum(["Regular", "Subscription", "Annual"]),
]);
const currencySchema = z.union([
  z.literal(1),
  z.literal(2),
  z.literal(3),
  z.literal(4),
  z.literal(5),
  z.literal(6),
  z.enum(["USD", "EUR", "GEL", "PLN", "RUB", "BYN"]),
]);
const regularExpenseAmountSchema = z.number().int().min(0);
const regularExpenseNameSchema = z.string().trim().min(1).max(200);
const regularExpenseDateOnlySchema = z.string()
  .regex(/^\d{4}-(?:0[1-9]|1[0-2])-(?:0[1-9]|[12]\d|3[01])$/, "Use ISO date YYYY-MM-DD.")
  .refine(isValidDateOnly, "Use a valid calendar date.");
const regularExpenseMonthOrDateSchema = z.union([
  regularExpenseMonthSchema,
  regularExpenseDateOnlySchema,
]);
const regularExpenseReviewItemSchema = z.object({
  regularExpenseId: guid,
  amount: regularExpenseAmountSchema,
}).strict();
const regularExpenseBodySchemas = {
  create_expense: z.object({
    name: regularExpenseNameSchema,
    type: regularExpenseTypeSchema,
    currency: currencySchema,
    defaultAmount: regularExpenseAmountSchema,
    orderPriority: z.number().int().optional().default(-1),
  }).strict(),
  update_expense: z.object({
    id: guid,
    name: regularExpenseNameSchema.optional(),
    type: regularExpenseTypeSchema.optional(),
    currency: currencySchema.optional(),
    defaultAmount: regularExpenseAmountSchema.optional(),
    orderPriority: z.number().int().optional(),
  }).strict().refine(
    (value) => value.name !== undefined
      || value.type !== undefined
      || value.currency !== undefined
      || value.defaultAmount !== undefined
      || value.orderPriority !== undefined,
    "At least one mutable field is required.",
  ),
  save_review: z.object({
    id: guid.optional(),
    month: regularExpenseMonthOrDateSchema.transform(normalizeRegularExpenseMonthValue),
    expenses: z.array(regularExpenseReviewItemSchema).default([]),
  }).strict(),
  update_diagram_setup: z.object({
    mainCurrency: currencySchema,
    lastMonths: z.number().int().min(1).max(120).optional(),
    endMonth: regularExpenseMonthOrDateSchema
      .transform(normalizeRegularExpenseMonthValue)
      .optional(),
  }).strict(),
} as const;

function normalizeRegularExpenseMonthValue(value: string): string {
  return `${value.slice(0, 7)}-01`;
}

function isValidDateOnly(value: string): boolean {
  const [year, month, day] = value.split("-").map(Number);
  if (year < 1) return false;

  const date = new Date(0);
  date.setUTCFullYear(year, month - 1, day);
  date.setUTCHours(0, 0, 0, 0);
  return date.getUTCFullYear() === year
    && date.getUTCMonth() === month - 1
    && date.getUTCDate() === day;
}

const readOnlyAnnotations = {
  readOnlyHint: true,
  destructiveHint: false,
  idempotentHint: true,
  openWorldHint: false,
};
