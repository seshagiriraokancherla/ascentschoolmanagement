import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import { readFileSync } from 'fs'

const pkg = JSON.parse(readFileSync('./package.json', 'utf-8'))

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  define: {
    // App version (from package.json) + build date — shown in the footer so we can
    // tell which build is live. __BUILD_DATE__ updates on every build automatically.
    __APP_VERSION__: JSON.stringify(pkg.version),
    __BUILD_DATE__:  JSON.stringify(new Date().toISOString().slice(0, 16).replace('T', ' ')),
  },
})
