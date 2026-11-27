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
