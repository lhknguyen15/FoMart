import { z } from 'zod'

// Tạo nhanh sản phẩm khi quét ra mã chưa có (UC-01 nhánh tạo nhanh): một đơn vị cơ sở, hệ số 1.
// Khớp CreateProductRequest / CreateProductUnitRequest: tên ≤ 200, tên đơn vị ≤ 30, giá bán ≥ 0.
export const quickProductSchema = z.object({
  name: z.string().trim().min(1, 'Nhập tên sản phẩm.').max(200, 'Tên tối đa 200 ký tự.'),
  unitName: z.string().trim().min(1, 'Nhập đơn vị (lon, chai, gói…).').max(30, 'Đơn vị tối đa 30 ký tự.'),
  salePrice: z.number({ error: 'Nhập giá bán (đồng).' }).int('Giá là số nguyên (đồng).').min(0, 'Giá không âm.').max(1_000_000_000, 'Giá quá lớn.'),
  trackExpiry: z.boolean(),
})

export type QuickProductValues = z.infer<typeof quickProductSchema>
