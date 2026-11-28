import { z } from 'zod'

// Lý do hủy phiếu nhập đã hoàn tất (BR-03). Khớp CancelGoodsReceiptRequest: 1-300 ký tự.
export const cancelReceiptSchema = z.object({
  reason: z.string().trim().min(1, 'Nhập lý do hủy.').max(300, 'Lý do tối đa 300 ký tự.'),
})

export type CancelReceiptValues = z.infer<typeof cancelReceiptSchema>
