import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { PortalBffClient } from "./services/portal-bff-client.js";
import { registerPortalTools } from "./tools/portal-tools.js";

export function createPortalMcpServer(bffToken: string, scopes: Set<string>, portalBaseUrl: string): McpServer {
  const server = new McpServer({ name: "defender-portal-mcp-server", version: "1.0.0" });
  registerPortalTools(server, { bff: new PortalBffClient(portalBaseUrl), bffToken, scopes });
  return server;
}
