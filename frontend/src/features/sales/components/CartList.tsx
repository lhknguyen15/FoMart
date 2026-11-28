import { MinusIcon, PlusIcon, ShoppingCartIcon, TrashIcon } from '@phosphor-icons/react'
import { formatMoney } from '../../../lib/format'
import type { CartLine } from '../../../stores/useCartStore'

interface CartListProps {
  lines: CartLine[]
  onQty: (key: string, qty: number) => void
  disabled?: boolean
}

const stepClass =
  'flex size-11 items-center justify-center rounded-lg border border-line bg-surface text-ink active:bg-brand-50 disabled:opacity-40 ' +
  'focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-brand-500'

// Giỏ hàng: món mới nhất trên cùng; +/− đổi số lượng, − ở số lượng 1 thì bỏ món (quét nhầm, UC-05).
export function CartList({ lines, onQty, disabled = false }: CartListProps) {
  if (lines.length === 0) {
    return (
      <div className="flex flex-col items-center gap-2 rounded-xl border border-dashed border-line bg-surface px-4 py-8 text-center">
        <ShoppingCartIcon size="2.5rem" weight="duotone" className="text-brand-600" aria-hidden="true" />
        <p className="font-semibold">Giỏ trống</p>
        <p className="text-sm text-ink-muted">Quét mã hoặc tìm theo tên để thêm món.</p>
      </div>
    )
  }

  return (
    <ul className="flex flex-col overflow-hidden rounded-xl border border-line bg-surface" aria-label="Giỏ hàng">
      {lines.map((line) => (
        // Hai tầng: tên món đọc trọn trên cùng (giỏ hẹp trên điện thoại và cột phải trên máy tính), bên dưới là +/− và thành tiền.
        <li key={line.key} className="flex flex-col gap-1.5 border-b border-line px-3 py-2.5 last:border-b-0">
          <div className="flex min-w-0 items-baseline justify-between gap-2">
            <span className="min-w-0 font-semibold wrap-break-word">{line.productName}</span>
            <span className="shrink-0 text-sm text-ink-muted tabular-nums">
              {line.unitName ?? 'chưa khai báo'} · {formatMoney(line.unitPrice)}
            </span>
          </div>
          <div className="flex items-center gap-1.5">
            <button
              type="button"
              className={stepClass}
              onClick={() => onQty(line.key, line.qty - 1)}
              disabled={disabled}
              aria-label={line.qty === 1 ? `Bỏ ${line.productName} khỏi giỏ` : `Bớt một ${line.productName}`}
            >
              {line.qty === 1 ? <TrashIcon size="1.25rem" /> : <MinusIcon size="1.25rem" weight="bold" />}
            </button>
            <span className="w-8 text-center text-lg font-bold tabular-nums" aria-label={`Số lượng ${line.qty}`}>
              {line.qty}
            </span>
            <button
              type="button"
              className={stepClass}
              onClick={() => onQty(line.key, line.qty + 1)}
              disabled={disabled}
              aria-label={`Thêm một ${line.productName}`}
            >
              <PlusIcon size="1.25rem" weight="bold" />
            </button>
            <span className="ml-auto text-right text-lg font-semibold tabular-nums">{formatMoney(line.unitPrice * line.qty)}</span>
          </div>
        </li>
      ))}
    </ul>
  )
}
