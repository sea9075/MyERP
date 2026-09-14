import type { ReactNode } from 'react';
import { Navigate, Outlet, useLocation } from 'react-router-dom';
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

/** 限 Admin 角色的頁面（目前只有操作紀錄查詢）：登入了但不是 Admin 就顯示無權限，不整頁導走。 */
export function AdminOnlyRoute({ children }: { children: ReactNode }) {
  const role = useAuthStore((state) => state.role);

  if (role !== 'Admin') {
    return (
      <div style={{ padding: 48, textAlign: 'center', color: 'rgba(0,0,0,0.45)' }}>
        <p style={{ fontSize: 16 }}>這個頁面僅限管理員（Admin）使用。</p>
      </div>
    );
  }

  return <>{children}</>;
}
