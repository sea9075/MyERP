import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { apiClient } from './client';
import type { CreateEmployeeRequest, EmployeeDto, UpdateEmployeeRequest } from './types';

const KEY = 'employees';

async function fetchEmployees(includeDeleted: boolean): Promise<EmployeeDto[]> {
  const { data } = await apiClient.get<EmployeeDto[]>('/employees', { params: { includeDeleted } });
  return data;
}

export function useEmployees(includeDeleted: boolean) {
  return useQuery({
    queryKey: [KEY, includeDeleted],
    queryFn: () => fetchEmployees(includeDeleted),
  });
}

/** 給出勤管理/薪資管理頁的員工下拉選單用：只要「在職」（未刪除）的員工。 */
export function useActiveEmployees() {
  return useEmployees(false);
}

export function useCreateEmployee() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (request: CreateEmployeeRequest) => apiClient.post<EmployeeDto>('/employees', request),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: [KEY] }),
  });
}

export function useUpdateEmployee() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, request }: { id: number; request: UpdateEmployeeRequest }) =>
      apiClient.put<EmployeeDto>(`/employees/${id}`, request),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: [KEY] }),
  });
}

/** 軟刪除＝離職。 */
export function useDeleteEmployee() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: number) => apiClient.delete(`/employees/${id}`),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: [KEY] }),
  });
}
