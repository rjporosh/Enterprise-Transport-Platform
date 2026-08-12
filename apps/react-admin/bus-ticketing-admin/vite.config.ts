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
      '/api/v1/auth': { target: 'http://localhost:5203', changeOrigin: true, secure: false },
      '/api/v1/buses': { target: 'http://localhost:5201', changeOrigin: true, secure: false },
      '/api/v1/routes': { target: 'http://localhost:5204', changeOrigin: true, secure: false },
      '/api/v1': { target: 'http://localhost:8080', changeOrigin: true, secure: false }
    }
  },
  build: { outDir: 'dist', sourcemap: true }
});
