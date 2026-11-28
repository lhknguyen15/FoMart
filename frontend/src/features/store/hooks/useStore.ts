import { useQuery } from '@tanstack/react-query'
import { getStore, getStorePayment } from '../../../api/store'

// Query key của chủ đề Store: mọi nơi đọc/làm mới thông tin cửa hàng dùng đúng key này.
export const storeKeys = {
  all: ['store'] as const,
  payment: () => [...storeKeys.all, 'payment'] as const,
}

// Thông tin cửa hàng (tên, địa chỉ, có tài khoản VietQR chưa). Ít đổi nên giữ "tươi" 5 phút.
export function useStoreSettings() {
  return useQuery({
    queryKey: storeKeys.all,
    queryFn: ({ signal }) => getStore(signal),
    staleTime: 5 * 60_000,
  })
}

// Tài khoản nhận chuyển khoản để vẽ mã VietQR (UC-05). Chỉ tải khi cần (enabled), giữ "tươi" 5 phút.
export function useStorePayment(enabled = true) {
  return useQuery({
    queryKey: storeKeys.payment(),
    queryFn: ({ signal }) => getStorePayment(signal),
    staleTime: 5 * 60_000,
    enabled,
  })
}
