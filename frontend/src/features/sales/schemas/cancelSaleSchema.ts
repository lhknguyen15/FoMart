import { z } from 'zod'

// Lý do hủy hóa đơn (UC-06). Khớp CancelSaleRequest: 1-300 ký tự.
export const cancelSaleSchema = z.object({
  reason: z.string().trim().min(1, 'Nhập lý do hủy.').max(300, 'Lý do tối đa 300 ký tự.'),
})

export type CancelSaleValues = z.infer<typeof cancelSaleSchema>
