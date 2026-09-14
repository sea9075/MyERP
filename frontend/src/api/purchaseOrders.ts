import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { apiClient } from './client';
import type { CreatePurchaseOrderRequest, PurchaseOrderDto } from './types';

const KEY = 'purchase-orders';

export interface PurchaseOrderSearchParams {
  dateFrom?: string;
  dateTo?: string;
  supplierId?: number;
}

async function searchPurchaseOrders(params: PurchaseOrderSearchParams): Promise<PurchaseOrderDto[]> {
  const { data } = await apiClient.get<PurchaseOrderDto[]>('/purchase-orders', { params });
  return data;
}

export function usePurchaseOrders(params: PurchaseOrderSearchParams) {
  return useQuery({
    queryKey: [KEY, params],
    queryFn: () => searchPurchaseOrders(params),
  });
}

export function useCreatePurchaseOrder() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (request: CreatePurchaseOrderRequest) => apiClient.post<PurchaseOrderDto>('/purchase-orders', request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: [KEY] });
      // 進貨會改庫存，順便讓庫存頁的快取失效，下次切過去看到的是最新庫存。
      queryClient.invalidateQueries({ queryKey: ['inventory'] });
      queryClient.invalidateQueries({ queryKey: ['products'] });
    },
  });
}

export function useVoidPurchaseOrder() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: number) => apiClient.post<PurchaseOrderDto>(`/purchase-orders/${id}/void`),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: [KEY] });
      queryClient.invalidateQueries({ queryKey: ['inventory'] });
      queryClient.invalidateQueries({ queryKey: ['products'] });
    },
  });
}
