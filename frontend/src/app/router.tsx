import { createBrowserRouter } from 'react-router'
import { SalePage } from '../features/sales/pages/SalePage'
import { ScanTestPage } from '../features/sales/pages/ScanTestPage'
import { AppLayout } from './AppLayout'
import { NotFoundPage } from './NotFoundPage'

// Bảng route của ứng dụng. Mỗi route trỏ tới một <ChủĐề>Page trong features/<mảng>/pages.
export const router = createBrowserRouter([
  {
    path: '/',
    element: <AppLayout />,
    children: [
      { index: true, element: <SalePage /> },
      // Tạm thời: thử quét mã (ADR-0003), bỏ khi đã chốt thư viện.
      { path: 'thu-quet', element: <ScanTestPage /> },
      { path: '*', element: <NotFoundPage /> },
    ],
  },
])
