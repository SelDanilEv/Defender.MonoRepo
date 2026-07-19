import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { createMcpExpressApp } from "@modelcontextprotocol/sdk/server/express.js";
import { StreamableHTTPServerTransport } from "@modelcontextprotocol/sdk/server/streamableHttp.js";
import { createRemoteJWKSet, jwtVerify } from "jose";
import { z } from "zod";
import { loadConfig } from "./config.js";
import { PortalBffClient } from "./portal-bff-client.js";

const config = loadConfig();
const bff = new PortalBffClient(config.portalBaseUrl);
const jwks = createRemoteJWKSet(new URL("/.well-known/jwks", config.portalIssuer));
const app = createMcpExpressApp({
  host: "0.0.0.0",
  allowedHosts: [new URL(config.publicUrl).host],
});

app.get("/health", (_request, response) => response.status(200).json({ status: "ok" }));
app.get("/.well-known/oauth-protected-resource/mcp", (_request, response) => response.json({
  resource: `${config.publicUrl}/mcp`, authorization_servers: [config.portalIssuer],
  scopes_supported: ["mcp:portal:read", "mcp:calendar:write", "mcp:calendar:delete"],
}));
app.post("/mcp", async (request, response) => {
  const origin = request.header("origin");
  if (origin && !config.allowedOrigins.has(origin)) return response.sendStatus(403);

  const token = request.header("authorization")?.replace(/^Bearer\s+/i, "");
  if (!token) return response.status(401).set("WWW-Authenticate", `Bearer resource_metadata="${config.publicUrl}/.well-known/oauth-protected-resource/mcp"`).send();

  try {
    const verified = await jwtVerify(token, jwks, { issuer: config.portalIssuer, audience: config.mcpAudience });
    const scopes = new Set(String(verified.payload.scope ?? "").split(" ").filter(Boolean));
    const bffToken = await bff.exchangeMcpToken(token);
    const server = createServer(bffToken, scopes);
    const transport = new StreamableHTTPServerTransport({ sessionIdGenerator: undefined });
    await server.connect(transport);
    await transport.handleRequest(request, response, request.body);
    response.on("close", () => void Promise.all([transport.close(), server.close()]));
  } catch (error) {
    const message = error instanceof Error ? error.message : "Unauthorized";
    if (!response.headersSent) response.status(401).json({ error: "invalid_token", error_description: message });
  }
});

function createServer(bffToken: string, scopes: Set<string>): McpServer {
  const server = new McpServer({ name: "defender-portal-mcp", version: "1.0.0" });
  const requireScope = (scope: string) => scopes.has(scope) ? undefined : ({ content: [{ type: "text" as const, text: `Missing required scope: ${scope}` }], isError: true });

  server.registerTool("calendar_get", {
    description: "Read the complete personal travel calendar. Dates are optional ISO-8601 boundaries.",
    inputSchema: { from: z.string().optional(), to: z.string().optional() },
    annotations: { readOnlyHint: true },
  }, async ({ from, to }) => {
    const denied = requireScope("mcp:portal:read"); if (denied) return denied;
    const query = new URLSearchParams(); if (from) query.set("from", from); if (to) query.set("to", to);
    const payload = await bff.get(`/api/travelcalendar${query.size ? `?${query}` : ""}`, bffToken);
    return jsonResult(payload);
  });

  server.registerTool("calendar_search_users", {
    description: "Search Portal users to add as travel-event participants.",
    inputSchema: { query: z.string().min(1) }, annotations: { readOnlyHint: true },
  }, async ({ query }) => {
    const denied = requireScope("mcp:portal:read"); if (denied) return denied;
    return jsonResult(await bff.get(`/api/travelcalendar/users?query=${encodeURIComponent(query)}`, bffToken));
  });

  server.registerTool("portal_read", {
    description: "Read Portal wallet or lottery data. Supported resources: wallet_info, transaction_history, active_lottery_draws, lottery_tickets, available_lottery_tickets. No money movement or ticket purchase is exposed.",
    inputSchema: { resource: z.string(), query: z.record(z.string(), z.string()).optional() },
    annotations: { readOnlyHint: true },
  }, async ({ resource, query }) => {
    const denied = requireScope("mcp:portal:read"); if (denied) return denied;
    const path = portalReadResources[resource];
    if (!path) return { content: [{ type: "text", text: "Unsupported read resource." }], isError: true };
    const parameters = new URLSearchParams(query ?? {});
    return jsonResult(await bff.get(`${path}${parameters.size ? `?${parameters}` : ""}`, bffToken));
  });

  server.registerTool("calendar_mutate", {
    description: "Perform a supported Travel Calendar mutation through Portal BFF. Delete operations require confirm=true. Supply the exact Portal request body for the selected operation.",
    inputSchema: { operation: z.string(), eventId: z.string().uuid().optional(), itemId: z.string().uuid().optional(), pointId: z.string().uuid().optional(), participantUserId: z.string().uuid().optional(), body: z.record(z.string(), z.unknown()), confirm: z.boolean().optional() },
    annotations: { destructiveHint: true },
  }, async (input) => {
    const operation = calendarOperations[input.operation];
    if (!operation) return { content: [{ type: "text", text: "Unsupported calendar operation." }], isError: true };
    const denied = requireScope(operation.delete ? "mcp:calendar:delete" : "mcp:calendar:write"); if (denied) return denied;
    if (operation.delete && input.confirm !== true) return { content: [{ type: "text", text: "Deletion requires confirm=true." }], isError: true };
    const path = operation.path(input);
    if (!path) return { content: [{ type: "text", text: "Required resource identifier is missing." }], isError: true };
    return jsonResult(await bff.request(path, { method: operation.method, headers: { "content-type": "application/json" }, body: JSON.stringify(input.body) }, bffToken));
  });
  return server;
}

interface CalendarMutationInput { eventId?: string; itemId?: string; pointId?: string; participantUserId?: string; }
interface CalendarOperation { method: string; delete?: boolean; path: (input: CalendarMutationInput) => string | undefined; }
const calendarOperations: Record<string, CalendarOperation> = {
  update_theme: { method: "PATCH", path: () => "/api/travelcalendar/theme" }, create_queued_trip: { method: "POST", path: () => "/api/travelcalendar/queued-trips" }, create_event_from_date: { method: "POST", path: () => "/api/travelcalendar/events/from-date" }, create_event: { method: "POST", path: () => "/api/travelcalendar/events" },
  update_event: { method: "PUT", path: (i: any) => i.eventId && `/api/travelcalendar/events/${i.eventId}` }, delete_event: { method: "DELETE", delete: true, path: (i: any) => i.eventId && `/api/travelcalendar/events/${i.eventId}` }, auto_schedule_event: { method: "POST", path: (i: any) => i.eventId && `/api/travelcalendar/events/${i.eventId}/auto-schedule` },
  add_point: { method: "POST", path: (i: any) => i.eventId && `/api/travelcalendar/events/${i.eventId}/points` }, update_point: { method: "PATCH", path: (i: any) => i.eventId && i.pointId && `/api/travelcalendar/events/${i.eventId}/points/${i.pointId}` }, delete_point: { method: "DELETE", delete: true, path: (i: any) => i.eventId && i.pointId && `/api/travelcalendar/events/${i.eventId}/points/${i.pointId}` },
  add_participant: { method: "POST", path: (i: any) => i.eventId && `/api/travelcalendar/events/${i.eventId}/participants` }, delete_participant: { method: "DELETE", delete: true, path: (i: any) => i.eventId && i.participantUserId && `/api/travelcalendar/events/${i.eventId}/participants/${i.participantUserId}` }, update_my_participation: { method: "PATCH", path: (i: any) => i.eventId && `/api/travelcalendar/events/${i.eventId}/my-participation` },
  add_packing_item: { method: "POST", path: () => "/api/travelcalendar/packing-items" }, update_packing_item: { method: "PATCH", path: (i: any) => i.itemId && `/api/travelcalendar/packing-items/${i.itemId}` }, delete_packing_item: { method: "DELETE", delete: true, path: (i: any) => i.itemId && `/api/travelcalendar/packing-items/${i.itemId}` },
};

const portalReadResources: Record<string, string> = {
  wallet_info: "/api/banking/wallet/info",
  transaction_history: "/api/banking/transaction/history",
  active_lottery_draws: "/api/lottery/draw/active",
  lottery_tickets: "/api/lottery/tickets",
  available_lottery_tickets: "/api/lottery/tickets/available",
};

function jsonResult(payload: unknown) { return { content: [{ type: "text" as const, text: JSON.stringify(payload) }] }; }
app.listen(config.port, () => console.log(`Defender Portal MCP listening on ${config.port}`));
