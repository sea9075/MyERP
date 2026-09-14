import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { apiClient, extractErrorMessage } from './client';
import type { CreateProductRequest, ProductDto, UpdateProductRequest } from './types';

const KEY = 'products';

export interface ProductSearchParams {
  keyword?: string;
  categoryId?: number;
  lowStock?: boolean;
  includeDeleted: boolean;
}

async function searchProducts(params: ProductSearchParams): Promise<ProductDto[]> {
  const { data } = await apiClient.get<ProductDto[]>('/products', { params });
  return data;
}

export function useProducts(params: ProductSearchParams) {
  return useQuery({
    queryKey: [KEY, params],
    queryFn: () => searchProducts(params),
  });
}

/** 給進貨單/出貨單挑商品的下拉選單用：只要「未刪除」的全部商品。 */
export function useActiveProducts() {
  return useProducts({ includeDeleted: false });
}

async function fetchProductById(id: number): Promise<ProductDto> {
  const { data } = await apiClient.get<ProductDto>(`/products/${id}`);
  return data;
}

/** 給庫存異動明細頁這種「已經知道 productId、只是要顯示商品名稱/SKU」的地方用。 */
export function useProduct(id: number | undefined) {
  return useQuery({
    queryKey: [KEY, 'detail', id],
    queryFn: () => fetchProductById(id as number),
    enabled: id !== undefined,
  });
}

export function useCreateProduct() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (request: CreateProductRequest) => apiClient.post<ProductDto>('/products', request),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: [KEY] }),
  });
}

export function useUpdateProduct() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, request }: { id: number; request: UpdateProductRequest }) =>
      apiClient.put<ProductDto>(`/products/${id}`, request),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: [KEY] }),
  });
}

export function useDeleteProduct() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: number) => apiClient.delete(`/products/${id}`),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: [KEY] }),
  });
}

/**
 * 條碼掃描查詢（USB 掃碼槍模擬鍵盤輸入＋Enter，見 ERP.md §8 Phase 2 項目 10）。
 * 找不到條碼時後端回 400（BusinessRuleException），這裡用 try/catch 轉成 null，
 * 讓呼叫端可以簡單判斷「有沒有掃到商品」，不用處理例外。
 */
export async function findProductByBarcode(barcode: string): Promise<{ product: ProductDto | null; error: string | null }> {
  try {
    const { data } = await apiClient.get<ProductDto>(`/products/by-barcode/${encodeURIComponent(barcode)}`);
    return { product: data, error: null };
  } catch (error) {
    return { product: null, error: extractErrorMessage(error, '查無此條碼對應的商品。') };
  }
}
