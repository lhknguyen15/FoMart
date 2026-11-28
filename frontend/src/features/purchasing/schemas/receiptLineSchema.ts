import { z } from 'zod'

// Một dòng nhập (UC-04 bước 3-4). Khớp ReceiptLineRequest của backend: Qty ≥ 1, UnitPrice ≥ 0 hoặc để trống,
// LotNo ≤ 50 ký tự, hạn dùng bắt buộc khi sản phẩm có quản lý hạn dùng.
// Giá và hạn dùng giữ dạng chuỗi trong form (ô trống = chưa nhập), đổi sang số / null lúc gửi.
// "190.000" → "190000". Dùng cả khi tính thành tiền tạm trên form.
export const toDigits = (text: string) => text.replace(/[.\s]/g, '')

export function receiptLineSchema(trackExpiry: boolean) {
  return z.object({
    unitId: z.number().int().positive(),
    qty: z.number({ error: 'Nhập số lượng.' }).int('Số lượng là số nguyên.').min(1, 'Ít nhất 1.').max(99_999, 'Tối đa 99.999.'),
    // Chấp nhận cách gõ quen thuộc "190.000" hoặc "190 000": bỏ dấu chấm, khoảng trắng rồi mới kiểm.
    unitPrice: z
      .string()
      .transform(toDigits)
      .pipe(z.string().regex(/^\d{0,10}$/, 'Giá là số tiền (đồng), ví dụ 190000.')),
    expiryDate: trackExpiry
      ? z.string().regex(/^\d{4}-\d{2}-\d{2}$/, 'Sản phẩm có quản lý hạn dùng: chọn hạn dùng.')
      : z.string(),
    lotNo: z.string().trim().max(50, 'Số lô tối đa 50 ký tự.'),
  })
}

export type ReceiptLineValues = z.infer<ReturnType<typeof receiptLineSchema>>
