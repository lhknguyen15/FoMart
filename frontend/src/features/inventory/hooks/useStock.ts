import { keepPreviousData, useInfiniteQuery, useQuery } from '@tanstack/react-query'
import { getExpirySummary, getExpiringLots, getProductStock, getStockMoves, type StockExpiryFilter } from '../../../api/inventory'

// Query key của chủ đề Stock. Bán / nhập / hủy chứng từ làm mới ['stock'] nên mọi key ở đây tự cập nhật.
export const stockKeys = {
  all: ['stock'] as const,
  lots: (productId: number) => ['stock', 'lots', productId] as const,
  moves: (productId: number) => ['stock', 'moves', productId] as const,
  expiring: (filter: StockExpiryFilter) => ['stock', 'expiring', filter] as const,
  expirySummary: () => ['stock', 'expiry-summary'] as const,
}

// Tồn theo lô và vị trí của một sản phẩm (kèm cảnh báo hạn dùng).
export function useProductStock(productId: number) {
  return useQuery({
    queryKey: stockKeys.lots(productId),
    queryFn: ({ signal }) => getProductStock(productId, signal),
    enabled: productId > 0,
  })
}

// Thẻ kho (mới nhất trước), "Xem thêm" từng trang 30 dòng.
export function useStockMoves(productId: number) {
  return useInfiniteQuery({
    queryKey: stockKeys.moves(productId),
    queryFn: ({ pageParam, signal }) => getStockMoves(productId, pageParam, signal),
    initialPageParam: 1,
    getNextPageParam: (last) => (last.page < last.totalPages ? last.page + 1 : undefined),
    enabled: productId > 0,
  })
}

// Lô cần xử lý hạn dùng trên toàn tiệm (màn hình hạn dùng, UC-08), "Xem thêm" từng trang 50 lô.
export function useExpiringLots(filter: StockExpiryFilter) {
  return useInfiniteQuery({
    queryKey: stockKeys.expiring(filter),
    queryFn: ({ pageParam, signal }) => getExpiringLots(filter, pageParam, signal),
    initialPageParam: 1,
    getNextPageParam: (last) => (last.page < last.totalPages ? last.page + 1 : undefined),
    placeholderData: keepPreviousData,
  })
}

// Số lô / số lượng / giá trị đã hết hạn và sắp hết hạn (đầu trang Hạn dùng, dòng nhắc ở trang Tồn kho).
export function useExpirySummary() {
  return useQuery({
    queryKey: stockKeys.expirySummary(),
    queryFn: ({ signal }) => getExpirySummary(signal),
  })
}
