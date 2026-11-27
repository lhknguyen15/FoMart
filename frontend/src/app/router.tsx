import { createBrowserRouter } from 'react-router'
import { SalePage } from '../features/sales/pages/SalePage'
import { AppLayout } from './AppLayout'
import { NotFoundPage } from './NotFoundPage'

// Bảng route của ứng dụng. Mỗi route trỏ tới một <ChủĐề>Page trong features/<mảng>/pages.
export const router = createBrowserRouter([
  {
    path: '/',
    element: <AppLayout />,
    children: [
      { index: true, element: <SalePage /> },
      { path: '*', element: <NotFoundPage /> },
    ],
  },
])
