import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { apiClient } from './client';
import type { CreateSalesOrderRequest, SalesOrderDto } from './types';

const KEY = 'sales-orders';

export interface SalesOrderSearchParams {
  dateFrom?: string;
  dateTo?: string;
  customerId?: number;
}

async function searchSalesOrders(params: SalesOrderSearchParams): Promise<SalesOrderDto[]> {
  const { data } = await apiClient.get<SalesOrderDto[]>('/sales-orders', { params });
  return data;
}

export function useSalesOrders(params: SalesOrderSearchParams) {
  return useQuery({
    queryKey: [KEY, params],
    queryFn: () => searchSalesOrders(params),
  });
}

export function useCreateSalesOrder() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (request: CreateSalesOrderRequest) => apiClient.post<SalesOrderDto>('/sales-orders', request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: [KEY] });
      queryClient.invalidateQueries({ queryKey: ['inventory'] });
      queryClient.invalidateQueries({ queryKey: ['products'] });
    },
  });
}

export function useVoidSalesOrder() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: number) => apiClient.post<SalesOrderDto>(`/sales-orders/${id}/void`),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: [KEY] });
      queryClient.invalidateQueries({ queryKey: ['inventory'] });
      queryClient.invalidateQueries({ queryKey: ['products'] });
    },
  });
}
