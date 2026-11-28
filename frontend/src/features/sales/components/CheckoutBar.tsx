import { QrCodeIcon } from '@phosphor-icons/react'
import { useState } from 'react'
import { formatMoney } from '../../../lib/format'

interface CheckoutBarProps {
  total: number
  itemCount: number
  busy: boolean
  // Tiền mặt: bấm "Xong" là xong (UC-05: ≤ 3 lần chạm); chọn tiền khách đưa chỉ để xem tiền thối, không bắt buộc.
  onCash: (cashGiven: number | undefined) => void
  onTransfer: () => void
}

const NOTES = [10_000, 20_000, 50_000, 100_000, 200_000, 500_000]

// Mệnh giá khách hay đưa: làm tròn lên 10k / 50k / 100k và các tờ tiền lớn hơn tổng, tối đa 4 gợi ý.
function cashSuggestions(total: number): number[] {
  if (total <= 0) return []
  const roundUp = (step: number) => Math.ceil(total / step) * step
  const values = new Set<number>([roundUp(10_000), roundUp(50_000), roundUp(100_000), ...NOTES.filter((note) => note >= total)])
  values.delete(total)
  return [...values].sort((a, b) => a - b).slice(0, 4)
}

// Thanh thanh toán dính đáy màn hình (vùng ngón cái): tổng tiền lớn, gợi ý tiền khách đưa, Xong / Chuyển khoản.
export function CheckoutBar({ total, itemCount, busy, onCash, onTransfer }: CheckoutBarProps) {
  // Lưu kèm tổng lúc chọn: giỏ đổi thì lựa chọn cũ tự hết hiệu lực, không cần effect để xóa.
  const [cash, setCash] = useState<{ amount: number; forTotal: number } | null>(null)
  const cashGiven = cash && cash.forTotal === total && cash.amount >= total ? cash.amount : undefined
  const empty = itemCount === 0
  const suggestions = cashSuggestions(total)

  return (
    <div className="flex flex-col gap-3">
      <div className="flex items-end justify-between gap-3">
        <div className="flex flex-col">
          <span className="text-sm font-semibold text-ink-muted">Tổng {itemCount > 0 ? `· ${itemCount} món` : ''}</span>
          <output className="text-[2.5rem] leading-none font-bold tabular-nums" aria-live="polite">
            {formatMoney(total)}
          </output>
        </div>
        {cashGiven !== undefined && (
          <div className="flex flex-col items-end">
            <span className="text-sm font-semibold text-ink-muted">Thối</span>
            <span className="text-2xl font-bold tabular-nums text-ok">{formatMoney(cashGiven - total)}</span>
          </div>
        )}
      </div>

      {suggestions.length > 0 && (
        <div className="flex gap-2 overflow-x-auto" role="group" aria-label="Tiền khách đưa">
          {suggestions.map((amount) => (
            <button
              key={amount}
              type="button"
              aria-pressed={cashGiven === amount}
              onClick={() => setCash(cashGiven === amount ? null : { amount, forTotal: total })}
              className={`min-h-11 shrink-0 rounded-full border px-4 text-sm font-semibold tabular-nums ${
                cashGiven === amount ? 'border-brand-600 bg-brand-50 text-brand-700' : 'border-line bg-surface text-ink'
              }`}
            >
              {formatMoney(amount)}
            </button>
          ))}
        </div>
      )}

      <div className="grid grid-cols-[1fr_auto] gap-2">
        <button
          type="button"
          disabled={empty || busy}
          onClick={() => {
            onCash(cashGiven)
            setCash(null)
          }}
          className="h-14 rounded-xl bg-brand-600 text-lg font-semibold text-white transition-transform active:scale-[0.98] active:bg-brand-700 disabled:opacity-50 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-brand-500"
        >
          {busy ? 'Đang lưu…' : 'Xong · Tiền mặt'}
        </button>
        <button
          type="button"
          disabled={empty || busy}
          onClick={onTransfer}
          className="flex h-14 items-center gap-2 rounded-xl border border-line bg-surface px-4 font-semibold text-ink active:bg-brand-50 disabled:opacity-50 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-brand-500"
        >
          <QrCodeIcon size="1.375rem" weight="bold" aria-hidden="true" />
          Chuyển khoản
        </button>
      </div>
    </div>
  )
}
