import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  addReceiptLine,
  cancelGoodsReceipt,
  completeGoodsReceipt,
  createGoodsReceipt,
  deleteGoodsReceipt,
  deleteReceiptLine,
  getGoodsReceipt,
  getLastPurchasePrice,
  searchGoodsReceipts,
  updateGoodsReceipt,
  updateReceiptLine,
  type CreateGoodsReceiptRequest,
  type GoodsReceiptResponse,
  type GoodsReceiptSearchParams,
  type ReceiptLineRequest,
  type UpdateGoodsReceiptRequest,
} from '../../../api/purchasing'
import { productKeys } from '../../catalog/hooks/useProducts'

// Query key của chủ đề GoodsReceipt.
export const receiptKeys = {
  all: ['goods-receipts'] as const,
  lists: () => [...receiptKeys.all, 'list'] as const,
  list: (params: GoodsReceiptSearchParams) => [...receiptKeys.lists(), params] as const,
  detail: (id: number) => [...receiptKeys.all, 'detail', id] as const,
  lastPrice: (unitId: number) => [...receiptKeys.all, 'last-price', unitId] as const,
}

// Danh sách phiếu nhập (lọc theo trạng thái).
export function useReceiptList(params: GoodsReceiptSearchParams) {
  return useQuery({
    queryKey: receiptKeys.list(params),
    queryFn: ({ signal }) => searchGoodsReceipts(params, signal),
  })
}

// Chi tiết một phiếu nhập kèm các dòng.
export function useReceipt(id: number) {
  return useQuery({
    queryKey: receiptKeys.detail(id),
    queryFn: ({ signal }) => getGoodsReceipt(id, signal),
    enabled: Number.isInteger(id) && id > 0,
  })
}

// Giá nhập lần trước để gợi ý (UC-04 bước 3). Không bật khi người xem bị ẩn giá (BR-08).
// 404 (chưa nhập lần nào) là kết quả bình thường: không thử lại (cấu hình chung bỏ qua lỗi 4xx).
export function useLastPrice(unitId: number | null, enabled: boolean) {
  return useQuery({
    queryKey: receiptKeys.lastPrice(unitId ?? 0),
    queryFn: ({ signal }) => getLastPurchasePrice(unitId ?? 0, signal),
    enabled: enabled && unitId !== null,
    staleTime: 5 * 60_000,
  })
}

// Mọi thao tác trên phiếu đều trả lại cả phiếu: ghi thẳng vào cache chi tiết (khỏi tải lại),
// rồi đánh dấu danh sách là cũ vì tổng tiền, số dòng, trạng thái có thể đã đổi.
function useApplyReceipt() {
  const queryClient = useQueryClient()
  return (receipt: GoodsReceiptResponse) => {
    queryClient.setQueryData(receiptKeys.detail(receipt.id), receipt)
    return queryClient.invalidateQueries({ queryKey: receiptKeys.lists() })
  }
}

// Tạo phiếu nháp mới.
export function useCreateReceipt() {
  const apply = useApplyReceipt()
  return useMutation({
    mutationFn: (body: CreateGoodsReceiptRequest) => createGoodsReceipt(body),
    onSuccess: apply,
  })
}

// Sửa đầu phiếu (nhà cung cấp, vị trí nhận, ghi chú).
export function useUpdateReceipt(id: number) {
  const apply = useApplyReceipt()
  return useMutation({
    mutationFn: (body: UpdateGoodsReceiptRequest) => updateGoodsReceipt(id, body),
    onSuccess: apply,
  })
}

// Xóa phiếu nháp.
export function useDeleteReceipt(id: number) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: () => deleteGoodsReceipt(id),
    onSuccess: () => {
      queryClient.removeQueries({ queryKey: receiptKeys.detail(id) })
      return queryClient.invalidateQueries({ queryKey: receiptKeys.lists() })
    },
  })
}

// Thêm dòng nhập.
export function useAddReceiptLine(id: number) {
  const apply = useApplyReceipt()
  return useMutation({
    mutationFn: (body: ReceiptLineRequest) => addReceiptLine(id, body),
    onSuccess: apply,
  })
}

// Sửa dòng nhập.
export function useUpdateReceiptLine(id: number) {
  const apply = useApplyReceipt()
  return useMutation({
    mutationFn: ({ lineId, body }: { lineId: number; body: ReceiptLineRequest }) => updateReceiptLine(id, lineId, body),
    onSuccess: apply,
  })
}

// Xóa dòng nhập, cập nhật lạc quan: dòng biến mất ngay, lỗi thì trả lại như cũ.
export function useDeleteReceiptLine(id: number) {
  const queryClient = useQueryClient()
  const apply = useApplyReceipt()
  const key = receiptKeys.detail(id)
  return useMutation({
    mutationFn: (lineId: number) => deleteReceiptLine(id, lineId),
    onMutate: async (lineId) => {
      await queryClient.cancelQueries({ queryKey: key }) // không để lần tải đang chạy ghi đè lên bản lạc quan
      const previous = queryClient.getQueryData<GoodsReceiptResponse>(key)
      if (previous) queryClient.setQueryData(key, { ...previous, lines: previous.lines.filter((line) => line.id !== lineId) })
      return { previous }
    },
    onError: (_error, _lineId, context) => {
      if (context?.previous) queryClient.setQueryData(key, context.previous)
    },
    onSuccess: apply, // bản thật từ server: tổng tiền và số dòng thiếu giá tính lại
  })
}

// Hoàn tất và hủy đổi tồn kho: làm mới cả tồn, sản phẩm (tổng tồn) lẫn phiếu.
function useApplyStockChange() {
  const queryClient = useQueryClient()
  const apply = useApplyReceipt()
  return (receipt: GoodsReceiptResponse) =>
    Promise.all([apply(receipt), queryClient.invalidateQueries({ queryKey: productKeys.all }), queryClient.invalidateQueries({ queryKey: ['stock'] })])
}

// Hoàn tất phiếu: tạo lô, ghi sổ kho, cộng tồn.
export function useCompleteReceipt(id: number) {
  const apply = useApplyStockChange()
  return useMutation({
    mutationFn: (confirmExpiryWarning: boolean) => completeGoodsReceipt(id, confirmExpiryWarning),
    onSuccess: apply,
  })
}

// Chủ tiệm hủy phiếu đã hoàn tất (chứng từ đảo ngược).
export function useCancelReceipt(id: number) {
  const apply = useApplyStockChange()
  return useMutation({
    mutationFn: (reason: string) => cancelGoodsReceipt(id, reason),
    onSuccess: apply,
  })
}
