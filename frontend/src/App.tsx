import { Navigate, Route, Routes } from 'react-router-dom';
import { AppLayout } from '@/components/layout/AppLayout';
import { ProtectedRoute, RequireDepartment } from '@/router/ProtectedRoute';
import { ActivityLogsPage } from '@/pages/ActivityLogsPage';
import { AttendanceManagementPage } from '@/pages/AttendanceManagementPage';
import { CategoriesPage } from '@/pages/CategoriesPage';
import { CustomersPage } from '@/pages/CustomersPage';
import { DashboardPage } from '@/pages/DashboardPage';
import { EmployeesPage } from '@/pages/EmployeesPage';
import { InventoryPage } from '@/pages/InventoryPage';
import { InventoryTransactionsPage } from '@/pages/InventoryTransactionsPage';
import { LoginPage } from '@/pages/LoginPage';
import { MyAttendancePage } from '@/pages/MyAttendancePage';
import { NotFoundPage } from '@/pages/NotFoundPage';
import { PayrollPage } from '@/pages/PayrollPage';
import { ProductsPage } from '@/pages/ProductsPage';
import { PurchaseOrderFormPage } from '@/pages/PurchaseOrderFormPage';
import { PurchaseOrdersPage } from '@/pages/PurchaseOrdersPage';
import { ReportsPage } from '@/pages/ReportsPage';
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

          {/* 既有 ERP 模組：後端也已限定只有 Product/Manager/Admin 能呼叫，HR 部門直接打 API 會拿到 403。 */}
          <Route
            path="/products"
            element={
              <RequireDepartment allowed={['Product', 'Manager', 'Admin']}>
                <ProductsPage />
              </RequireDepartment>
            }
          />
          <Route
            path="/categories"
            element={
              <RequireDepartment allowed={['Product', 'Manager', 'Admin']}>
                <CategoriesPage />
              </RequireDepartment>
            }
          />
          <Route
            path="/suppliers"
            element={
              <RequireDepartment allowed={['Product', 'Manager', 'Admin']}>
                <SuppliersPage />
              </RequireDepartment>
            }
          />
          <Route
            path="/purchase-orders"
            element={
              <RequireDepartment allowed={['Product', 'Manager', 'Admin']}>
                <PurchaseOrdersPage />
              </RequireDepartment>
            }
          />
          <Route
            path="/purchase-orders/new"
            element={
              <RequireDepartment allowed={['Product', 'Manager', 'Admin']}>
                <PurchaseOrderFormPage />
              </RequireDepartment>
            }
          />
          <Route
            path="/sales-orders"
            element={
              <RequireDepartment allowed={['Product', 'Manager', 'Admin', 'Support']}>
                <SalesOrdersPage />
              </RequireDepartment>
            }
          />
          {/* 新增出貨單：原本只有 Support（客服部門）能用，2026-09-15 使用者要求追加開放給
              Manager/Admin（系統裡 Manager/Admin 權限一直保持完全相同，這次比照辦理）；
              Product 部門維持唯讀查詢。 */}
          <Route
            path="/sales-orders/new"
            element={
              <RequireDepartment allowed={['Support', 'Manager', 'Admin']}>
                <SalesOrderFormPage />
              </RequireDepartment>
            }
          />
          {/* 客戶管理：Support（客服部）的核心工作範圍之一，跟出貨單一起用；商品部（Product）不需要。 */}
          <Route
            path="/customers"
            element={
              <RequireDepartment allowed={['Manager', 'Admin', 'Support']}>
                <CustomersPage />
              </RequireDepartment>
            }
          />
          <Route
            path="/inventory"
            element={
              <RequireDepartment allowed={['Product', 'Manager', 'Admin']}>
                <InventoryPage />
              </RequireDepartment>
            }
          />
          <Route
            path="/inventory/:productId/transactions"
            element={
              <RequireDepartment allowed={['Product', 'Manager', 'Admin']}>
                <InventoryTransactionsPage />
              </RequireDepartment>
            }
          />

          {/* 打卡「我的出勤」：不分部門，任何登入使用者都能用，不套 RequireDepartment。 */}
          <Route path="/my-attendance" element={<MyAttendancePage />} />

          {/* 人資／薪資系統：只有 HR/Manager/Admin 能用。 */}
          <Route
            path="/employees"
            element={
              <RequireDepartment allowed={['HR', 'Manager', 'Admin']}>
                <EmployeesPage />
              </RequireDepartment>
            }
          />
          <Route
            path="/attendance/manage"
            element={
              <RequireDepartment allowed={['HR', 'Manager', 'Admin']}>
                <AttendanceManagementPage />
              </RequireDepartment>
            }
          />
          <Route
            path="/payroll"
            element={
              <RequireDepartment allowed={['HR', 'Manager', 'Admin']}>
                <PayrollPage />
              </RequireDepartment>
            }
          />

          <Route
            path="/activity-logs"
            element={
              <RequireDepartment allowed={['Manager', 'Admin']}>
                <ActivityLogsPage />
              </RequireDepartment>
            }
          />

          {/* 報表模組（ERP.md §4.6 Phase 3，Infra-Progress.md §27，2026-09-15 新增）：依使用者決定，
              只開放 Manager/Admin，跟操作紀錄同樣的權限收斂。 */}
          <Route
            path="/reports"
            element={
              <RequireDepartment allowed={['Manager', 'Admin']}>
                <ReportsPage />
              </RequireDepartment>
            }
          />
        </Route>
      </Route>

      <Route path="/404" element={<NotFoundPage />} />
      <Route path="*" element={<Navigate to="/404" replace />} />
    </Routes>
  );
}
