import { z } from 'zod'

// Hàng chưa khai báo (UC-05): tên và giá nhập tay, không trừ kho. Khớp SaleLineRequest của backend
// (Name ≤ 200 ký tự, UnitPrice ≥ 0, Qty ≥ 1).
export const undeclaredItemSchema = z.object({
  name: z.string().trim().min(1, 'Nhập tên món.').max(200, 'Tên tối đa 200 ký tự.'),
  unitPrice: z.number({ error: 'Nhập giá (số tiền, đồng).' }).int('Giá là số nguyên (đồng).').min(0, 'Giá không âm.').max(100_000_000, 'Giá quá lớn.'),
  qty: z.number({ error: 'Nhập số lượng.' }).int('Số lượng là số nguyên.').min(1, 'Ít nhất 1.').max(9999, 'Tối đa 9.999.'),
})

export type UndeclaredItemValues = z.infer<typeof undeclaredItemSchema>
