import { Link, useParams } from 'react-router'
import type { SaleLineResponse, SaleLotResponse } from '../../../api/sales'
import { ApiError } from '../../../api/types'
import { formatDate, formatDateTime, formatMoney, formatPaymentMethod } from '../../../lib/format'
import { useAuthStore } from '../../../stores/useAuthStore'
import { CancelSaleForm } from '../components/CancelSaleForm'
import { PendingSales } from '../components/PendingSales'
import { SaleStatusBadge } from '../components/SaleStatusBadge'
import { useSale } from '../hooks/useSales'

const backLink = (
  <Link to="/hoa-don" className="flex min-h-11 items-center self-start font-semibold text-brand-700 underline-offset-4 hover:underline">
    Về danh sách hóa đơn
  </Link>
)

// Chi tiết một hóa đơn (SC-04): các dòng với giá đơn vị lúc bán (BR-07), lô đã trừ (BR-04), giá vốn và lãi
// cho người được xem (BR-08); chủ tiệm hủy hóa đơn đã thanh toán (UC-06).
export function SaleDetailPage() {
  const { id = '' } = useParams()
  const sale = useSale(id)
  const role = useAuthStore((state) => state.session?.user.role)

  if (sale.isPending) return <p className="text-ink-muted">Đang tải hóa đơn…</p>
  if (sale.isError) {
    const notFound = sale.error instanceof ApiError && (sale.error.status === 404 || sale.error.status === 400)
    return (
      <div className="flex flex-col gap-2">
        <p className="font-semibold text-bad">{notFound ? 'Không tìm thấy hóa đơn này.' : sale.error.message}</p>
        {backLink}
      </div>
    )
  }

  const data = sale.data
  const tone = data.status === 'paid' ? 'border-ok bg-ok/10' : data.status === 'cancelled' ? 'border-bad bg-bad/10' : 'border-warn bg-warn/10'

  return (
    <div className="mx-auto flex max-w-2xl flex-col gap-4 lg:grid lg:max-w-none lg:grid-cols-[22rem_minmax(0,1fr)] xl:grid-cols-[26rem_minmax(0,1fr)] lg:items-start lg:gap-6">
      <div className={`flex flex-col gap-1 rounded-xl border-2 px-4 py-3 ${tone}`}>
        <h1 className="flex flex-wrap items-center gap-2 text-xl font-bold">
          Hóa đơn <span className="tabular-nums">{data.code}</span>
          <SaleStatusBadge status={data.status} />
        </h1>
        <p className={`text-3xl font-bold tabular-nums ${data.status === 'cancelled' ? 'line-through' : ''}`}>{formatMoney(data.total)}</p>
        <p className="text-sm">
          {formatPaymentMethod(data.paymentMethod)}
          {data.cashGiven !== null ? ` · khách đưa ${formatMoney(data.cashGiven)}` : ''}
          {data.change !== null && data.change > 0 ? ` · thối ${formatMoney(data.change)}` : ''}
        </p>
        <p className="text-sm">
          Bán {formatDateTime(data.createdAt)} · {data.createdByName} · {data.locationName}
        </p>
        {data.paidAt && data.paymentMethod === 'transfer' && <p className="text-sm">Nhận tiền {formatDateTime(data.paidAt)}</p>}
        {data.cancelledAt && (
          <p className="text-sm font-semibold text-bad">
            Hủy {formatDateTime(data.cancelledAt)} · {data.cancelledByName}: {data.cancelReason}
          </p>
        )}
        {!data.costHidden && data.costTotal !== null && data.grossProfit !== null && (
          <p className="text-sm tabular-nums">
            Giá vốn {formatMoney(data.costTotal)} · lãi gộp <span className="font-semibold">{formatMoney(data.grossProfit)}</span>
          </p>
        )}
        {!data.costHidden && data.status !== 'pending_payment' && data.costTotal === null && (
          <p className="text-sm text-ink-muted">Chưa tính được lãi: có món trừ vào lô tạm hoặc hàng chưa khai báo (giá vốn chưa xác định, BR-01).</p>
        )}
      </div>

      <ul className="flex flex-col overflow-hidden rounded-xl border border-line bg-surface lg:col-start-2 lg:row-span-4 lg:row-start-1">
        {data.lines.map((line) => (
          <SaleLineItem key={line.id} line={line} costHidden={data.costHidden} />
        ))}
      </ul>

      {data.status === 'pending_payment' && <PendingSales focusId={data.id} onlyId={data.id} />}
      {data.status === 'paid' && role === 'owner' && <CancelSaleForm saleId={data.id} />}
      {backLink}
    </div>
  )
}

// Tên lô cho người bán: số lô in trên thùng và hạn dùng, không dùng id nội bộ (id có thể nhảy số).
function describeLot(lot: SaleLotResponse): string {
  if (lot.lotKind === 'provisional') return 'lô tạm'
  const parts = [lot.lotNo ? `lô ${lot.lotNo}` : 'lô', lot.expiryDate ? `HSD ${formatDate(lot.expiryDate)}` : null]
  return parts.filter(Boolean).join(' ')
}

function SaleLineItem({ line, costHidden }: { line: SaleLineResponse; costHidden: boolean }) {
  return (
    <li className="flex flex-col gap-0.5 border-b border-line px-3 py-2.5 last:border-b-0">
      <div className="flex items-baseline justify-between gap-2">
        <span className="min-w-0 font-semibold">{line.name}</span>
        <span className="shrink-0 font-semibold tabular-nums">{formatMoney(line.lineTotal)}</span>
      </div>
      <p className="text-sm text-ink-muted tabular-nums">
        {line.qty} × {formatMoney(line.unitPrice)}
        {line.baseQty !== null && line.baseQty !== line.qty ? ` · ${line.baseQty} đơn vị cơ sở` : ''}
        {!costHidden && line.costTotal !== null ? ` · vốn ${formatMoney(line.costTotal)}` : ''}
      </p>
      {line.isUndeclared && <p className="text-sm font-semibold text-warn">Hàng chưa khai báo: không trừ kho.</p>}
      {line.expiredConfirmed && <p className="text-sm font-semibold text-warn">Bán từ lô đã hết hạn (đã xác nhận kiểm hàng).</p>}
      {line.lots.length > 0 && (
        <p className="text-sm text-ink-muted">
          Trừ:{' '}
          {line.lots.map((lot) => `${lot.qty} từ ${describeLot(lot)}`).join(', ')}
        </p>
      )}
    </li>
  )
}
