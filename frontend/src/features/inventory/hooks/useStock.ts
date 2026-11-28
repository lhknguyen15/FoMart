import { useInfiniteQuery, useQuery } from '@tanstack/react-query'
import { getProductStock, getStockMoves } from '../../../api/inventory'

// Query key của chủ đề Stock. Bán / nhập / hủy chứng từ làm mới ['stock'] nên mọi key ở đây tự cập nhật.
export const stockKeys = {
  all: ['stock'] as const,
  lots: (productId: number) => ['stock', 'lots', productId] as const,
  moves: (productId: number) => ['stock', 'moves', productId] as const,
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
