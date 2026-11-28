import { Link, useParams } from 'react-router'
import type { DocType, StockLotResponse, StockMoveResponse } from '../../../api/inventory'
import { ApiError } from '../../../api/types'
import { DataTable, type DataColumn } from '../../../components/ui/DataTable'
import { formatDate, formatDateTime, formatMoney } from '../../../lib/format'
import { useProduct } from '../../catalog/hooks/useProducts'
import { useProductStock, useStockMoves } from '../hooks/useStock'

const docText: Record<DocType, string> = {
  receipt: 'Nhập hàng',
  receipt_cancel: 'Hủy phiếu nhập',
  sale: 'Bán',
  sale_cancel: 'Hủy hóa đơn',
  transfer_out: 'Chuyển đi',
  transfer_in: 'Chuyển đến',
  disposal: 'Xuất hủy',
  count_adjust: 'Kiểm kê',
}

const badge = 'rounded px-1.5 text-xs font-semibold whitespace-nowrap'
const backLink = (
  <Link to="/ton-kho" className="flex min-h-11 items-center self-start font-semibold text-brand-700 underline-offset-4 hover:underline">
    Về danh sách tồn kho
  </Link>
)

// Lô cho người dùng: số lô in trên thùng + hạn dùng; lô tạm (bán khi kệ chưa có lô, BR-01) ghi rõ.
const lotName = (lot: { lotNo: string | null; expiryDate: string | null }, provisional = false) =>
  provisional ? 'Lô tạm' : [lot.lotNo ? `Lô ${lot.lotNo}` : 'Lô', lot.expiryDate ? `HSD ${formatDate(lot.expiryDate)}` : null].filter(Boolean).join(' · ')

function ExpiryBadge({ lot }: { lot: StockLotResponse }) {
  if (lot.expiryWarning === 'expired') return <span className={`${badge} bg-bad/10 text-bad`}>Đã hết hạn</span>
  if (lot.expiryWarning === 'near_expiry') return <span className={`${badge} bg-warn/10 text-warn`}>Sắp hết hạn</span>
  return null
}

// Chứng từ gốc mở được: hóa đơn, phiếu nhập. Chuyển kho, xuất hủy, kiểm kê chưa có màn hình.
const docHref = (move: StockMoveResponse) =>
  move.saleId ? `/hoa-don/${move.saleId}` : move.receiptId ? `/nhap-hang/${move.receiptId}` : undefined

const qtyText = (qty: number) => (
  <span className={`font-semibold ${qty < 0 ? 'text-bad' : 'text-ok'}`}>{qty > 0 ? `+${qty}` : qty}</span>
)

// Chi tiết tồn một sản phẩm (SC-07, UC-10): tồn theo vị trí, các lô đang còn (hạn dùng, giá vốn), thẻ kho có chứng từ.
export function StockDetailPage() {
  const { productId } = useParams()
  const id = Number(productId)
  const product = useProduct(id)
  const lots = useProductStock(id)
  const moves = useStockMoves(id)

  if (!Number.isInteger(id) || id <= 0 || (product.error instanceof ApiError && product.error.status === 404)) {
    return (
      <div className="flex flex-col gap-2">
        <p className="font-semibold">Không tìm thấy sản phẩm này.</p>
        {backLink}
      </div>
    )
  }
  if (product.isPending) return <p className="text-ink-muted">Đang tải…</p>
  if (product.isError) return <p className="font-semibold text-bad">{product.error.message}</p>

  const data = product.data
  const baseUnit = data.units.find((unit) => unit.isBase)?.name ?? ''
  const total = data.stocks.reduce((sum, stock) => sum + stock.qty, 0)
  const lotRows = lots.data ?? []
  const showCost = lotRows.some((lot) => lot.unitCost !== null)
  const moveRows = moves.data?.pages.flatMap((page) => page.items) ?? []
  const moveCount = moves.data?.pages[0]?.totalCount ?? 0

  const lotColumns: DataColumn<StockLotResponse>[] = [
    { header: 'Lô', cell: (lot) => lotName(lot, lot.lotKind === 'provisional') },
    { header: 'Vị trí', cell: (lot) => lot.locationName },
    { header: 'Ngày nhập', cell: (lot) => <span className="tabular-nums">{formatDate(lot.receivedAt)}</span>, className: 'hidden xl:table-cell' },
    { header: 'Cảnh báo', cell: (lot) => <ExpiryBadge lot={lot} /> },
    ...(showCost ? [{ header: `Giá vốn / ${baseUnit}`, cell: (lot: StockLotResponse) => (lot.unitCost !== null ? formatMoney(Math.round(lot.unitCost)) : '—'), align: 'right' as const }] : []),
    { header: 'Tồn', cell: (lot) => <span className={`font-semibold ${lot.qty < 0 ? 'text-bad' : ''}`}>{lot.qty}</span>, align: 'right' },
  ]

  const moveColumns: DataColumn<StockMoveResponse>[] = [
    { header: 'Thời gian', cell: (move) => <span className="tabular-nums">{formatDateTime(move.createdAt)}</span> },
    { header: 'Chứng từ', cell: (move) => <span className="tabular-nums">{move.docCode}</span> },
    { header: 'Loại', cell: (move) => docText[move.docType] },
    { header: 'Vị trí', cell: (move) => move.locationName, className: 'hidden xl:table-cell' },
    { header: 'Lô', cell: (move) => lotName(move), className: 'hidden 2xl:table-cell' },
    { header: 'Thay đổi', cell: (move) => qtyText(move.qtyChange), align: 'right' },
    { header: 'Tồn sau', cell: (move) => move.balance, align: 'right' },
    { header: 'Người làm', cell: (move) => move.userName, className: 'hidden 2xl:table-cell' },
  ]

  return (
    // Trên cùng: ba khối tóm tắt (một hàng trên máy tính); dưới: bảng lô và thẻ kho dùng hết chiều ngang.
    <div className="mx-auto flex max-w-2xl flex-col gap-4 lg:max-w-none lg:gap-6">
      <div className="flex flex-col gap-4 lg:grid lg:grid-cols-3 lg:items-start">
        <section className="flex flex-col gap-1 rounded-xl border border-line bg-surface px-4 py-3">
          <h1 className="text-xl font-bold">{data.name}</h1>
          <p className="text-sm text-ink-muted">{data.categoryName ?? 'Chưa có nhóm hàng'}</p>
          <p className={`text-3xl font-bold tabular-nums ${total < 0 ? 'text-bad' : ''}`}>
            {total} {baseUnit}
          </p>
          <div className="flex flex-wrap gap-1">
            {total < 0 && <span className={`${badge} bg-bad/10 text-bad`}>Tồn âm</span>}
            {total >= 0 && data.minStock > 0 && total < data.minStock && <span className={`${badge} bg-warn/10 text-warn`}>Sắp hết (tối thiểu {data.minStock})</span>}
            {data.needsCount && <span className={`${badge} bg-warn/10 text-warn`}>Cần kiểm kê</span>}
          </div>
          {data.lastCountedAt && <p className="text-sm text-ink-muted">Kiểm kê lần cuối {formatDate(data.lastCountedAt)}</p>}
        </section>

        <section aria-labelledby="by-location" className="flex flex-col gap-2">
          <h2 id="by-location" className="text-lg font-semibold">
            Theo vị trí
          </h2>
          <ul className="flex flex-col overflow-hidden rounded-xl border border-line bg-surface">
            {data.stocks.length === 0 && <li className="px-4 py-3 text-sm text-ink-muted">Chưa có tồn ở vị trí nào.</li>}
            {data.stocks.map((stock) => (
              <li key={stock.locationId} className="flex items-baseline justify-between gap-2 border-b border-line px-4 py-2.5 last:border-b-0">
                <span>{stock.locationName}</span>
                <span className={`font-semibold tabular-nums ${stock.qty < 0 ? 'text-bad' : ''}`}>
                  {stock.qty} {baseUnit}
                </span>
              </li>
            ))}
          </ul>
        </section>

        <section aria-labelledby="units" className="flex flex-col gap-2">
          <h2 id="units" className="text-lg font-semibold">
            Đơn vị bán
          </h2>
          <ul className="flex flex-col overflow-hidden rounded-xl border border-line bg-surface">
            {data.units
              .filter((unit) => unit.isActive)
              .map((unit) => (
                <li key={unit.id} className="flex items-baseline justify-between gap-2 border-b border-line px-4 py-2.5 last:border-b-0">
                  <span>
                    {unit.name}
                    {unit.factor > 1 ? ` = ${unit.factor} ${baseUnit}` : ''}
                    {unit.barcode && <span className="block text-sm text-ink-muted tabular-nums">{unit.barcode}</span>}
                  </span>
                  <span className="font-semibold tabular-nums">{formatMoney(unit.salePrice)}</span>
                </li>
              ))}
          </ul>
        </section>
      </div>

      <div className="flex min-w-0 flex-col gap-4 lg:gap-6">
        <section aria-labelledby="lots" className="flex flex-col gap-2">
          <h2 id="lots" className="text-lg font-semibold">
            Lô đang còn · {lotRows.length}
          </h2>
          {lots.isPending && <p className="text-sm text-ink-muted">Đang tải…</p>}
          {lots.isError && <p className="text-sm font-semibold text-bad">{lots.error.message}</p>}
          {lots.isSuccess && lotRows.length === 0 && <p className="text-sm text-ink-muted">Không còn lô nào có hàng.</p>}
          {lotRows.length > 0 && (
            <DataTable
              caption="Các lô đang còn hàng"
              rows={lotRows}
              columns={lotColumns}
              rowKey={(lot) => `${lot.lotId}-${lot.locationId}`}
              renderCard={(lot) => (
                <>
                  <span className="flex items-baseline justify-between gap-2">
                    <span className="font-semibold">{lotName(lot, lot.lotKind === 'provisional')}</span>
                    <span className={`font-semibold tabular-nums ${lot.qty < 0 ? 'text-bad' : ''}`}>
                      {lot.qty} {baseUnit}
                    </span>
                  </span>
                  <span className="text-sm text-ink-muted">
                    {lot.locationName} · nhập {formatDate(lot.receivedAt)}
                    {lot.unitCost !== null ? ` · vốn ${formatMoney(Math.round(lot.unitCost))}/${baseUnit}` : ''}
                  </span>
                  <span className="self-start">
                    <ExpiryBadge lot={lot} />
                  </span>
                </>
              )}
            />
          )}
        </section>

        <section aria-labelledby="moves" className="flex flex-col gap-2">
          <h2 id="moves" className="text-lg font-semibold">
            Thẻ kho · {moveCount} biến động
          </h2>
          {moves.isPending && <p className="text-sm text-ink-muted">Đang tải…</p>}
          {moves.isError && <p className="text-sm font-semibold text-bad">{moves.error.message}</p>}
          {moves.isSuccess && moveRows.length === 0 && <p className="text-sm text-ink-muted">Chưa có biến động nào.</p>}
          {moveRows.length > 0 && (
            <DataTable
              caption="Thẻ kho: biến động tồn, mới nhất trước"
              rows={moveRows}
              columns={moveColumns}
              rowKey={(move) => move.id}
              rowHref={docHref}
              renderCard={(move) => (
                <>
                  <span className="flex items-baseline justify-between gap-2">
                    <span className="font-semibold">
                      {docText[move.docType]} <span className="tabular-nums">{move.docCode}</span>
                    </span>
                    <span className="tabular-nums">{qtyText(move.qtyChange)}</span>
                  </span>
                  <span className="text-sm text-ink-muted tabular-nums">
                    {formatDateTime(move.createdAt)} · tồn sau {move.balance} · {move.userName}
                  </span>
                </>
              )}
            />
          )}
          {moves.hasNextPage && (
            <button
              type="button"
              onClick={() => void moves.fetchNextPage()}
              disabled={moves.isFetchingNextPage}
              className="min-h-11 self-start rounded-lg border border-line bg-surface px-4 font-semibold active:bg-brand-50 disabled:opacity-60"
            >
              {moves.isFetchingNextPage ? 'Đang tải…' : `Xem thêm (còn ${moveCount - moveRows.length})`}
            </button>
          )}
        </section>
        {backLink}
      </div>
    </div>
  )
}
