import vinext from "vinext";
import { defineConfig } from "vite";
import { resolve } from "node:path";
import hostingConfig from "./.openai/hosting.json";
import { sites } from "./build/sites-vite-plugin";

const CLOUDFLARE_DATABASE_ID = "4910a936-e4c6-4dd7-848d-5d3aac46d5da";

const { d1, r2 } = hostingConfig;

// macOS Seatbelt blocks FSEvents, so Codex previews need polling for HMR.
const isCodexSeatbeltSandbox = process.env.CODEX_SANDBOX === "seatbelt";
const isOfflineWindowsPreview = process.env.NEON_OFFLINE_PREVIEW === "1";

const localBindingConfig = {
  main: "./worker/index.ts",
  compatibility_flags: ["nodejs_compat"],
  d1_databases: d1
    ? [{ binding: d1, database_name: "neon-clash-rooms", database_id: CLOUDFLARE_DATABASE_ID }]
    : [],
  r2_buckets: r2 ? [{ binding: r2, bucket_name: "site-creator-r2" }] : [],
};

export default defineConfig(async () => {
  // Keep Wrangler and Miniflare state project-local. These are non-secret tool
  // settings; application environment belongs in ignored `.env*` files.
  process.env.WRANGLER_WRITE_LOGS ??= "false";
  process.env.WRANGLER_LOG_PATH ??= ".wrangler/logs";
  process.env.MINIFLARE_REGISTRY_PATH ??= ".wrangler/registry";

  const cloudflarePlugin = isOfflineWindowsPreview
    ? []
    : [(await import("@cloudflare/vite-plugin")).cloudflare({
        viteEnvironment: { name: "rsc", childEnvironments: ["ssr"] },
        config: localBindingConfig,
      })];

  return {
    server: {
      host: "0.0.0.0",
      ...(isCodexSeatbeltSandbox ? { watch: { useFsEvents: false, usePolling: true } } : {}),
    },
    preview: { host: "0.0.0.0" },
    resolve: isOfflineWindowsPreview
      ? { alias: { "cloudflare:workers": resolve("build/cloudflare-workers-local.ts") } }
      : undefined,
    plugins: [vinext(), sites(), ...cloudflarePlugin],
  };
});
