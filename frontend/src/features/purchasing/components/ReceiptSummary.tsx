import { Link } from 'react-router'
import type { GoodsReceiptResponse } from '../../../api/purchasing'
import { formatDateTime, formatMoney } from '../../../lib/format'
import { useAuthStore } from '../../../stores/useAuthStore'
import { CancelReceiptForm } from './CancelReceiptForm'
import { ReceiptLineList } from './ReceiptLineList'

// Phiếu nhập đã hoàn tất hoặc đã hủy: chỉ xem (BR-03). Chủ tiệm hủy được phiếu đã hoàn tất.
export function ReceiptSummary({ receipt }: { receipt: GoodsReceiptResponse }) {
  const role = useAuthStore((state) => state.session?.user.role)
  const completed = receipt.status === 'completed'

  return (
    <div className="mx-auto flex max-w-2xl flex-col gap-4 lg:grid lg:max-w-none lg:grid-cols-[22rem_minmax(0,1fr)] xl:grid-cols-[26rem_minmax(0,1fr)] lg:items-start lg:gap-6">
      <div className={`flex flex-col gap-1 rounded-xl border-2 px-4 py-3 ${completed ? 'border-ok bg-ok/10' : 'border-bad bg-bad/10'}`}>
        <h1 className="text-xl font-bold">
          Phiếu nhập <span className="tabular-nums">{receipt.code}</span> · {completed ? 'Đã nhập kho' : 'Đã hủy'}
        </h1>
        <p>
          {receipt.supplierName ?? 'Không ghi nhà cung cấp'} · nhận vào {receipt.locationName}
        </p>
        {receipt.completedAt && (
          <p className="text-sm">
            Hoàn tất {formatDateTime(receipt.completedAt)} · {receipt.completedByName}
          </p>
        )}
        {receipt.cancelledAt && (
          <p className="text-sm font-semibold text-bad">
            Hủy {formatDateTime(receipt.cancelledAt)} · {receipt.cancelledByName}: {receipt.cancelReason}
          </p>
        )}
        {receipt.total !== null && <p className="text-2xl font-bold tabular-nums">{formatMoney(receipt.total)}</p>}
      </div>

      <div className="lg:col-start-2 lg:row-span-4 lg:row-start-1">
        <ReceiptLineList lines={receipt.lines} pricesHidden={receipt.pricesHidden} />
      </div>

      {completed && role === 'owner' && <CancelReceiptForm receiptId={receipt.id} />}

      <Link to="/nhap-hang" className="flex min-h-11 items-center self-start font-semibold text-brand-700 underline-offset-4 hover:underline">
        Về danh sách phiếu nhập
      </Link>
    </div>
  )
}
