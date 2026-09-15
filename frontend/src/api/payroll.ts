import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { apiClient } from './client';
import type { CalculatePayrollRequest, PayrollRecordDto, UpdatePayrollBonusRequest } from './types';

const KEY = 'payroll';

export interface PayrollSearchParams {
  employeeId?: number;
  periodMonth?: string;
  includeDeleted: boolean;
}

async function searchPayroll(params: PayrollSearchParams): Promise<PayrollRecordDto[]> {
  const { data } = await apiClient.get<PayrollRecordDto[]>('/payroll', { params });
  return data;
}

export function usePayrollRecords(params: PayrollSearchParams, enabled = true) {
  return useQuery({
    queryKey: [KEY, params],
    queryFn: () => searchPayroll(params),
    enabled,
  });
}

/** 依當月出勤紀錄計算薪資；如果該員工、該月份已經算過，後端會重算一次。 */
export function useCalculatePayroll() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (request: CalculatePayrollRequest) => apiClient.post<PayrollRecordDto>('/payroll/calculate', request),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: [KEY] }),
  });
}

export function useUpdatePayrollBonus() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, request }: { id: number; request: UpdatePayrollBonusRequest }) =>
      apiClient.put<PayrollRecordDto>(`/payroll/${id}/bonus`, request),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: [KEY] }),
  });
}

export function useDeletePayrollRecord() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: number) => apiClient.delete(`/payroll/${id}`),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: [KEY] }),
  });
}
