import { defineConfig, devices } from "@playwright/test";

export default defineConfig({
  testDir: "./tests/staging",
  expect: {
    timeout: 15_000,
  },
  fullyParallel: false,
  workers: 1,
  reporter: [["list"]],
  use: {
    baseURL: process.env.SERVICEPILOT_STAGING_URL ?? "https://localhost:8443",
    ignoreHTTPSErrors: true,
    screenshot: "only-on-failure",
    trace: "off",
  },
  projects: [
    {
      name: "staging-chromium",
      use: { ...devices["Desktop Chrome"] },
    },
  ],
});
