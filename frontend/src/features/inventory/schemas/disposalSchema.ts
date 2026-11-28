import { z } from 'zod'

const reasons = ['expired', 'broken', 'damaged', 'other'] as const

// Một dòng đang soạn trong form con (DisposalLineEditor): vị trí, lô (0 = để hệ thống chọn), đơn vị + số lượng, lý do.
// Khớp DisposalLineRequest: số lượng nguyên dương, ghi chú ≤ 300 ký tự.
export const disposalLineSchema = z.object({
  locationId: z.number({ error: 'Chọn vị trí.' }).int().positive('Chọn vị trí.'),
  lotId: z.number().int().min(0),
  unitId: z.number().int().positive(),
  qty: z.number({ error: 'Nhập số lượng.' }).int('Số lượng là số nguyên.').min(1, 'Ít nhất 1.').max(99_999, 'Tối đa 99.999.'),
  reason: z.enum(reasons, { error: 'Chọn lý do.' }),
  note: z.string().trim().max(300, 'Ghi chú tối đa 300 ký tự.'),
})
export type DisposalLineValues = z.infer<typeof disposalLineSchema>

// Một dòng đã thêm vào phiếu (đủ thông tin để hiện và để gửi API).
export const disposalDraftLineSchema = z.object({
  productId: z.number().int().positive(),
  productName: z.string(),
  baseUnitName: z.string(),
  locationId: z.number().int().positive(),
  locationName: z.string(),
  lotId: z.number().int().positive().nullable(),
  lotLabel: z.string(),
  baseQty: z.number().int().positive(),
  qtyLabel: z.string(), // ví dụ "2 thùng = 48 chai"
  reason: z.enum(reasons),
  note: z.string(),
})
export type DisposalDraftLine = z.infer<typeof disposalDraftLineSchema>

// Cả phiếu: ghi chú ≤ 500, ít nhất một dòng (khớp CreateDisposalRequest).
export const disposalSchema = z.object({
  note: z.string().trim().max(500, 'Ghi chú tối đa 500 ký tự.'),
  lines: z.array(disposalDraftLineSchema).min(1, 'Thêm ít nhất một dòng hàng cần hủy.').max(200, 'Tối đa 200 dòng.'),
})
export type DisposalValues = z.infer<typeof disposalSchema>

// Lý do từ chối phiếu (1–300 ký tự).
export const rejectDisposalSchema = z.object({
  reason: z.string().trim().min(1, 'Nhập lý do từ chối.').max(300, 'Lý do tối đa 300 ký tự.'),
})
export type RejectDisposalValues = z.infer<typeof rejectDisposalSchema>
