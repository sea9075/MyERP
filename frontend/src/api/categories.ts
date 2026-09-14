import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { apiClient } from './client';
import type { CategoryDto, CreateCategoryRequest, UpdateCategoryRequest } from './types';

const KEY = 'categories';

async function fetchCategories(includeDeleted: boolean): Promise<CategoryDto[]> {
  const { data } = await apiClient.get<CategoryDto[]>('/categories', { params: { includeDeleted } });
  return data;
}

export function useCategories(includeDeleted: boolean) {
  return useQuery({
    queryKey: [KEY, includeDeleted],
    queryFn: () => fetchCategories(includeDeleted),
  });
}

export function useCreateCategory() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (request: CreateCategoryRequest) => apiClient.post<CategoryDto>('/categories', request),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: [KEY] }),
  });
}

export function useUpdateCategory() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, request }: { id: number; request: UpdateCategoryRequest }) =>
      apiClient.put<CategoryDto>(`/categories/${id}`, request),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: [KEY] }),
  });
}

export function useDeleteCategory() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: number) => apiClient.delete(`/categories/${id}`),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: [KEY] }),
  });
}
