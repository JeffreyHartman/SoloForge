import { defineConfig } from '@playwright/test'

export default defineConfig({
  testDir: './e2e',
  fullyParallel: false,
  retries: 0,
  workers: 1,
  reporter: 'list',
  use: {
    baseURL: 'http://localhost:5173',
    trace: 'retain-on-failure',
  },
  webServer: {
    command: 'bash ../../start-dev.sh',
    // Wait for the API health endpoint (proxied through Vite) to confirm both servers are up
    url: 'http://localhost:5173/api/health',
    reuseExistingServer: !process.env.CI,
    timeout: 60_000,
  },
})
