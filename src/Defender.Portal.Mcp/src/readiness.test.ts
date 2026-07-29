import assert from "node:assert/strict";
import test from "node:test";
import { loadConfig } from "./config.js";
import { createPortalReadinessProbe } from "./health.js";

const config = loadConfig({
  PORTAL_BASE_URL: "https://portal.example.test",
  PORTAL_OAUTH_ISSUER: "https://issuer.example.test",
  MCP_PUBLIC_URL: "https://mcp.example.test",
});

test("portalReadiness_WhenPortalDependenciesRespond_ReturnsTrue", async () => {
  const probe = createPortalReadinessProbe(config, async () => new Response(null, { status: 200 }));
  await probe.check();
});

test("portalReadiness_WhenPortalDependencyFails_ReturnsFalse", async () => {
  const probe = createPortalReadinessProbe(config, async (url) =>
    new Response(null, { status: String(url).includes("jwks") ? 503 : 200 }));
  await assert.rejects(probe.check());
});
