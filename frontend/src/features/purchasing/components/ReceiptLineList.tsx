import { PackageIcon, PencilSimpleIcon, TrashIcon, WarningIcon } from '@phosphor-icons/react'
import type { ReceiptLineResponse } from '../../../api/purchasing'
import { formatDate, formatMoney } from '../../../lib/format'

interface ReceiptLineListProps {
  lines: ReceiptLineResponse[]
  pricesHidden: boolean
  // Có khi phiếu còn nháp; phiếu đã hoàn tất / đã hủy chỉ xem.
  onEdit?: (line: ReceiptLineResponse) => void
  onDelete?: (line: ReceiptLineResponse) => void
  busy?: boolean
}

const expiryText = { expired: 'Đã hết hạn', near_expiry: 'Sắp hết hạn' } as const

// Các dòng của phiếu nhập: số lượng theo đơn vị nhập và quy ra đơn vị cơ sở, giá, hạn dùng, cảnh báo.
export function ReceiptLineList({ lines, pricesHidden, onEdit, onDelete, busy }: ReceiptLineListProps) {
  if (lines.length === 0) {
    return (
      <div className="flex flex-col items-center gap-1 rounded-xl border border-dashed border-line bg-surface px-4 py-6 text-center text-ink-muted">
        <PackageIcon size="2rem" weight="duotone" aria-hidden="true" />
        <p>Chưa có dòng nào. Quét mã hoặc tìm tên hàng để thêm.</p>
      </div>
    )
  }

  return (
    <ul className="flex flex-col overflow-hidden rounded-xl border border-line bg-surface">
      {lines.map((line) => (
        <li key={line.id} className="flex flex-col gap-1 border-b border-line px-3 py-2.5 last:border-b-0">
          <div className="flex items-baseline justify-between gap-2">
            <span className="min-w-0 font-semibold">{line.productName}</span>
            {!pricesHidden && (
              <span className="shrink-0 font-semibold tabular-nums">{line.lineTotal !== null ? formatMoney(line.lineTotal) : 'Chưa có giá'}</span>
            )}
          </div>
          <p className="text-sm text-ink-muted tabular-nums">
            {line.qty} {line.unitName}
            {line.factor > 1 ? ` = ${line.baseQty}` : ''}
            {!pricesHidden && line.unitPrice !== null ? ` · ${formatMoney(line.unitPrice)}/${line.unitName}` : ''}
            {line.expiryDate ? ` · HSD ${formatDate(line.expiryDate)}` : ''}
            {line.lotNo ? ` · lô ${line.lotNo}` : ''}
          </p>
          {line.expiryWarning && (
            <p className="flex items-center gap-1 text-sm font-semibold text-warn">
              <WarningIcon size="1.125rem" weight="bold" aria-hidden="true" />
              {expiryText[line.expiryWarning]}
            </p>
          )}
          {pricesHidden && line.unitPrice === null && <p className="text-sm text-ink-muted">Giá do chủ tiệm điền.</p>}
          {(onEdit || onDelete) && (
            <div className="flex gap-2 pt-1">
              {onEdit && (
                <button
                  type="button"
                  onClick={() => onEdit(line)}
                  disabled={busy}
                  className="flex min-h-11 items-center gap-1.5 rounded-lg border border-line px-3 text-sm font-semibold active:bg-brand-50 disabled:opacity-60"
                >
                  <PencilSimpleIcon size="1.125rem" aria-hidden="true" />
                  Sửa
                </button>
              )}
              {onDelete && (
                <button
                  type="button"
                  onClick={() => onDelete(line)}
                  disabled={busy}
                  aria-label={`Xóa dòng ${line.productName}`}
                  className="flex min-h-11 items-center gap-1.5 rounded-lg border border-line px-3 text-sm font-semibold text-bad active:bg-bad/10 disabled:opacity-60"
                >
                  <TrashIcon size="1.125rem" aria-hidden="true" />
                  Xóa
                </button>
              )}
            </div>
          )}
        </li>
      ))}
    </ul>
  )
}
