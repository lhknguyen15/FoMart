import { createBrowserRouter } from 'react-router'
import { LoginPage } from '../features/auth/pages/LoginPage'
import { SalePage } from '../features/sales/pages/SalePage'
import { ScanTestPage } from '../features/sales/pages/ScanTestPage'
import { AppLayout } from './AppLayout'
import { NotFoundPage } from './NotFoundPage'
import { RequireAuth } from './RequireAuth'

// Bảng route của ứng dụng. Mỗi route trỏ tới một <ChủĐề>Page trong features/<mảng>/pages.
// Mọi trang trong AppLayout đều cần đăng nhập (RequireAuth); /dang-nhap đứng riêng.
export const router = createBrowserRouter([
  { path: '/dang-nhap', element: <LoginPage /> },
  {
    path: '/',
    element: (
      <RequireAuth>
        <AppLayout />
      </RequireAuth>
    ),
    children: [
      { index: true, element: <SalePage /> },
      // Tạm thời: trang thử quét mã (ADR-0003), bỏ khi màn hình bán hàng có component quét chính thức.
      { path: 'thu-quet', element: <ScanTestPage /> },
      { path: '*', element: <NotFoundPage /> },
    ],
  },
])
