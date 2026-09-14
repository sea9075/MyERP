import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { apiClient } from './client';
import type { AdjustInventoryRequest, InventoryItemDto, InventoryTransactionDto } from './types';

async function fetchInventory(): Promise<InventoryItemDto[]> {
  const { data } = await apiClient.get<InventoryItemDto[]>('/inventory');
  return data;
}

export function useInventory() {
  return useQuery({
    queryKey: ['inventory'],
    queryFn: fetchInventory,
  });
}

async function fetchInventoryTransactions(productId: number): Promise<InventoryTransactionDto[]> {
  const { data } = await apiClient.get<InventoryTransactionDto[]>(`/inventory/${productId}/transactions`);
  return data;
}

export function useInventoryTransactions(productId: number | undefined) {
  return useQuery({
    queryKey: ['inventory', 'transactions', productId],
    queryFn: () => fetchInventoryTransactions(productId as number),
    enabled: productId !== undefined,
  });
}

export function useAdjustInventory() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (request: AdjustInventoryRequest) => apiClient.post<InventoryItemDto>('/inventory/adjust', request),
    onSuccess: (_response, variables) => {
      queryClient.invalidateQueries({ queryKey: ['inventory'] });
      queryClient.invalidateQueries({ queryKey: ['products'] });
      queryClient.invalidateQueries({ queryKey: ['inventory', 'transactions', variables.productId] });
    },
  });
}
