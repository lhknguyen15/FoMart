import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useCallback } from 'react'
import { createProduct, getProduct, getProductByBarcode, searchProducts, type CreateProductRequest, type ProductSearchParams } from '../../../api/catalog'
import { normalizeBarcode } from '../../../lib/barcode'

// Query key của chủ đề Product: mọi nơi đọc / làm mới sản phẩm dùng đúng các key này.
export const productKeys = {
  all: ['products'] as const,
  list: (params: ProductSearchParams) => ['products', 'list', params] as const,
  detail: (id: number) => ['products', 'detail', id] as const,
  barcode: (code: string) => ['products', 'barcode', code] as const,
}

// Tìm sản phẩm khi gõ (từ 2 ký tự). Giữ kết quả cũ trong lúc tải kết quả mới để danh sách không nhấp nháy.
export function useProductSearch(keyword: string) {
  const trimmed = keyword.trim()
  const params: ProductSearchParams = { keyword: trimmed, status: 'active', pageSize: 8 }
  return useQuery({
    queryKey: productKeys.list(params),
    queryFn: ({ signal }) => searchProducts(params, signal),
    enabled: trimmed.length >= 2,
    placeholderData: keepPreviousData,
  })
}

// Hàm tra mã vạch dùng trong handler (quét được mã): lấy từ cache nếu vừa tra, không thì gọi API.
// Mã được chuẩn hóa UPC-A → EAN-13 trước khi tra (ADR-0003).
export function useBarcodeLookup() {
  const queryClient = useQueryClient()
  return useCallback(
    (raw: string) => {
      const code = normalizeBarcode(raw)
      return queryClient.fetchQuery({
        queryKey: productKeys.barcode(code),
        queryFn: ({ signal }) => getProductByBarcode(code, signal),
        staleTime: 60_000,
      })
    },
    [queryClient],
  )
}

// Hàm lấy chi tiết sản phẩm (các đơn vị) dùng trong handler, ví dụ khi chọn từ kết quả tìm.
export function useProductLoader() {
  const queryClient = useQueryClient()
  return useCallback(
    (id: number) =>
      queryClient.fetchQuery({
        queryKey: productKeys.detail(id),
        queryFn: ({ signal }) => getProduct(id, signal),
        staleTime: 60_000,
      }),
    [queryClient],
  )
}

// Tạo sản phẩm (chủ tiệm). Xong thì ghi chi tiết vào cache và bỏ các kết quả tìm / tra mã cũ
// (mã vừa tạo trước đó tra ra 404 vẫn nằm trong cache).
export function useCreateProduct() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (body: CreateProductRequest) => createProduct(body),
    onSuccess: (product) => {
      queryClient.setQueryData(productKeys.detail(product.id), product)
      return queryClient.invalidateQueries({ queryKey: productKeys.all })
    },
  })
}
