import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import path from 'path';

export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
      '@styles': path.resolve(__dirname, './src/styles'),
      '@features': path.resolve(__dirname, './src/features'),
    },
  },
  server: {
    host: true,
    port: 5173,
    proxy: {
      '/api': {
        target: 'http://localhost:5023',
        changeOrigin: true,
        secure: false,
      },
      '/hubs': {
        target: 'http://localhost:5023',
        changeOrigin: true,
        ws: true,
        secure: false,
      },
    },
  },
});
