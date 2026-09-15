import { useQuery } from '@tanstack/react-query';
import { apiClient } from './client';
import type {
  GrossMarginGroupBy,
  GrossMarginReportDto,
  InventoryReportDto,
  PurchaseReportDto,
  SalesReportDto,
} from './types';

// 報表模組（2026-09-15 新增，見 ERP.md §4.6、Infra-Progress.md §27）。只有 Manager/Admin 能用，
// 後端也已經用 [Authorize(Roles="Manager,Admin")] 擋掉其他部門，見 ReportsController。

export interface PurchaseReportParams {
  dateFrom: string;
  dateTo: string;
  supplierId?: number;
  productId?: number;
  categoryId?: number;
}

export function usePurchaseReport(params: PurchaseReportParams) {
  return useQuery({
    queryKey: ['reports', 'purchases', params],
    queryFn: async () => {
      const { data } = await apiClient.get<PurchaseReportDto>('/reports/purchases', { params });
      return data;
    },
  });
}

export interface SalesReportParams {
  dateFrom: string;
  dateTo: string;
  customerId?: number;
  productId?: number;
  categoryId?: number;
}

export function useSalesReport(params: SalesReportParams) {
  return useQuery({
    queryKey: ['reports', 'sales', params],
    queryFn: async () => {
      const { data } = await apiClient.get<SalesReportDto>('/reports/sales', { params });
      return data;
    },
  });
}

export interface GrossMarginReportParams {
  dateFrom: string;
  dateTo: string;
  productId?: number;
  categoryId?: number;
  groupBy: GrossMarginGroupBy;
}

export function useGrossMarginReport(params: GrossMarginReportParams) {
  return useQuery({
    queryKey: ['reports', 'gross-margin', params],
    queryFn: async () => {
      const { data } = await apiClient.get<GrossMarginReportDto>('/reports/gross-margin', { params });
      return data;
    },
  });
}

export interface InventoryReportParams {
  categoryId?: number;
  lowStockOnly: boolean;
}

export function useInventoryReport(params: InventoryReportParams) {
  return useQuery({
    queryKey: ['reports', 'inventory', params],
    queryFn: async () => {
      const { data } = await apiClient.get<InventoryReportDto>('/reports/inventory', { params });
      return data;
    },
  });
}
