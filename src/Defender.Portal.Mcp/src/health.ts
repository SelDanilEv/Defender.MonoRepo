import type { Express, Request, Response } from "express";
import type { McpConfig } from "./config.js";

export interface PortalReadinessProbe {
  check(): Promise<void>;
}

export function createPortalReadinessProbe(config: McpConfig, fetchLike: typeof fetch = fetch): PortalReadinessProbe {
  const endpoints = [
    new URL("/health", config.portalBaseUrl),
    new URL("/.well-known/jwks", config.portalIssuer),
  ];

  return {
    async check(): Promise<void> {
      const timeout = AbortSignal.timeout(5_000);
      const responses = await Promise.all(endpoints.map((endpoint) => fetchLike(endpoint, { signal: timeout })));
      if (!responses.every((response) => response.ok)) throw new Error("Portal dependency is unavailable.");
    },
  };
}

export function mapHealthRoutes(app: Express, readiness: PortalReadinessProbe): void {
  app.get("/health", (_request: Request, response: Response) => response.status(200).json({ status: "ok" }));
  app.get("/health/ready", async (_request: Request, response: Response) => {
    try {
      await readiness.check();
      return response.status(200).json({ status: "ok" });
    } catch {
      return response.status(503).json({ status: "unavailable" });
    }
  });
}
