import assert from "node:assert/strict";
import { once } from "node:events";
import { createServer } from "node:http";
import test from "node:test";
import express from "express";
import { mapHealthRoutes, type PortalReadinessProbe } from "./health.js";

test("mapHealthRoutes_WhenReadinessDependencyFails_OnlyReadinessIsUnavailable", async () => {
  const app = express();
  const failingReadiness: PortalReadinessProbe = {
    check: async () => { throw new Error("Portal unavailable"); },
  };
  mapHealthRoutes(app, failingReadiness);

  await withServer(app, async (baseUrl) => {
    const liveness = await fetch(`${baseUrl}/health`);
    const readiness = await fetch(`${baseUrl}/health/ready`);

    assert.equal(liveness.status, 200);
    assert.deepEqual(await liveness.json(), { status: "ok" });
    assert.equal(readiness.status, 503);
  });
});

test("mapHealthRoutes_WhenReadinessDependenciesSucceed_ReturnsOk", async () => {
  const app = express();
  const ready: PortalReadinessProbe = { check: async () => undefined };
  mapHealthRoutes(app, ready);

  await withServer(app, async (baseUrl) => {
    const readiness = await fetch(`${baseUrl}/health/ready`);

    assert.equal(readiness.status, 200);
    assert.deepEqual(await readiness.json(), { status: "ok" });
  });
});

async function withServer(app: express.Express, action: (baseUrl: string) => Promise<void>): Promise<void> {
  const server = createServer(app);
  server.listen(0, "127.0.0.1");
  await once(server, "listening");
  const address = server.address();
  if (!address || typeof address === "string") throw new Error("Test server address unavailable.");

  try {
    await action(`http://127.0.0.1:${address.port}`);
  } finally {
    server.close();
    await once(server, "close");
  }
}
