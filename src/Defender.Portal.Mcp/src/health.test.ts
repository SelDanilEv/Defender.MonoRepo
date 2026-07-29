import assert from "node:assert/strict";
import { createServer } from "node:net";
import test from "node:test";
import { once } from "node:events";
import { spawn } from "node:child_process";

test("health_WhenRequested_ReturnsProcessHealth", async () => {
  const port = await reservePort();
  const process = spawn("node", ["--import", "tsx", "src/index.ts"], {
    cwd: new URL("..", import.meta.url),
    env: {
      ...globalThis.process.env,
      MCP_PUBLIC_URL: "https://127.0.0.1",
      PORT: String(port),
    },
    stdio: ["ignore", "pipe", "pipe"],
  });

  try {
    await waitForStartup(process);

    const response = await fetch(`http://127.0.0.1:${port}/health`);

    assert.equal(response.status, 200);
    assert.deepEqual(await response.json(), { status: "ok" });
  } finally {
    process.kill();
    await once(process, "exit");
  }
});

async function reservePort(): Promise<number> {
  const server = createServer();
  server.listen(0, "127.0.0.1");
  await once(server, "listening");
  const address = server.address();
  server.close();
  await once(server, "close");

  if (!address || typeof address === "string") throw new Error("Unable to reserve test port.");
  return address.port;
}

async function waitForStartup(child: ReturnType<typeof spawn>): Promise<void> {
  const stdout = child.stdout;
  if (!stdout) throw new Error("MCP server stdout is unavailable.");

  await new Promise<void>((resolve, reject) => {
    const timeout = setTimeout(() => reject(new Error("MCP server did not start.")), 10_000);
    stdout.on("data", (data) => {
      if (!data.toString().includes("listening on")) return;

      clearTimeout(timeout);
      resolve();
    });
  });
}
