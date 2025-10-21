import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import path from "path";

export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: {
      "@": path.resolve(__dirname, "./src"),
    },
  },
  server: {
    host: true,
    port: 5173,
    proxy: {
      // Proxy API calls to backend
      "/api": {
        target: "http://localhost:5023",
        changeOrigin: true,
        secure: false,
      },
      // Proxy SignalR hubs
      "/hubs": {
        target: "http://localhost:5023",
        changeOrigin: true,
        ws: true, // Important for WebSocket/SignalR
        secure: false,
      },
    },
  },
});
