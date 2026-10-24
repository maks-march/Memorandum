import { defineConfig, loadEnv } from 'vite';
import react from '@vitejs/plugin-react';

// Два режима:
//  • dev  — Vite proxy отправляет /api и /mock на бэкенд (VITE_API_TARGET, по умолчанию localhost:5000)
//  • prod — статику отдаёт nginx, а /api проксирует на контейнер api (см. infra/nginx)
export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), '');
  const target = env.VITE_API_TARGET || 'http://localhost:5000';

  return {
    plugins: [react()],
    server: {
      host: '0.0.0.0',
      port: Number(env.VITE_PORT) || 5173,
      strictPort: true,
      allowedHosts: true,          // нужно для preview-домена в песочнице/на VPS
      proxy: {
        '/api': { target, changeOrigin: true },
        '/mock': { target, changeOrigin: true },
      },
    },
    build: {
      outDir: 'dist',
      sourcemap: mode !== 'production',
    },
  };
});
