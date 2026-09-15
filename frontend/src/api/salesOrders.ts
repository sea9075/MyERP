import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { apiClient } from './client';
import type { CreateSalesOrderRequest, SalesOrderDto } from './types';

const KEY = 'sales-orders';

// 出貨單新增／作廢只開放給 Support（客服部門），後端 SalesOrdersController 的 Create／Void
// 端點也額外疊了一層 [Authorize(Roles = "Support")]；Product/Manager/Admin 呼叫這兩個 mutation
// 會被後端擋下來（403），前端也只在 role === 'Support' 時才會顯示對應的按鈕（見 SalesOrdersPage）。

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
      // 出貨會改庫存，順便讓庫存頁的快取失效，下次切過去看到的是最新庫存。
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
