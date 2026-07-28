import { readFileSync } from "node:fs";
import { resolve } from "node:path";

import { describe, expect, test } from "vitest";

const clientIndexHtml = readFileSync(resolve(__dirname, "../../index.html"), "utf8");

describe("Telegram Mini App bootstrap page", () => {
  test("loads Telegram bridge before application entry", () => {
    const bridgeIndex = clientIndexHtml.indexOf("https://telegram.org/js/telegram-web-app.js?63");
    const applicationIndex = clientIndexHtml.indexOf('src="/src/index.tsx"');

    expect(bridgeIndex).toBeGreaterThanOrEqual(0);
    expect(bridgeIndex).toBeLessThan(applicationIndex);
  });
});
