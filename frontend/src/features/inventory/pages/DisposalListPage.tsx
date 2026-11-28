import { PlusIcon } from '@phosphor-icons/react'
import { useState } from 'react'
import { Link } from 'react-router'
import type { DisposalListItemResponse, DisposalStatus } from '../../../api/inventory'
import { ChoiceChips, type Choice } from '../../../components/ui/ChoiceChips'
import { DataTable, type DataColumn } from '../../../components/ui/DataTable'
import { formatDateTime, formatMoney } from '../../../lib/format'
import { DisposalStatusBadge } from '../components/DisposalStatusBadge'
import { useDisposalList } from '../hooks/useDisposals'

const statusChoices: Choice<DisposalStatus>[] = [
  { value: '', label: 'Tất cả' },
  { value: 'pending', label: 'Chờ duyệt' },
  { value: 'approved', label: 'Đã hủy' },
  { value: 'rejected', label: 'Từ chối' },
]

// Cột của bảng phiếu hủy trên máy tính (DESIGN.md > Tables). Tổn thất trống khi chưa duyệt hoặc bị ẩn giá vốn (BR-08).
const columns: DataColumn<DisposalListItemResponse>[] = [
  { header: 'Mã', cell: (item) => <span className="tabular-nums">{item.code}</span> },
  { header: 'Lập lúc', cell: (item) => <span className="tabular-nums">{formatDateTime(item.createdAt)}</span> },
  { header: 'Người lập', cell: (item) => item.createdByName },
  { header: 'Số dòng', cell: (item) => item.lineCount, align: 'right' },
  { header: 'Số lượng', cell: (item) => item.totalBaseQty, align: 'right' },
  { header: 'Tổn thất', cell: (item) => (item.lossValue !== null ? <span className="font-semibold">{formatMoney(item.lossValue)}</span> : '—'), align: 'right' },
  { header: 'Trạng thái', cell: (item) => <DisposalStatusBadge status={item.status} /> },
  { header: 'Duyệt bởi', cell: (item) => item.decidedByName ?? '—', className: 'hidden xl:table-cell' },
]

// Phiếu xuất hủy (UC-08, SC-12): lọc chờ duyệt / đã hủy / từ chối; chủ tiệm vào đây để duyệt phiếu của nhân viên (BR-21).
export function DisposalListPage() {
  const [status, setStatus] = useState<DisposalStatus | ''>('')
  const list = useDisposalList(status || undefined)
  const items = list.data?.pages.flatMap((page) => page.items) ?? []
  const totalCount = list.data?.pages[0]?.totalCount ?? 0

  return (
    <div className="mx-auto flex max-w-2xl flex-col gap-4 lg:max-w-none">
      <div className="flex items-center justify-between gap-3">
        <h1 className="text-2xl font-bold">Xuất hủy</h1>
        <Link to="/xuat-huy/moi" className="flex h-12 items-center gap-2 rounded-lg bg-brand-600 px-4 font-semibold text-white active:bg-brand-700">
          <PlusIcon size="1.25em" weight="bold" aria-hidden="true" />
          Lập phiếu hủy
        </Link>
      </div>

      <div className="rounded-xl border border-line bg-surface p-3 lg:p-4">
        <ChoiceChips label="Trạng thái" choices={statusChoices} value={status} onChange={setStatus} />
      </div>

      <p className="text-sm text-ink-muted" aria-live="polite">
        {list.isPending ? 'Đang tải…' : `${totalCount} phiếu`}
      </p>
      {list.isError && <p className="font-semibold text-bad">{list.error.message}</p>}

      {items.length > 0 && (
        <DataTable
          caption="Phiếu xuất hủy, mới nhất trước"
          rows={items}
          columns={columns}
          rowKey={(item) => item.id}
          rowHref={(item) => `/xuat-huy/${item.id}`}
          renderCard={(item) => (
            <>
              <span className="flex items-baseline justify-between gap-2">
                <span className="flex items-center gap-2">
                  <span className="font-semibold tabular-nums">{item.code}</span>
                  <DisposalStatusBadge status={item.status} />
                </span>
                {item.lossValue !== null && <span className="font-semibold tabular-nums">{formatMoney(item.lossValue)}</span>}
              </span>
              <span className="text-sm text-ink-muted tabular-nums">
                {formatDateTime(item.createdAt)} · {item.createdByName} · {item.lineCount} dòng · {item.totalBaseQty} đơn vị
              </span>
            </>
          )}
        />
      )}
      {list.isSuccess && items.length === 0 && <p className="rounded-xl border border-dashed border-line bg-surface px-4 py-6 text-center text-ink-muted">Chưa có phiếu nào.</p>}

      {list.hasNextPage && (
        <button
          type="button"
          onClick={() => void list.fetchNextPage()}
          disabled={list.isFetchingNextPage}
          className="min-h-11 rounded-lg border border-line bg-surface px-4 font-semibold active:bg-brand-50 disabled:opacity-60 lg:self-start"
        >
          {list.isFetchingNextPage ? 'Đang tải…' : `Xem thêm (còn ${totalCount - items.length})`}
        </button>
      )}
    </div>
  )
}
