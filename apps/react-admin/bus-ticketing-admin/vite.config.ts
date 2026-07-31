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
  server: { port: 5173 },
  build: { outDir: 'dist', sourcemap: true }
});
