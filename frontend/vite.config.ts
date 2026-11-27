import tailwindcss from '@tailwindcss/vite'
import basicSsl from '@vitejs/plugin-basic-ssl'
import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

// Dev: HTTPS (camera điện thoại chỉ chạy qua HTTPS) và mở ra mạng LAN để điện thoại cùng Wi-Fi truy cập.
// /api được proxy sang FoMart.Api nên frontend gọi đường dẫn tương đối, không vướng CORS.
export default defineConfig({
  plugins: [react(), tailwindcss(), basicSsl()],
  server: {
    host: true,
    port: 5173,
    strictPort: true,
    proxy: {
      '/api': { target: 'http://localhost:5224', changeOrigin: true },
      '/health': { target: 'http://localhost:5224', changeOrigin: true },
    },
  },
})
