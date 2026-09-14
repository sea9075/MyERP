import { Navigate, Route, Routes } from 'react-router-dom';
import { AppLayout } from '@/components/layout/AppLayout';
import { AdminOnlyRoute, ProtectedRoute } from '@/router/ProtectedRoute';
import { ActivityLogsPage } from '@/pages/ActivityLogsPage';
import { CategoriesPage } from '@/pages/CategoriesPage';
import { CustomersPage } from '@/pages/CustomersPage';
import { DashboardPage } from '@/pages/DashboardPage';
import { InventoryPage } from '@/pages/InventoryPage';
import { InventoryTransactionsPage } from '@/pages/InventoryTransactionsPage';
import { LoginPage } from '@/pages/LoginPage';
import { NotFoundPage } from '@/pages/NotFoundPage';
import { ProductsPage } from '@/pages/ProductsPage';
import { PurchaseOrderFormPage } from '@/pages/PurchaseOrderFormPage';
import { PurchaseOrdersPage } from '@/pages/PurchaseOrdersPage';
import { SalesOrderFormPage } from '@/pages/SalesOrderFormPage';
import { SalesOrdersPage } from '@/pages/SalesOrdersPage';
import { SuppliersPage } from '@/pages/SuppliersPage';

export function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />

      <Route element={<ProtectedRoute />}>
        <Route element={<AppLayout />}>
          <Route path="/" element={<DashboardPage />} />
          <Route path="/products" element={<ProductsPage />} />
          <Route path="/categories" element={<CategoriesPage />} />
          <Route path="/suppliers" element={<SuppliersPage />} />
          <Route path="/customers" element={<CustomersPage />} />
          <Route path="/purchase-orders" element={<PurchaseOrdersPage />} />
          <Route path="/purchase-orders/new" element={<PurchaseOrderFormPage />} />
          <Route path="/sales-orders" element={<SalesOrdersPage />} />
          <Route path="/sales-orders/new" element={<SalesOrderFormPage />} />
          <Route path="/inventory" element={<InventoryPage />} />
          <Route path="/inventory/:productId/transactions" element={<InventoryTransactionsPage />} />
          <Route
            path="/activity-logs"
            element={
              <AdminOnlyRoute>
                <ActivityLogsPage />
              </AdminOnlyRoute>
            }
          />
        </Route>
      </Route>

      <Route path="/404" element={<NotFoundPage />} />
      <Route path="*" element={<Navigate to="/404" replace />} />
    </Routes>
  );
}
