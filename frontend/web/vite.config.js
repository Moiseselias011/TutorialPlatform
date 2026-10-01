import vue from '@vitejs/plugin-vue'
import { defineConfig } from 'vite'

// https://vite.dev/config/
export default defineConfig({
  plugins: [vue()],
  server: {
    port: 5173,
    // Proxea /api y /uploads al backend .NET en desarrollo
    proxy: {
      '/api': { target: 'http://localhost:5099', changeOrigin: true },
      '/uploads': { target: 'http://localhost:5099', changeOrigin: true },
    },
  },
})
