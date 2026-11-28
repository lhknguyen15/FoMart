import type { ReactNode } from 'react'
import { Navigate, useLocation } from 'react-router'
import { useAuthStore } from '../stores/useAuthStore'

// Chặn các trang cần đăng nhập: chưa có phiên (hoặc phiên vừa bị thu hồi) thì sang /dang-nhap,
// nhớ trang đang định vào để đăng nhập xong quay lại đúng chỗ.
export function RequireAuth({ children }: { children: ReactNode }) {
  const session = useAuthStore((state) => state.session)
  const location = useLocation()
  if (!session) return <Navigate to="/dang-nhap" replace state={{ from: location.pathname }} />
  return children
}
