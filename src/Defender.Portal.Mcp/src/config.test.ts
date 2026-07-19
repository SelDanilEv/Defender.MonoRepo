import assert from "node:assert/strict";
import test from "node:test";
import { loadConfig } from "./config.js";

test("loadConfig_WhenIssuerHasNoTrailingSlash_NormalizesItForJwtValidation", () => {
  const config = loadConfig({
    PORTAL_BASE_URL: "https://portal.coded-by-danil.dev",
    PORTAL_OAUTH_ISSUER: "https://portal.coded-by-danil.dev",
    MCP_PUBLIC_URL: "https://mcp.coded-by-danil.dev",
  });

  assert.equal(config.portalIssuer, "https://portal.coded-by-danil.dev/");
});
