import type { DisposalStatus } from '../../../api/inventory'

const text: Record<DisposalStatus, string> = { pending: 'Chờ duyệt', approved: 'Đã hủy', rejected: 'Từ chối' }
const tone: Record<DisposalStatus, string> = { pending: 'bg-warn/10 text-warn', approved: 'bg-ok/10 text-ok', rejected: 'bg-line text-ink-muted' }

// Nhãn trạng thái phiếu hủy bằng chữ (DESIGN.md: Not-Only-Color).
export function DisposalStatusBadge({ status }: { status: DisposalStatus }) {
  return <span className={`rounded px-1.5 text-xs font-semibold whitespace-nowrap ${tone[status]}`}>{text[status]}</span>
}
