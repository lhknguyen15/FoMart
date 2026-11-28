import type { SaleStatus } from '../../../api/sales'

const statusText: Record<SaleStatus, string> = { paid: 'Đã thanh toán', pending_payment: 'Chờ chuyển khoản', cancelled: 'Đã hủy' }
const statusClass: Record<SaleStatus, string> = { paid: 'bg-ok/10 text-ok', pending_payment: 'bg-warn/10 text-warn', cancelled: 'bg-bad/10 text-bad' }

// Nhãn trạng thái hóa đơn: có chữ, không chỉ dựa vào màu (DESIGN.md: Not-Only-Color).
export function SaleStatusBadge({ status }: { status: SaleStatus }) {
  return <span className={`rounded px-1.5 text-xs font-semibold ${statusClass[status]}`}>{statusText[status]}</span>
}
