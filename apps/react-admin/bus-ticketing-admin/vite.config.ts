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
    // M0: single API entry point. `vite dev` proxies every /api call to the
    // platform API gateway (YARP), which owns routing to the individual
    // backend services. This file no longer knows about any service or port.
    // Run the gateway locally: `dotnet run --project infrastructure/gateway/src/Platform.Gateway`
    // (listens on http://localhost:8080).
    proxy: {
      '/api': {
        target: 'http://localhost:8080',
        changeOrigin: true,
        secure: false
      }
    }
  },
  build: { outDir: 'dist', sourcemap: true }
});
