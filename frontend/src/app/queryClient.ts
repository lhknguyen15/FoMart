import { QueryClient } from '@tanstack/react-query'
import { ApiError } from '../api/types'

// Cấu hình chung của TanStack Query cho cả ứng dụng.
export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      // Dữ liệu được coi là "tươi" 30 giây: quay lại màn hình trong khoảng này không tải lại.
      staleTime: 30_000,
      // Chỉ thử lại lỗi mạng / lỗi máy chủ (tối đa 2 lần); lỗi 4xx là lỗi thật, thử lại cũng vậy.
      retry: (failureCount, error) =>
        failureCount < 2 && (!(error instanceof ApiError) || error.isNetworkError || error.status >= 500),
    },
    mutations: {
      // Ghi dữ liệu (bán, nhập) không tự thử lại: tránh ghi hai lần. Hóa đơn tự chống trùng bằng id UUID v7.
      retry: false,
    },
  },
})
