import { existsSync, readFileSync } from "node:fs";
import { resolve } from "node:path";

import react from "@vitejs/plugin-react";
import { loadEnv } from "vite";
import { defineConfig } from "vitest/config";

function getApiTarget(): string {
  if (process.env.ASPNETCORE_HTTPS_PORT) {
    return `http://localhost:${process.env.ASPNETCORE_HTTPS_PORT}`;
  }

  return process.env.ASPNETCORE_URLS?.split(";")[0] ?? "http://localhost:47053";
}

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), "");
  const certificatePath = env.SSL_CRT_FILE;
  const keyPath = env.SSL_KEY_FILE;
  const https = certificatePath && keyPath && existsSync(certificatePath) && existsSync(keyPath)
    ? { cert: readFileSync(certificatePath), key: readFileSync(keyPath) }
    : undefined;

  return {
    plugins: [react()],
    resolve: {
      alias: {
        src: resolve(__dirname, "src"),
      },
    },
    define: {
      "process.env.NODE_ENV": JSON.stringify(mode),
      "process.env.PUBLIC_URL": JSON.stringify(""),
    },
    server: {
      https,
      port: 47054,
      strictPort: true,
      proxy: {
        "/api": {
          target: getApiTarget(),
          secure: false,
        },
      },
    },
    build: {
      outDir: "build",
    },
    test: {
      environment: "jsdom",
      globals: true,
    },
  };
});
