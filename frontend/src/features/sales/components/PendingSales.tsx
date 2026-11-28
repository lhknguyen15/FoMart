import { QrCodeIcon } from '@phosphor-icons/react'
import { lazy, Suspense, useState } from 'react'
import { ApiError } from '../../../api/types'
import { formatDateTime, formatMoney } from '../../../lib/format'
import { useCancelSale, useConfirmSalePayment, usePendingSales } from '../hooks/useSales'

// Mã QR tải lười: thư viện qrcode chỉ tải khi có hóa đơn chuyển khoản cần hiện mã.
const VietQrPanel = lazy(() => import('./VietQrPanel').then((module) => ({ default: module.VietQrPanel })))

interface PendingSalesProps {
  focusId?: string // hóa đơn vừa tạo ở máy này: mở sẵn mã QR cho khách quét
  onlyId?: string // trang chi tiết hóa đơn: chỉ hiện đúng hóa đơn này
}

// Hóa đơn chờ chuyển khoản (UC-05): giỏ đã được giữ lại để bán tiếp khách khác; khách chuyển xong thì bấm "Đã nhận".
export function PendingSales({ focusId, onlyId }: PendingSalesProps) {
  const pending = usePendingSales()
  const confirm = useConfirmSalePayment()
  const cancel = useCancelSale()
  // Chỉ còn lô hết hạn (409): hỏi xác nhận đã kiểm hàng trên tay rồi gửi lại (BR-05).
  const [expiredPrompt, setExpiredPrompt] = useState<{ id: string; message: string } | null>(null)
  const [error, setError] = useState<string | null>(null)
  // Người bán tự mở/đóng mã QR từng hóa đơn; chưa bấm thì hóa đơn vừa tạo mở sẵn.
  const [qrToggled, setQrToggled] = useState<Record<string, boolean>>({})
  const isQrOpen = (id: string) => qrToggled[id] ?? id === focusId

  const items = (pending.data?.items ?? []).filter((sale) => !onlyId || sale.id === onlyId)
  if (items.length === 0) return null

  const received = (id: string, confirmExpired = false) => {
    setError(null)
    confirm.mutate(
      { id, confirmExpired },
      {
        onSuccess: () => setExpiredPrompt(null),
        onError: (caught) => {
          if (caught instanceof ApiError && caught.status === 409 && caught.message.includes('hết hạn')) setExpiredPrompt({ id, message: caught.message })
          else setError(caught instanceof ApiError ? caught.message : 'Không xác nhận được.')
        },
      },
    )
  }

  return (
    <section className="flex flex-col gap-2" aria-labelledby="pending-title">
      <h2 id="pending-title" className="text-lg font-semibold">
        Chờ chuyển khoản · {items.length}
      </h2>
      {error && <p className="text-sm font-semibold text-bad">{error}</p>}
      <ul className="flex flex-col overflow-hidden rounded-xl border border-warn/40 bg-surface">
        {items.map((sale) => (
          <li key={sale.id} className="flex flex-col gap-2 border-b border-line px-3 py-2.5 last:border-b-0">
            <div className="flex items-baseline justify-between gap-2">
              <span className="font-semibold tabular-nums">{sale.code}</span>
              <span className="text-lg font-bold tabular-nums">{formatMoney(sale.total)}</span>
            </div>
            <span className="text-sm text-ink-muted">
              {sale.lineCount} món · {formatDateTime(sale.createdAt)} · {sale.createdByName}
            </span>
            {isQrOpen(sale.id) && (
              <Suspense fallback={<p className="text-sm text-ink-muted">Đang tạo mã QR…</p>}>
                <VietQrPanel code={sale.code} total={sale.total} />
              </Suspense>
            )}
            {expiredPrompt?.id === sale.id && (
              <div role="alert" className="flex flex-col gap-2 rounded-lg border border-warn/50 bg-warn/10 p-2 text-sm">
                <p className="font-semibold text-warn">{expiredPrompt.message}</p>
                <button type="button" onClick={() => received(sale.id, true)} className="min-h-11 rounded-lg bg-warn px-3 font-semibold text-white">
                  Đã kiểm hàng, vẫn bán
                </button>
              </div>
            )}
            <div className="grid grid-cols-[1fr_auto_auto] gap-2">
              <button
                type="button"
                onClick={() => received(sale.id)}
                disabled={confirm.isPending}
                className="h-12 rounded-lg bg-brand-600 font-semibold text-white active:bg-brand-700 disabled:opacity-60"
              >
                Đã nhận tiền
              </button>
              <button
                type="button"
                aria-expanded={isQrOpen(sale.id)}
                onClick={() => setQrToggled((state) => ({ ...state, [sale.id]: !isQrOpen(sale.id) }))}
                className="flex h-12 items-center gap-1.5 rounded-lg border border-line bg-surface px-3 font-semibold text-ink active:bg-brand-50"
              >
                <QrCodeIcon size="1.25em" weight="bold" aria-hidden="true" />
                Mã QR
              </button>
              <button
                type="button"
                onClick={() => cancel.mutate({ id: sale.id, reason: 'Khách không chuyển khoản' })}
                disabled={cancel.isPending}
                className="h-12 rounded-lg border border-line bg-surface px-4 font-semibold text-ink active:bg-brand-50 disabled:opacity-60"
              >
                Bỏ
              </button>
            </div>
          </li>
        ))}
      </ul>
    </section>
  )
}
