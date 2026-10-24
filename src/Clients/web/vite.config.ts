import { readFileSync } from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

import tailwindcss from '@tailwindcss/vite';
import react from '@vitejs/plugin-react';
import { defineConfig } from 'vitest/config';

const root = fileURLToPath(new URL('.', import.meta.url));
const webPackage: { version: string } = JSON.parse(
  readFileSync(new URL('./package.json', import.meta.url), 'utf8'),
);

export default defineConfig({
  define: {
    __APP_VERSION__: JSON.stringify(webPackage.version),
  },
  plugins: [react(), tailwindcss()],
  resolve: {
    alias: {
      '@': path.resolve(root, 'src'),
      '@jjdevhub/theme': path.resolve(root, '../shared/theme/tokens.ts'),
    },
  },
  server: {
    port: 4200,
    proxy: {
      '/api': { target: 'http://localhost:5080', changeOrigin: true },
      '/health': { target: 'http://localhost:5080', changeOrigin: true },
      '/openapi': { target: 'http://localhost:5080', changeOrigin: true },
      '/scalar': { target: 'http://localhost:5080', changeOrigin: true },
    },
  },
  test: {
    environment: 'jsdom',
    globals: true,
    setupFiles: './src/test-setup.ts',
  },
});
