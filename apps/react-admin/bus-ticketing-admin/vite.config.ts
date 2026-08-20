import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import { fileURLToPath } from 'node:url';

export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url)),
      // Consume the shared React component library as source — see
      // apps/shared-ui-library/README.md for why there's no build step.
      '@shared-ui/react': fileURLToPath(new URL('../../shared-ui-library/react/src', import.meta.url))
    }
  },
  server: {
    port: 5173,
    // Routes each real backend service's path prefix to its actual port
    // (most specific first), mirroring nginx.conf for the production
    // build — no API Gateway exists yet (infrastructure/gateway/ is an
    // empty placeholder).
    proxy: {
      '/api/v1/auth': { target: 'http://localhost:5101', changeOrigin: true, secure: false },
      '/api/v1/admin': { target: 'http://localhost:5101', changeOrigin: true, secure: false },
      '/api/v1/buses': { target: 'http://localhost:5201', changeOrigin: true, secure: false },
      '/api/v1/depots': { target: 'http://localhost:5201', changeOrigin: true, secure: false },
      '/api/v1/routes': { target: 'http://localhost:5401', changeOrigin: true, secure: false },
      '/api/v1/schedules': { target: 'http://localhost:5401', changeOrigin: true, secure: false },
      '/api/v1/stops': { target: 'http://localhost:5401', changeOrigin: true, secure: false },
      // Bookings/{id} (get) and Bookings/{id}/cancel are real — see
      // mockAdapter.ts's realBackendWithFallbackAdapter for exactly which
      // /bookings and /trips shapes are genuinely wired vs. still on mock
      // fixtures pending a matching backend endpoint.
      '/api/v1/bookings': { target: 'http://localhost:5601', changeOrigin: true, secure: false },
      '/api/v1/trips': { target: 'http://localhost:5601', changeOrigin: true, secure: false },
      '/api/v1/payments': { target: 'http://localhost:5003', changeOrigin: true, secure: false },
      '/api/v1/notifications': { target: 'http://localhost:5301', changeOrigin: true, secure: false }
    }
  },
  build: { outDir: 'dist', sourcemap: true }
});
