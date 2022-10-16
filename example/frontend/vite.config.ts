import { defineConfig } from 'vite'
import legacy from '@vitejs/plugin-legacy'
import react from '@vitejs/plugin-react'
import svgrPlugin from 'vite-plugin-svgr'

// https://vitejs.dev/config/
export default defineConfig(configEnv => ({
    plugins: [
        react(),
        svgrPlugin(),
        // legacy({
        //     targets: ['> 0.01%']
        // })
    ],
    server: {
        host: "127.0.0.1"
    },
    build: {
        outDir: '../static',
    }
}))

