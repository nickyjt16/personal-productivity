import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  build: {
    // Build straight into the API's wwwroot so a single Kestrel process serves
    // the SPA in production.
    outDir: '../ProductivityHub.Api/wwwroot',
    emptyOutDir: true,
  },
  server: {
    // During `npm run dev`, proxy API calls to the running Kestrel process.
    proxy: {
      '/api': 'http://localhost:5180',
    },
  },
})
