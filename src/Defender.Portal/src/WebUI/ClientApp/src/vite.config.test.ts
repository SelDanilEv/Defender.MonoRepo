import { describe, expect, test } from "vitest";

import createViteConfig from "../vite.config";

describe("Portal Vite build configuration", () => {
  test("WhenUsedForDevelopment_PreservesSpaProxyAndPortalOutput", () => {
    const config = createViteConfig({ command: "serve", mode: "development", isSsrBuild: false, isPreview: false });

    expect(config.build?.outDir).toBe("build");
    expect(config.server?.port).toBe(47054);
    const apiProxy = config.server?.proxy?.["/api"];
    expect(typeof apiProxy === "string" ? apiProxy : apiProxy?.target).toBe("http://localhost:47053");
    expect(config.resolve?.alias).toEqual({ src: expect.any(String) });
  });
});
