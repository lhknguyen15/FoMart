import { keepPreviousData, useInfiniteQuery, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { cancelSale, confirmSalePayment, createSale, getSale, searchSales, type CreateSaleRequest, type SaleSearchParams } from '../../../api/sales'
import { productKeys } from '../../catalog/hooks/useProducts'

// Query key của chủ đề Sale.
export const saleKeys = {
  all: ['sales'] as const,
  list: (params: SaleSearchParams) => ['sales', 'list', params] as const,
  history: (params: SaleSearchParams) => ['sales', 'history', params] as const,
  detail: (id: string) => ['sales', 'detail', id] as const,
}

// Danh sách hóa đơn có "Xem thêm" (SC-04): mỗi trang là một lần gọi API, TanStack Query nối các trang lại.
// Đổi bộ lọc = key mới = bắt đầu lại từ trang 1; giữ danh sách cũ trong lúc tải để màn hình không nháy.
export function useSaleHistory(params: Omit<SaleSearchParams, 'page'>) {
  return useInfiniteQuery({
    queryKey: saleKeys.history(params),
    queryFn: ({ pageParam, signal }) => searchSales({ ...params, page: pageParam }, signal),
    initialPageParam: 1,
    getNextPageParam: (last) => (last.page < last.totalPages ? last.page + 1 : undefined),
    placeholderData: keepPreviousData,
  })
}

// Chi tiết một hóa đơn kèm các lô đã trừ.
export function useSale(id: string) {
  return useQuery({
    queryKey: saleKeys.detail(id),
    queryFn: ({ signal }) => getSale(id, signal),
    enabled: id !== '',
  })
}

const PENDING: SaleSearchParams = { status: 'pending_payment', pageSize: 20 }

// Hóa đơn chờ chuyển khoản (giỏ đã giữ lại để bán tiếp khách khác, UC-05).
export function usePendingSales() {
  return useQuery({
    queryKey: saleKeys.list(PENDING),
    queryFn: ({ signal }) => searchSales(PENDING, signal),
    refetchInterval: 30_000,
  })
}

// Bán xong thì tồn kho, sản phẩm (cờ cần kiểm kê) và danh sách hóa đơn đều đổi: làm mới các key đó.
function useInvalidateAfterSale() {
  const queryClient = useQueryClient()
  return () =>
    Promise.all([
      queryClient.invalidateQueries({ queryKey: saleKeys.all }),
      queryClient.invalidateQueries({ queryKey: productKeys.all }),
      queryClient.invalidateQueries({ queryKey: ['stock'] }),
    ])
}

// Lưu hóa đơn: tiền mặt (trừ kho ngay) hoặc chuyển khoản (chờ "Đã nhận").
export function useCreateSale() {
  const invalidate = useInvalidateAfterSale()
  return useMutation({
    mutationFn: (body: CreateSaleRequest) => createSale(body),
    onSuccess: invalidate,
  })
}

// "Đã nhận" tiền chuyển khoản.
export function useConfirmSalePayment() {
  const invalidate = useInvalidateAfterSale()
  return useMutation({
    mutationFn: ({ id, confirmExpired }: { id: string; confirmExpired?: boolean }) => confirmSalePayment(id, confirmExpired),
    onSuccess: invalidate,
  })
}

// Bỏ hóa đơn chờ thanh toán (khách không chuyển).
export function useCancelSale() {
  const invalidate = useInvalidateAfterSale()
  return useMutation({
    mutationFn: ({ id, reason }: { id: string; reason: string }) => cancelSale(id, reason),
    onSuccess: invalidate,
  })
}
