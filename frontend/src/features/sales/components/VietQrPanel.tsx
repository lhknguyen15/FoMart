import QRCode from 'qrcode'
import { useMemo } from 'react'
import { ApiError } from '../../../api/types'
import { formatMoney } from '../../../lib/format'
import { bankName, buildVietQrPayload } from '../../../lib/vietqr'
import { useStorePayment } from '../../store/hooks/useStore'

interface VietQrPanelProps {
  code: string // mã hóa đơn, cũng là nội dung chuyển khoản
  total: number
}

// Mã VietQR cho một hóa đơn chờ chuyển khoản (UC-05 bước 5): khách quét bằng app ngân hàng,
// số tiền và nội dung đã điền sẵn; người bán thấy tiền về thì bấm "Đã nhận tiền" (quyết định số 4).
export function VietQrPanel({ code, total }: VietQrPanelProps) {
  const payment = useStorePayment()

  const payload = payment.data ? buildVietQrPayload({ bankBin: payment.data.bankBin, accountNo: payment.data.accountNo, amount: total, content: code }) : null
  // Tạo ma trận QR đồng bộ rồi vẽ thành một path SVG: sắc nét ở mọi cỡ, không cần ảnh hay effect.
  const qr = useMemo(() => {
    if (!payload) return null
    const { modules } = QRCode.create(payload, { errorCorrectionLevel: 'M' })
    let path = ''
    for (let row = 0; row < modules.size; row++)
      for (let col = 0; col < modules.size; col++) if (modules.get(row, col)) path += `M${col + 4} ${row + 4}h1v1h-1z`
    return { size: modules.size + 8, path } // 4 ô lề trắng mỗi phía theo chuẩn QR
  }, [payload])

  if (payment.isPending) return <p className="text-sm text-ink-muted">Đang tạo mã QR…</p>
  if (payment.isError) {
    const notConfigured = payment.error instanceof ApiError && payment.error.status === 404
    return (
      <p className="rounded-lg border border-warn/50 bg-warn/10 p-2 text-sm font-semibold text-warn">
        {notConfigured ? payment.error.message : 'Không tải được tài khoản ngân hàng. Khách chuyển khoản thủ công theo số tài khoản của tiệm.'}
      </p>
    )
  }
  if (!qr) return null

  return (
    <figure className="flex flex-col items-center gap-2 rounded-xl border border-line bg-surface p-3">
      <svg
        viewBox={`0 0 ${qr.size} ${qr.size}`}
        role="img"
        aria-label={`Mã VietQR chuyển ${formatMoney(total)} cho hóa đơn ${code}`}
        shapeRendering="crispEdges"
        className="aspect-square w-full max-w-64 rounded-lg bg-white"
      >
        <path d={qr.path} className="fill-ink" />
      </svg>
      <figcaption className="flex flex-col items-center gap-0.5 text-center text-sm">
        <span className="text-2xl font-bold tabular-nums">{formatMoney(total)}</span>
        <span className="font-semibold">
          {bankName(payment.data.bankBin)} · <span className="tabular-nums">{payment.data.accountNo}</span>
        </span>
        <span>{payment.data.accountName}</span>
        <span className="text-ink-muted">
          Nội dung: <span className="font-semibold tabular-nums text-ink">{code}</span>
        </span>
      </figcaption>
    </figure>
  )
}
