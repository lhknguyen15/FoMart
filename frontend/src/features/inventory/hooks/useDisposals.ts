import { useInfiniteQuery, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  approveDisposal,
  createDisposal,
  getDisposal,
  rejectDisposal,
  searchDisposals,
  type CreateDisposalRequest,
  type DisposalResponse,
  type DisposalStatus,
} from '../../../api/inventory'
import { productKeys } from '../../catalog/hooks/useProducts'
import { stockKeys } from './useStock'

// Query key của chủ đề Disposal.
export const disposalKeys = {
  all: ['disposals'] as const,
  lists: () => [...disposalKeys.all, 'list'] as const,
  list: (status: DisposalStatus | undefined) => [...disposalKeys.lists(), status ?? 'all'] as const,
  detail: (id: number) => [...disposalKeys.all, 'detail', id] as const,
}

// Danh sách phiếu hủy theo trạng thái, "Xem thêm" từng trang.
export function useDisposalList(status: DisposalStatus | undefined) {
  return useInfiniteQuery({
    queryKey: disposalKeys.list(status),
    queryFn: ({ pageParam, signal }) => searchDisposals(status, pageParam, signal),
    initialPageParam: 1,
    getNextPageParam: (last) => (last.page < last.totalPages ? last.page + 1 : undefined),
  })
}

// Số phiếu chờ duyệt (nhắc chủ tiệm ở trang Tồn kho / Hạn dùng).
export function usePendingDisposalCount() {
  return useQuery({
    queryKey: [...disposalKeys.list('pending'), 'count'] as const,
    queryFn: ({ signal }) => searchDisposals('pending', 1, signal),
    select: (page) => page.totalCount,
  })
}

// Chi tiết một phiếu hủy.
export function useDisposal(id: number) {
  return useQuery({
    queryKey: disposalKeys.detail(id),
    queryFn: ({ signal }) => getDisposal(id, signal),
    enabled: Number.isInteger(id) && id > 0,
  })
}

// Phiếu được duyệt thì tồn đổi: làm mới phiếu, tồn (lô, thẻ kho, hạn dùng) và sản phẩm (tổng tồn).
function useApplyDisposal() {
  const queryClient = useQueryClient()
  return (disposal: DisposalResponse) => {
    queryClient.setQueryData(disposalKeys.detail(disposal.id), disposal)
    return Promise.all([
      queryClient.invalidateQueries({ queryKey: disposalKeys.lists() }),
      queryClient.invalidateQueries({ queryKey: stockKeys.all }),
      queryClient.invalidateQueries({ queryKey: productKeys.all }),
    ])
  }
}

// Lập phiếu hủy.
export function useCreateDisposal() {
  const apply = useApplyDisposal()
  return useMutation({
    mutationFn: (body: CreateDisposalRequest) => createDisposal(body),
    onSuccess: ({ disposal }) => apply(disposal),
  })
}

// Chủ tiệm duyệt phiếu chờ.
export function useApproveDisposal(id: number) {
  const apply = useApplyDisposal()
  return useMutation({
    mutationFn: () => approveDisposal(id),
    onSuccess: apply,
  })
}

// Chủ tiệm từ chối phiếu chờ.
export function useRejectDisposal(id: number) {
  const apply = useApplyDisposal()
  return useMutation({
    mutationFn: (reason: string) => rejectDisposal(id, reason),
    onSuccess: apply,
  })
}
