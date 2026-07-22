import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import tailwindcss from '@tailwindcss/vite';

// The API base is proxied in dev so the frontend can call /api directly,
// matching the nginx reverse-proxy setup used in the Docker image.
export default defineConfig({
  plugins: [react(), tailwindcss()],
  server: {
    port: 5173,
    proxy: {
      '/api': {
        target: process.env.VITE_API_TARGET ?? 'http://localhost:5080',
        changeOrigin: true,
      },
    },
  },
});
