import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { apiClient } from './client';
import type {
  AttendanceRecordDto,
  ClockInRequest,
  ClockOutRequest,
  CreateManualAttendanceRequest,
  UpdateManualAttendanceRequest,
} from './types';

const KEY = 'attendance';

// --- 自助打卡：任何登入使用者都可以用（不分部門），對應後端 [Authorize] 的 3 支 action ---

export interface MyAttendanceParams {
  dateFrom?: string;
  dateTo?: string;
}

async function fetchMyAttendance(params: MyAttendanceParams): Promise<AttendanceRecordDto[]> {
  const { data } = await apiClient.get<AttendanceRecordDto[]>('/attendance/me', { params });
  return data;
}

export function useMyAttendance(params: MyAttendanceParams) {
  return useQuery({
    queryKey: [KEY, 'me', params],
    queryFn: () => fetchMyAttendance(params),
  });
}

export function useClockIn() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (request: ClockInRequest) => apiClient.post<AttendanceRecordDto>('/attendance/clock-in', request),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: [KEY, 'me'] }),
  });
}

export function useClockOut() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (request: ClockOutRequest) => apiClient.post<AttendanceRecordDto>('/attendance/clock-out', request),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: [KEY, 'me'] }),
  });
}

// --- HR/Manager/Admin 出勤管理：查全部 + 手動建立/修改/刪除，對應後端 [Authorize(Roles="HR,Manager,Admin")] ---

export interface AttendanceSearchParams {
  employeeId?: number;
  dateFrom?: string;
  dateTo?: string;
  includeDeleted: boolean;
}

async function searchAttendance(params: AttendanceSearchParams): Promise<AttendanceRecordDto[]> {
  const { data } = await apiClient.get<AttendanceRecordDto[]>('/attendance', { params });
  return data;
}

export function useAttendanceRecords(params: AttendanceSearchParams) {
  return useQuery({
    queryKey: [KEY, 'search', params],
    queryFn: () => searchAttendance(params),
  });
}

export function useCreateManualAttendance() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (request: CreateManualAttendanceRequest) =>
      apiClient.post<AttendanceRecordDto>('/attendance/manual', request),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: [KEY] }),
  });
}

export function useUpdateAttendance() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, request }: { id: number; request: UpdateManualAttendanceRequest }) =>
      apiClient.put<AttendanceRecordDto>(`/attendance/${id}`, request),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: [KEY] }),
  });
}

export function useDeleteAttendance() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: number) => apiClient.delete(`/attendance/${id}`),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: [KEY] }),
  });
}
