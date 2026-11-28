import { Link, useParams } from 'react-router'
import { ApiError } from '../../../api/types'
import { DraftReceipt } from '../components/DraftReceipt'
import { ReceiptSummary } from '../components/ReceiptSummary'
import { useReceipt } from '../hooks/useGoodsReceipts'

// Một phiếu nhập (UC-04, SC-10): nháp thì nhập hàng, đã hoàn tất / đã hủy thì xem.
export function ReceiptPage() {
  const { id } = useParams()
  const receiptId = Number(id)
  const receipt = useReceipt(receiptId)

  if (!Number.isInteger(receiptId) || receiptId <= 0 || (receipt.error instanceof ApiError && receipt.error.status === 404)) {
    return (
      <div className="flex flex-col gap-2">
        <p className="font-semibold">Không tìm thấy phiếu nhập này.</p>
        <Link to="/nhap-hang" className="min-h-11 font-semibold text-brand-700">
          Về danh sách phiếu nhập
        </Link>
      </div>
    )
  }
  if (receipt.isPending) return <p className="text-ink-muted">Đang tải phiếu nhập…</p>
  if (receipt.isError) return <p className="font-semibold text-bad">{receipt.error.message}</p>

  return receipt.data.status === 'draft' ? <DraftReceipt receipt={receipt.data} /> : <ReceiptSummary receipt={receipt.data} />
}
