export interface McpConfig {
  port: number;
  portalBaseUrl: string;
  portalIssuer: string;
  publicUrl: string;
  mcpAudience: string;
  allowedOrigins: Set<string>;
}

export function loadConfig(environment: NodeJS.ProcessEnv = process.env): McpConfig {
  const portalBaseUrl = requireHttpsOrigin(environment.PORTAL_BASE_URL ?? "https://portal.coded-by-danil.dev");
  const portalIssuer = requireHttpsIssuer(environment.PORTAL_OAUTH_ISSUER ?? portalBaseUrl);
  const allowedOrigins = (environment.MCP_ALLOWED_ORIGINS ?? "")
    .split(",")
    .map((origin) => origin.trim())
    .filter(Boolean);

  return {
    port: Number.parseInt(environment.PORT ?? "3000", 10),
    portalBaseUrl,
    portalIssuer,
    publicUrl: requireHttpsOrigin(environment.MCP_PUBLIC_URL ?? "https://mcp.coded-by-danil.dev"),
    mcpAudience: environment.MCP_AUDIENCE ?? "defender-mcp",
    allowedOrigins: new Set(allowedOrigins),
  };
}

function requireHttpsOrigin(value: string): string {
  const url = new URL(value);
  if (url.protocol !== "https:") throw new Error("MCP public dependencies must use HTTPS.");
  return url.origin;
}

function requireHttpsIssuer(value: string): string {
  const url = new URL(value);
  if (url.protocol !== "https:") throw new Error("MCP public dependencies must use HTTPS.");
  return url.href;
}
