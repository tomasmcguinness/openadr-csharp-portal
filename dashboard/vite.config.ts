import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

// During `npm run dev` the dashboard runs on its own Vite dev server, so API and
// SignalR calls are proxied to the VTN server (see ../src/OpenAdr.Vtn.Server's
// launchSettings.json for the dev port). In production the built dashboard is
// served by Kestrel itself from wwwroot/, same-origin, and this proxy is unused.
export default defineConfig({
  plugins: [react()],
  server: {
    proxy: {
      '/api': 'http://localhost:5080',
      '/hubs': {
        target: 'http://localhost:5080',
        ws: true,
      },
    },
  },
})
