import react from '@vitejs/plugin-react';
import { defineConfig, loadEnv } from 'vite';
import { mockEditor } from './mocks/editorMiddleware.js';

export default defineConfig(({ mode }) => {
  const mock = mode === 'mock';
  const { PIXELFLOW_API_TARGET = 'http://localhost:5193' } = loadEnv(mode, process.cwd(), 'PIXELFLOW_');

  return {
    plugins: [react(), ...(mock ? [mockEditor()] : [])],
    server: {
      host: '127.0.0.1',
      strictPort: true,
      proxy: mode === 'api' ? { '/api': PIXELFLOW_API_TARGET } : undefined,
    },
    preview: {
      host: '127.0.0.1',
      strictPort: true,
    },
  };
});
