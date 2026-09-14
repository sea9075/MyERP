import { apiClient } from './client';
import type { LoginRequest, LoginResponse } from './types';

export async function login(request: LoginRequest): Promise<LoginResponse> {
  const { data } = await apiClient.post<LoginResponse>('/auth/login', request);
  return data;
}
