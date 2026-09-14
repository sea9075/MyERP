import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { apiClient } from './client';
import type { CreateSupplierRequest, SupplierDto, UpdateSupplierRequest } from './types';

const KEY = 'suppliers';

async function fetchSuppliers(includeDeleted: boolean): Promise<SupplierDto[]> {
  const { data } = await apiClient.get<SupplierDto[]>('/suppliers', { params: { includeDeleted } });
  return data;
}

export function useSuppliers(includeDeleted: boolean) {
  return useQuery({
    queryKey: [KEY, includeDeleted],
    queryFn: () => fetchSuppliers(includeDeleted),
  });
}

/** 給下拉選單用：商品/進貨單選供應商時，只需要「未刪除」的供應商。 */
export function useActiveSuppliers() {
  return useSuppliers(false);
}

export function useCreateSupplier() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (request: CreateSupplierRequest) => apiClient.post<SupplierDto>('/suppliers', request),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: [KEY] }),
  });
}

export function useUpdateSupplier() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, request }: { id: number; request: UpdateSupplierRequest }) =>
      apiClient.put<SupplierDto>(`/suppliers/${id}`, request),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: [KEY] }),
  });
}

export function useDeleteSupplier() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: number) => apiClient.delete(`/suppliers/${id}`),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: [KEY] }),
  });
}
