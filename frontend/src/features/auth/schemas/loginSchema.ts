import { z } from 'zod'

// Khớp kiểm tra của backend (Application/DTO/Auth/AuthModels.cs: AuthValidation, LoginRequest, PinLoginRequest).
// Backend vẫn là nơi kiểm tra cuối cùng; schema này chỉ báo lỗi sớm ngay trên form.

export const phoneSchema = z
  .string()
  .trim()
  .regex(/^[0-9+]{8,15}$/, 'Số điện thoại chỉ gồm chữ số, 8–15 số.')

export const pinSchema = z.string().regex(/^[0-9]{4,6}$/, 'PIN gồm 4–6 chữ số.')

// Đăng nhập bằng số điện thoại: chọn mật khẩu (chủ tiệm) hoặc PIN (nhân viên).
// discriminatedUnion: quy tắc của ô "secret" đổi theo giá trị của "method".
export const phoneLoginSchema = z.discriminatedUnion('method', [
  z.object({ method: z.literal('password'), phone: phoneSchema, secret: z.string().min(1, 'Nhập mật khẩu.') }),
  z.object({ method: z.literal('pin'), phone: phoneSchema, secret: pinSchema }),
])

export type PhoneLoginValues = z.infer<typeof phoneLoginSchema>
