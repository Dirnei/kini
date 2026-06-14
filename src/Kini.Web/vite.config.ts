import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'
import path from 'node:path'

// The build output lands directly in the API project's wwwroot so Kestrel
// serves it as static files. Dev mode proxies API requests to Kestrel.
export default defineConfig({
  plugins: [react(), tailwindcss()],
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
    },
  },
  server: {
    port: 5173,
    strictPort: true,
    proxy: {
      '/v1':          'http://localhost:5000',
      '/.well-known': 'http://localhost:5000',
      '/healthz':     'http://localhost:5000',
    },
  },
  build: {
    outDir: '../Kini.Api/wwwroot',
    emptyOutDir: true,
  },
})
