import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { apiClient } from './client';
import type { CreateCustomerRequest, CustomerDto, UpdateCustomerRequest } from './types';

const KEY = 'customers';

async function fetchCustomers(includeDeleted: boolean): Promise<CustomerDto[]> {
  const { data } = await apiClient.get<CustomerDto[]>('/customers', { params: { includeDeleted } });
  return data;
}

export function useCustomers(includeDeleted: boolean) {
  return useQuery({
    queryKey: [KEY, includeDeleted],
    queryFn: () => fetchCustomers(includeDeleted),
  });
}

/** 給出貨單選客戶用：只需要「未刪除」的客戶，且出貨單本身可以不選客戶（一般散客）。 */
export function useActiveCustomers() {
  return useCustomers(false);
}

export function useCreateCustomer() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (request: CreateCustomerRequest) => apiClient.post<CustomerDto>('/customers', request),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: [KEY] }),
  });
}

export function useUpdateCustomer() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, request }: { id: number; request: UpdateCustomerRequest }) =>
      apiClient.put<CustomerDto>(`/customers/${id}`, request),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: [KEY] }),
  });
}

export function useDeleteCustomer() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: number) => apiClient.delete(`/customers/${id}`),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: [KEY] }),
  });
}
