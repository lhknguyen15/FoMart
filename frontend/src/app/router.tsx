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
      { path: 'hoa-don', lazy: async () => ({ Component: (await import('../features/sales/pages/SaleListPage')).SaleListPage }) },
      { path: 'hoa-don/:id', lazy: async () => ({ Component: (await import('../features/sales/pages/SaleDetailPage')).SaleDetailPage }) },
      { path: 'ton-kho', lazy: async () => ({ Component: (await import('../features/inventory/pages/StockListPage')).StockListPage }) },
      { path: 'ton-kho/:productId', lazy: async () => ({ Component: (await import('../features/inventory/pages/StockDetailPage')).StockDetailPage }) },
      { path: 'xuat-huy', lazy: async () => ({ Component: (await import('../features/inventory/pages/DisposalListPage')).DisposalListPage }) },
      { path: 'xuat-huy/moi', lazy: async () => ({ Component: (await import('../features/inventory/pages/DisposalCreatePage')).DisposalCreatePage }) },
      { path: 'xuat-huy/:id', lazy: async () => ({ Component: (await import('../features/inventory/pages/DisposalDetailPage')).DisposalDetailPage }) },
      { path: 'han-dung', lazy: async () => ({ Component: (await import('../features/inventory/pages/StockExpiryPage')).StockExpiryPage }) },
      // Nhập hàng tải lười theo route: màn hình bán hàng (mở nhiều nhất) không phải tải phần này.
      { path: 'nhap-hang', lazy: async () => ({ Component: (await import('../features/purchasing/pages/ReceiptListPage')).ReceiptListPage }) },
      { path: 'nhap-hang/:id', lazy: async () => ({ Component: (await import('../features/purchasing/pages/ReceiptPage')).ReceiptPage }) },
      { path: '*', element: <NotFoundPage /> },
    ],
  },
])
