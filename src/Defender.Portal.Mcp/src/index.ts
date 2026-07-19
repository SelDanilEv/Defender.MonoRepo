import { createMcpExpressApp } from "@modelcontextprotocol/sdk/server/express.js";
import { StreamableHTTPServerTransport } from "@modelcontextprotocol/sdk/server/streamableHttp.js";
import { createRemoteJWKSet, jwtVerify } from "jose";
import { loadConfig } from "./config.js";
import { PortalBffClient } from "./services/portal-bff-client.js";
import { createPortalMcpServer } from "./server.js";

const config = loadConfig();
const bff = new PortalBffClient(config.portalBaseUrl);
const jwks = createRemoteJWKSet(new URL("/.well-known/jwks", config.portalIssuer));
const publicHost = new URL(config.publicUrl).host.toLowerCase();
const publicHostname = new URL(config.publicUrl).hostname.toLowerCase();
const app = createMcpExpressApp({ host: "0.0.0.0", allowedHosts: [publicHostname] });

app.get("/health", (_request, response) => response.status(200).json({ status: "ok" }));
app.get("/.well-known/oauth-protected-resource/mcp", (_request, response) => response.json({
  resource: `${config.publicUrl}/mcp`, authorization_servers: [config.portalIssuer],
  scopes_supported: ["mcp:portal:read", "mcp:calendar:write", "mcp:calendar:delete"],
}));
app.post("/mcp", async (request, response) => {
  if (request.header("host")?.toLowerCase() !== publicHost) return response.sendStatus(403);

  const origin = request.header("origin");
  if (origin && !config.allowedOrigins.has(origin)) return response.sendStatus(403);

  const token = request.header("authorization")?.replace(/^Bearer\s+/i, "");
  if (!token) return response.status(401).set("WWW-Authenticate", `Bearer resource_metadata="${config.publicUrl}/.well-known/oauth-protected-resource/mcp"`).send();

  try {
    const verified = await jwtVerify(token, jwks, { issuer: config.portalIssuer, audience: config.mcpAudience });
    const scopes = new Set(String(verified.payload.scope ?? "").split(" ").filter(Boolean));
    const bffToken = await bff.exchangeMcpToken(token);
    const server = createPortalMcpServer(bffToken, scopes, config.portalBaseUrl);
    const transport = new StreamableHTTPServerTransport({ sessionIdGenerator: undefined });
    response.on("close", () => void Promise.all([transport.close(), server.close()]));
    await server.connect(transport);
    await transport.handleRequest(request, response, request.body);
  } catch (error) {
    const message = error instanceof Error ? error.message : "Unauthorized";
    if (!response.headersSent) response.status(401).json({ error: "invalid_token", error_description: message });
  }
});

app.listen(config.port, () => console.log(`Defender Portal MCP listening on ${config.port}`));
