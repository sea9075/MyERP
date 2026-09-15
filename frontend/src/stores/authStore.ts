import { create } from 'zustand';
import type { Department, LoginResponse } from '@/api/types';

const STORAGE_KEY = 'myerp.auth';

interface StoredAuth {
  token: string;
  expiresAtUtc: string;
  userId: number;
  username: string;
  displayName: string;
  role: Department;
}

interface AuthState {
  token: string | null;
  userId: number | null;
  username: string | null;
  displayName: string | null;
  role: Department | null;
  isAuthenticated: boolean;
  login: (response: LoginResponse) => void;
  logout: () => void;
}

function loadFromStorage(): StoredAuth | null {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (!raw) return null;

    const parsed = JSON.parse(raw) as StoredAuth;
    // 登入時效已過期就當作沒登入，避免帶著過期 JWT 一直打 API 收 401。
    if (new Date(parsed.expiresAtUtc).getTime() <= Date.now()) {
      localStorage.removeItem(STORAGE_KEY);
      return null;
    }
    return parsed;
  } catch {
    // localStorage 內容壞掉（不是合法 JSON）就當作沒登入，不要整個 App 掛掉。
    return null;
  }
}

const initial = loadFromStorage();

// 用官方推薦的「curry 版」create<T>()(...) 寫法（而不是 create<T>(...)），
// 這是 zustand 官方文件建議的寫法，可以避免純手動指定泛型時型別推斷不穩定的問題。
export const useAuthStore = create<AuthState>()((set) => ({
  token: initial?.token ?? null,
  userId: initial?.userId ?? null,
  username: initial?.username ?? null,
  displayName: initial?.displayName ?? null,
  role: initial?.role ?? null,
  isAuthenticated: initial !== null,

  login: (response) => {
    const stored: StoredAuth = {
      token: response.token,
      expiresAtUtc: response.expiresAtUtc,
      userId: response.userId,
      username: response.username,
      displayName: response.displayName,
      role: response.role,
    };
    localStorage.setItem(STORAGE_KEY, JSON.stringify(stored));

    set({
      token: response.token,
      userId: response.userId,
      username: response.username,
      displayName: response.displayName,
      role: response.role,
      isAuthenticated: true,
    });
  },

  logout: () => {
    localStorage.removeItem(STORAGE_KEY);
    set({
      token: null,
      userId: null,
      username: null,
      displayName: null,
      role: null,
      isAuthenticated: false,
    });
  },
}));

/** 給 axios 攔截器這種非 React 元件的地方直接讀目前的 token，不用透過 hook。 */
export function getStoredToken(): string | null {
  return useAuthStore.getState().token;
}
