import { apiClient } from './client';
import type { ChangePasswordRequest, LoginRequest, LoginResponse } from './types';

export async function login(request: LoginRequest): Promise<LoginResponse> {
  const { data } = await apiClient.post<LoginResponse>('/auth/login', request);
  return data;
}

/** 使用者自己改自己的密碼（右上角選單「密碼修改」），任何登入使用者都可以用，不分部門。 */
export async function changePassword(request: ChangePasswordRequest): Promise<void> {
  await apiClient.put('/auth/password', request);
}
