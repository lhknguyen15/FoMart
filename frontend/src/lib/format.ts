// Định dạng hiển thị theo quy ước FoMart: tiền 18.000đ, ngày dd/mm/yyyy, giờ Việt Nam. Server luôn trả UTC.

const timeZone = 'Asia/Ho_Chi_Minh'
const moneyFormat = new Intl.NumberFormat('vi-VN', { maximumFractionDigits: 0 })
const dateFormat = new Intl.DateTimeFormat('vi-VN', { timeZone, day: '2-digit', month: '2-digit', year: 'numeric' })
const timeFormat = new Intl.DateTimeFormat('vi-VN', { timeZone, hour: '2-digit', minute: '2-digit', hour12: false })

// 18000 → "18.000đ"
export function formatMoney(amount: number): string {
  return `${moneyFormat.format(amount)}đ`
}

// "2026-11-27T05:00:00Z" → "27/11/2026" (giờ Việt Nam). Chuỗi chỉ có ngày ("2026-11-27") giữ nguyên ngày đó.
export function formatDate(value: string): string {
  if (/^\d{4}-\d{2}-\d{2}$/.test(value)) {
    const [year, month, day] = value.split('-')
    return `${day}/${month}/${year}`
  }
  return dateFormat.format(new Date(value))
}

// "2026-11-27T05:00:00Z" → "12:00 27/11/2026" (giờ Việt Nam).
export function formatDateTime(value: string): string {
  const date = new Date(value)
  return `${timeFormat.format(date)} ${dateFormat.format(date)}`
}

// Ngày hôm nay theo giờ Việt Nam, dạng "yyyy-mm-dd" (giá trị cho <input type="date"> và tham số from/to của API).
export function todayInVietnam(): string {
  const parts = new Intl.DateTimeFormat('en-CA', { timeZone, year: 'numeric', month: '2-digit', day: '2-digit' }).formatToParts(new Date())
  const get = (type: string) => parts.find((part) => part.type === type)?.value ?? ''
  return `${get('year')}-${get('month')}-${get('day')}`
}

// "cash" → "Tiền mặt", "transfer" → "Chuyển khoản".
export function formatPaymentMethod(method: 'cash' | 'transfer'): string {
  return method === 'cash' ? 'Tiền mặt' : 'Chuyển khoản'
}

// "2026-11-27T05:00:00Z" → "12:00" (giờ Việt Nam).
export function formatTime(value: string): string {
  return timeFormat.format(new Date(value))
}

// Lý do xuất hủy (UC-08): "expired" → "Hết hạn"...
export function formatDisposalReason(reason: 'expired' | 'broken' | 'damaged' | 'other'): string {
  return { expired: 'Hết hạn', broken: 'Bể vỡ', damaged: 'Hư hỏng', other: 'Khác' }[reason]
}
