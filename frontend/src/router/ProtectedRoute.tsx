import type { ReactNode } from 'react';
import { Navigate, Outlet, useLocation } from 'react-router-dom';
import type { Department } from '@/api/types';
import { useAuthStore } from '@/stores/authStore';

/** 沒登入就導回 /login，並記住原本想去的頁面，登入成功後可以導回去。 */
export function ProtectedRoute() {
  const isAuthenticated = useAuthStore((state) => state.isAuthenticated);
  const location = useLocation();

  if (!isAuthenticated) {
    return <Navigate to="/login" replace state={{ from: location }} />;
  }

  return <Outlet />;
}

/**
 * 限定部門的頁面（人資/薪資/權限系統新增）：登入了但部門不在 allowed 名單裡就顯示無權限，
 * 不整頁導走（跟原本 AdminOnlyRoute 的行為一致，只是從「二選一 Admin」改成「部門白名單」）。
 * 取代原本只認 Admin 的 AdminOnlyRoute。
 */
export function RequireDepartment({ allowed, children }: { allowed: Department[]; children: ReactNode }) {
  const role = useAuthStore((state) => state.role);

  if (!role || !allowed.includes(role)) {
    return (
      <div style={{ padding: 48, textAlign: 'center', color: 'rgba(0,0,0,0.45)' }}>
        <p style={{ fontSize: 16 }}>這個頁面你的部門沒有使用權限。</p>
      </div>
    );
  }

  return <>{children}</>;
}
