import { CaretRightIcon, PlusIcon } from '@phosphor-icons/react'
import { useState } from 'react'
import { Link, useNavigate } from 'react-router'
import type { GoodsReceiptListItemResponse, GoodsReceiptSearchParams } from '../../../api/purchasing'
import { ApiError } from '../../../api/types'
import { formatDateTime, formatMoney } from '../../../lib/format'
import { useCreateReceipt, useReceiptList } from '../hooks/useGoodsReceipts'

const DRAFTS: GoodsReceiptSearchParams = { status: 'draft', pageSize: 20 }
const RECENT: GoodsReceiptSearchParams = { pageSize: 15 }

const statusText = { draft: 'Nháp', completed: 'Đã nhập', cancelled: 'Đã hủy' } as const
const statusClass = { draft: 'bg-warn/10 text-warn', completed: 'bg-ok/10 text-ok', cancelled: 'bg-bad/10 text-bad' } as const

// Danh sách phiếu nhập (UC-04, SC-10): phiếu nháp đang làm dở ở trên, các phiếu gần đây ở dưới.
export function ReceiptListPage() {
  const drafts = useReceiptList(DRAFTS)
  const recent = useReceiptList(RECENT)
  const create = useCreateReceipt()
  const navigate = useNavigate()
  const [error, setError] = useState<string | null>(null)

  const startNew = () => {
    setError(null)
    create.mutate(
      {},
      {
        onSuccess: (receipt) => void navigate(`/nhap-hang/${receipt.id}`),
        onError: (caught) => setError(caught instanceof ApiError ? caught.message : 'Không tạo được phiếu nhập.'),
      },
    )
  }

  const recentItems = (recent.data?.items ?? []).filter((item) => item.status !== 'draft')

  return (
    <div className="mx-auto flex max-w-2xl flex-col gap-4">
      <div className="flex items-center justify-between gap-3">
        <h1 className="text-2xl font-bold">Nhập hàng</h1>
        <button
          type="button"
          onClick={startNew}
          disabled={create.isPending}
          className="flex h-12 items-center gap-2 rounded-lg bg-brand-600 px-4 font-semibold text-white active:bg-brand-700 disabled:opacity-60"
        >
          <PlusIcon size="1.25em" weight="bold" aria-hidden="true" />
          Phiếu nhập mới
        </button>
      </div>
      {error && (
        <p role="alert" className="font-semibold text-bad">
          {error}
        </p>
      )}

      <ReceiptSection title="Đang làm dở" query={drafts} items={drafts.data?.items ?? []} empty="Không có phiếu nháp nào." />
      <ReceiptSection title="Gần đây" query={recent} items={recentItems} empty="Chưa có phiếu nhập nào." />
    </div>
  )
}

interface ReceiptSectionProps {
  title: string
  query: { isPending: boolean; isError: boolean; error: Error | null }
  items: GoodsReceiptListItemResponse[]
  empty: string
}

function ReceiptSection({ title, query, items, empty }: ReceiptSectionProps) {
  return (
    <section className="flex flex-col gap-2">
      <h2 className="text-lg font-semibold">{title}</h2>
      {query.isPending && <p className="text-sm text-ink-muted">Đang tải…</p>}
      {query.isError && <p className="text-sm font-semibold text-bad">{query.error?.message}</p>}
      {!query.isPending && !query.isError && items.length === 0 && <p className="text-sm text-ink-muted">{empty}</p>}
      {items.length > 0 && (
        <ul className="flex flex-col overflow-hidden rounded-xl border border-line bg-surface">
          {items.map((item) => (
            <li key={item.id} className="border-b border-line last:border-b-0">
              <Link to={`/nhap-hang/${item.id}`} className="flex min-h-16 items-center gap-3 px-3 py-2.5 active:bg-brand-50">
                <span className="flex min-w-0 flex-1 flex-col gap-0.5">
                  <span className="flex items-center gap-2">
                    <span className="font-semibold tabular-nums">{item.code}</span>
                    <span className={`rounded px-1.5 text-xs font-semibold ${statusClass[item.status]}`}>{statusText[item.status]}</span>
                  </span>
                  <span className="truncate text-sm text-ink-muted">
                    {item.supplierName ?? 'Chưa chọn nhà cung cấp'} · {item.lineCount} dòng · {formatDateTime(item.completedAt ?? item.createdAt)}
                  </span>
                </span>
                {item.total !== null && <span className="shrink-0 font-semibold tabular-nums">{formatMoney(item.total)}</span>}
                <CaretRightIcon size="1.25rem" className="shrink-0 text-ink-muted" aria-hidden="true" />
              </Link>
            </li>
          ))}
        </ul>
      )}
    </section>
  )
}
