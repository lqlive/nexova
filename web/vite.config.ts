import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    port: 3000,
    proxy: {
      '/api': {
        target: 'http://localhost:5101',
        changeOrigin: true,
      },
      '/engine': {
        target: 'http://localhost:7071',
        changeOrigin: true,
        rewrite: (path) => path.replace(/^\/engine/, ''),
      },
    },
  },
  build: {
    outDir: 'dist',
    sourcemap: true
  }
})