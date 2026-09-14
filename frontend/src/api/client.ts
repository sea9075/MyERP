import axios, { AxiosError } from 'axios';
import { getStoredToken, useAuthStore } from '@/stores/authStore';
import type { ApiErrorPayload } from './types';

export const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL,
  timeout: 15000,
});

apiClient.interceptors.request.use((config) => {
  const token = getStoredToken();
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

apiClient.interceptors.response.use(
  (response) => response,
  (error: AxiosError<ApiErrorPayload>) => {
    if (error.response?.status === 401) {
      // JWT 過期或無效：清掉本機登入狀態，讓 ProtectedRoute 導回登入頁。
      // 登入頁本身呼叫 /api/auth/login 失敗時也會走到這裡，但那時 isAuthenticated 本來就是
      // false，logout() 只是把它保持在 false，不會有副作用。
      useAuthStore.getState().logout();
    }
    return Promise.reject(error);
  },
);

/** 把後端各種錯誤回應格式，統一整理成一句可以直接顯示給使用者看的訊息。 */
export function extractErrorMessage(error: unknown, fallback = '發生未預期的錯誤，請稍後再試。'): string {
  if (axios.isAxiosError(error)) {
    const data = error.response?.data as ApiErrorPayload | undefined;

    if (data?.message) {
      return data.message;
    }

    // [ApiController] 自動 Model Validation 失敗（400）會是 ValidationProblemDetails 格式。
    if (data?.errors) {
      const firstField = Object.values(data.errors)[0];
      if (firstField && firstField.length > 0) {
        return firstField[0];
      }
    }

    if (error.response?.status === 401) {
      return '帳號或密碼錯誤。';
    }

    if (error.code === 'ECONNABORTED') {
      return '連線逾時，請確認後端服務是否正常運作。';
    }

    if (!error.response) {
      return '無法連線到後端服務，請確認後端是否已啟動、網址是否正確。';
    }
  }

  return fallback;
}
