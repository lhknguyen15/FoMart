import { createBrowserRouter } from 'react-router'
import { LoginPage } from '../features/auth/pages/LoginPage'
import { SalePage } from '../features/sales/pages/SalePage'
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
      // Nhập hàng tải lười theo route: màn hình bán hàng (mở nhiều nhất) không phải tải phần này.
      { path: 'nhap-hang', lazy: async () => ({ Component: (await import('../features/purchasing/pages/ReceiptListPage')).ReceiptListPage }) },
      { path: 'nhap-hang/:id', lazy: async () => ({ Component: (await import('../features/purchasing/pages/ReceiptPage')).ReceiptPage }) },
      { path: '*', element: <NotFoundPage /> },
    ],
  },
])
