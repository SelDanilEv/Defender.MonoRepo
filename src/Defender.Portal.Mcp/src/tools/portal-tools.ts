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

const isoDate = z.string().regex(/^\d{4}-\d{2}-\d{2}$/, "Use ISO-8601 date YYYY-MM-DD.");
const guid = z.string().uuid();
const readOnlyAnnotations = {
  readOnlyHint: true,
  destructiveHint: false,
  idempotentHint: true,
  openWorldHint: false,
};
