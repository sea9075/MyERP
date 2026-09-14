import { fileURLToPath, URL } from 'node:url';
import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: {
      // 注意：不能直接用 `new URL('./src', import.meta.url).pathname`。
      // 在 Windows 上，file URL 的 pathname 會多一個開頭斜線（變成 "/C:/Users/..."），
      // 這個字串本身不是合法的 Windows 路徑，會導致 "@/xxx" 這種別名 import 全部解析失敗
      // （這次遇到的 "Failed to resolve import ... AppLayout" 就是這個原因）。
      // fileURLToPath 會正確轉成該作業系統原生的路徑格式，跨平台都能用。
      '@': fileURLToPath(new URL('./src', import.meta.url)),
    },
  },
  server: {
    port: 5173,
  },
});
