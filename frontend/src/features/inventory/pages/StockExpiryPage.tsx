import { useState } from 'react'
import type { StockExpiryFilter, StockLotResponse } from '../../../api/inventory'
import { ChoiceChips, type Choice } from '../../../components/ui/ChoiceChips'
import { DataTable, type DataColumn } from '../../../components/ui/DataTable'
import { formatDate, formatMoney, todayInVietnam } from '../../../lib/format'
import { useExpiringLots, useExpirySummary } from '../hooks/useStock'

const filterChoices: Choice<StockExpiryFilter>[] = [
  { value: 'warning', label: 'Tất cả cần xử lý' },
  { value: 'expired', label: 'Đã hết hạn' },
  { value: 'near_expiry', label: 'Sắp hết hạn' },
]

const badge = 'rounded px-1.5 text-xs font-semibold whitespace-nowrap'

// Số ngày từ hôm nay (giờ VN) tới hạn dùng; âm = đã quá hạn.
function daysLeft(expiryDate: string, today: string): number {
  return Math.round((Date.parse(expiryDate) - Date.parse(today)) / 86_400_000)
}

// "Hết hạn 3 ngày trước", "Hết hạn hôm nay", "Còn 5 ngày": nói bằng chữ, không chỉ bằng màu.
function DaysLeft({ lot, today }: { lot: StockLotResponse; today: string }) {
  if (!lot.expiryDate) return null
  const days = daysLeft(lot.expiryDate, today)
  if (days < 0) return <span className={`${badge} bg-bad/10 text-bad`}>Quá hạn {-days} ngày</span>
  if (days === 0) return <span className={`${badge} bg-bad/10 text-bad`}>Hết hạn hôm nay</span>
  return <span className={`${badge} bg-warn/10 text-warn`}>Còn {days} ngày</span>
}

const lotLabel = (lot: StockLotResponse) => (lot.lotNo ? `Lô ${lot.lotNo}` : 'Lô không số')
// Giá trị tồn theo giá vốn của lô (BR-11: làm tròn về đồng khi hiển thị); null khi bị ẩn giá vốn (BR-08).
const lotValue = (lot: StockLotResponse) => (lot.unitCost !== null ? Math.round(lot.unitCost * lot.qty) : null)

// Hạn dùng (UC-08, SC-12): lô đã hết hạn còn tồn và lô hết hạn trong N ngày tới (N = cài đặt cửa hàng), kèm vị trí,
// để đưa ra bán trước, giảm giá, hoặc xuất hủy. Hạn gấp nhất đứng đầu.
export function StockExpiryPage() {
  const [filter, setFilter] = useState<StockExpiryFilter>('warning')
  const summary = useExpirySummary()
  const lots = useExpiringLots(filter)
  const today = todayInVietnam()
  const rows = lots.data?.pages.flatMap((page) => page.items) ?? []
  const totalCount = lots.data?.pages[0]?.totalCount ?? 0
  const showValue = rows.some((lot) => lot.unitCost !== null)
  const warnDays = summary.data?.warnDays ?? 7

  const columns: DataColumn<StockLotResponse>[] = [
    { header: 'Sản phẩm', cell: (lot) => lot.productName },
    { header: 'Lô', cell: lotLabel },
    { header: 'Hạn dùng', cell: (lot) => <span className="tabular-nums">{lot.expiryDate ? formatDate(lot.expiryDate) : '—'}</span> },
    { header: 'Còn', cell: (lot) => <DaysLeft lot={lot} today={today} /> },
    { header: 'Vị trí', cell: (lot) => lot.locationName },
    { header: 'Tồn', cell: (lot) => `${lot.qty} ${lot.baseUnitName}`, align: 'right' },
    ...(showValue ? [{ header: 'Giá trị (vốn)', cell: (lot: StockLotResponse) => { const value = lotValue(lot); return value !== null ? formatMoney(value) : '—' }, align: 'right' as const }] : []),
  ]

  return (
    <div className="mx-auto flex max-w-2xl flex-col gap-4 lg:max-w-none">
      <div className="flex flex-col gap-1">
        <h1 className="text-2xl font-bold">Hạn dùng</h1>
        <p className="text-sm text-ink-muted">
          Lô còn tồn đã hết hạn và lô hết hạn trong {warnDays} ngày tới. Đưa hàng sắp hết hạn ra trước hoặc giảm giá; hàng đã hết hạn thì cất khỏi kệ và lập
          phiếu xuất hủy (sắp có).
        </p>
      </div>

      {summary.data && (
        <div className="grid gap-3 sm:grid-cols-2 lg:max-w-3xl">
          <button
            type="button"
            aria-pressed={filter === 'expired'}
            onClick={() => setFilter(filter === 'expired' ? 'warning' : 'expired')}
            className="flex flex-col items-start gap-0.5 rounded-xl border-2 border-bad/40 bg-surface px-4 py-3 text-left aria-pressed:border-bad aria-pressed:bg-bad/10"
          >
            <span className="text-sm font-semibold text-bad">Đã hết hạn</span>
            <span className="text-2xl font-bold tabular-nums">{summary.data.expiredLots} lô</span>
            <span className="text-sm text-ink-muted tabular-nums">
              {summary.data.expiredQty} đơn vị
              {summary.data.expiredValue !== null ? ` · ${formatMoney(summary.data.expiredValue)} theo giá vốn` : ''}
            </span>
          </button>
          <button
            type="button"
            aria-pressed={filter === 'near_expiry'}
            onClick={() => setFilter(filter === 'near_expiry' ? 'warning' : 'near_expiry')}
            className="flex flex-col items-start gap-0.5 rounded-xl border-2 border-warn/40 bg-surface px-4 py-3 text-left aria-pressed:border-warn aria-pressed:bg-warn/10"
          >
            <span className="text-sm font-semibold text-warn">Sắp hết hạn (dưới {warnDays} ngày)</span>
            <span className="text-2xl font-bold tabular-nums">{summary.data.nearLots} lô</span>
            <span className="text-sm text-ink-muted tabular-nums">
              {summary.data.nearQty} đơn vị
              {summary.data.nearValue !== null ? ` · ${formatMoney(summary.data.nearValue)} theo giá vốn` : ''}
            </span>
          </button>
        </div>
      )}
      {summary.isError && <p className="text-sm font-semibold text-bad">{summary.error.message}</p>}

      <div className="rounded-xl border border-line bg-surface p-3 lg:p-4">
        <ChoiceChips label="Hiện" choices={filterChoices} value={filter} onChange={(value) => setFilter(value || 'warning')} />
      </div>

      <p className="text-sm text-ink-muted" aria-live="polite">
        {lots.isPending ? 'Đang tải…' : `${totalCount} lô`}
      </p>
      {lots.isError && <p className="font-semibold text-bad">{lots.error.message}</p>}

      {rows.length > 0 && (
        <DataTable
          caption="Lô cần xử lý hạn dùng, hạn gấp nhất trước"
          rows={rows}
          columns={columns}
          rowKey={(lot) => `${lot.lotId}-${lot.locationId}`}
          rowHref={(lot) => `/ton-kho/${lot.productId}`}
          dimmed={lots.isPlaceholderData}
          renderCard={(lot) => (
            <>
              <span className="flex items-baseline justify-between gap-2">
                <span className="min-w-0 font-semibold">{lot.productName}</span>
                <span className="shrink-0 font-semibold tabular-nums">
                  {lot.qty} {lot.baseUnitName}
                </span>
              </span>
              <span className="text-sm text-ink-muted tabular-nums">
                {lotLabel(lot)} · HSD {lot.expiryDate ? formatDate(lot.expiryDate) : '—'} · {lot.locationName}
              </span>
              <span className="self-start">
                <DaysLeft lot={lot} today={today} />
              </span>
            </>
          )}
        />
      )}
      {lots.isSuccess && rows.length === 0 && (
        <p className="rounded-xl border border-dashed border-line bg-surface px-4 py-6 text-center text-ink-muted">Không có lô nào cần xử lý.</p>
      )}

      {lots.hasNextPage && (
        <button
          type="button"
          onClick={() => void lots.fetchNextPage()}
          disabled={lots.isFetchingNextPage}
          className="min-h-11 rounded-lg border border-line bg-surface px-4 font-semibold active:bg-brand-50 disabled:opacity-60 lg:self-start"
        >
          {lots.isFetchingNextPage ? 'Đang tải…' : `Xem thêm (còn ${totalCount - rows.length})`}
        </button>
      )}
    </div>
  )
}
