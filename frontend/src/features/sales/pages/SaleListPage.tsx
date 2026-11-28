import { CaretRightIcon, MagnifyingGlassIcon } from '@phosphor-icons/react'
import { useState } from 'react'
import { Link } from 'react-router'
import type { PaymentMethod, SaleListItemResponse, SaleSearchParams, SaleStatus } from '../../../api/sales'
import { ChoiceChips, type Choice } from '../../../components/ui/ChoiceChips'
import { DataTable, type DataColumn } from '../../../components/ui/DataTable'
import { formatDate, formatDateTime, formatMoney, formatPaymentMethod, formatTime, todayInVietnam } from '../../../lib/format'
import { SaleStatusBadge } from '../components/SaleStatusBadge'
import { useSaleHistory } from '../hooks/useSales'

const statusChoices: Choice<SaleStatus>[] = [
  { value: '', label: 'Tất cả' },
  { value: 'paid', label: 'Đã thanh toán' },
  { value: 'pending_payment', label: 'Chờ CK' },
  { value: 'cancelled', label: 'Đã hủy' },
]
const paymentChoices: Choice<PaymentMethod>[] = [
  { value: '', label: 'Mọi hình thức' },
  { value: 'cash', label: 'Tiền mặt' },
  { value: 'transfer', label: 'Chuyển khoản' },
]

// Cột của bảng hóa đơn trên máy tính (DESIGN.md > Tables). Đang xem một ngày thì cột thời gian chỉ ghi giờ;
// tìm theo mã (nhiều ngày) thì ghi đủ ngày giờ.
const saleColumns = (oneDay: boolean): DataColumn<SaleListItemResponse>[] => [
  { header: 'Mã', cell: (sale) => <span className="tabular-nums">{sale.code}</span> },
  {
    header: oneDay ? 'Giờ' : 'Thời gian',
    cell: (sale) => <span className="tabular-nums">{oneDay ? formatTime(sale.createdAt) : formatDateTime(sale.createdAt)}</span>,
  },
  { header: 'Hình thức', cell: (sale) => formatPaymentMethod(sale.paymentMethod) },
  { header: 'Người bán', cell: (sale) => sale.createdByName },
  { header: 'Số món', cell: (sale) => sale.lineCount, align: 'right' },
  {
    header: 'Tổng',
    cell: (sale) => <span className={`font-semibold ${sale.status === 'cancelled' ? 'text-ink-muted line-through' : ''}`}>{formatMoney(sale.total)}</span>,
    align: 'right',
  },
  { header: 'Trạng thái', cell: (sale) => <SaleStatusBadge status={sale.status} /> },
]

const inputClass = 'h-12 w-full rounded-lg border border-line bg-surface px-3 text-base focus:border-brand-600 focus:outline-2 focus:outline-brand-500/40'

// Danh sách hóa đơn (SC-04, UC-05/06): lọc theo ngày, trạng thái, hình thức, mã; chạm để xem chi tiết và hủy.
export function SaleListPage() {
  const [day, setDay] = useState(todayInVietnam)
  const [status, setStatus] = useState<SaleStatus | ''>('')
  const [payment, setPayment] = useState<PaymentMethod | ''>('')
  const [keyword, setKeyword] = useState('')

  // Gõ mã hóa đơn thì tìm trên mọi ngày; không gõ thì xem theo ngày đã chọn.
  const code = keyword.trim()
  const params: Omit<SaleSearchParams, 'page'> = {
    keyword: code || undefined,
    from: code ? undefined : day,
    to: code ? undefined : day,
    status: status || undefined,
    paymentMethod: payment || undefined,
    pageSize: 20,
  }
  const history = useSaleHistory(params)
  const items = history.data?.pages.flatMap((page) => page.items) ?? []
  const totalCount = history.data?.pages[0]?.totalCount ?? 0

  return (
    <div className="mx-auto flex max-w-2xl flex-col gap-4 lg:max-w-none">
      <h1 className="text-2xl font-bold">Hóa đơn</h1>

      <div className="flex flex-col gap-3 rounded-xl border border-line bg-surface p-3 lg:flex-row lg:flex-wrap lg:items-end lg:gap-x-6 lg:p-4">
        <div className="grid grid-cols-2 gap-2 lg:w-[28rem]">
          <div className="flex flex-col gap-1">
            <label htmlFor="sale-day" className="text-sm font-semibold">
              Ngày bán
            </label>
            <input id="sale-day" type="date" value={day} max={todayInVietnam()} disabled={code !== ''} onChange={(event) => setDay(event.target.value || todayInVietnam())} className={`${inputClass} disabled:opacity-60`} />
          </div>
          <div className="flex flex-col gap-1">
            <label htmlFor="sale-code" className="text-sm font-semibold">
              Mã hóa đơn
            </label>
            <div className="relative">
              <MagnifyingGlassIcon size="1.125rem" aria-hidden="true" className="pointer-events-none absolute top-1/2 left-3 -translate-y-1/2 text-ink-muted" />
              <input id="sale-code" type="search" autoComplete="off" placeholder="HD…" value={keyword} onChange={(event) => setKeyword(event.target.value)} className={`${inputClass} pl-9 uppercase`} />
            </div>
          </div>
        </div>
        <ChoiceChips label="Trạng thái" choices={statusChoices} value={status} onChange={setStatus} />
        <ChoiceChips label="Hình thức" choices={paymentChoices} value={payment} onChange={setPayment} />
      </div>

      <p className="text-sm text-ink-muted" aria-live="polite">
        {history.isPending ? 'Đang tải…' : `${totalCount} hóa đơn ${code ? `có mã chứa “${code}”` : `ngày ${formatDate(day)}`}`}
      </p>
      {history.isError && <p className="font-semibold text-bad">{history.error.message}</p>}

      {items.length > 0 && (
        <div className="hidden lg:block">
          <DataTable
            caption="Danh sách hóa đơn"
            rows={items}
            rowKey={(sale) => sale.id}
            rowHref={(sale) => `/hoa-don/${sale.id}`}
            dimmed={history.isPlaceholderData}
            columns={saleColumns(code === '')}
          />
        </div>
      )}
      {items.length > 0 && (
        <ul className={`flex flex-col overflow-hidden rounded-xl border border-line bg-surface lg:hidden ${history.isPlaceholderData ? 'opacity-60' : ''}`}>
          {items.map((sale) => (
            <li key={sale.id} className="border-b border-line last:border-b-0">
              <Link to={`/hoa-don/${sale.id}`} className="flex min-h-16 items-center gap-3 px-3 py-2.5 active:bg-brand-50">
                <span className="flex min-w-0 flex-1 flex-col gap-0.5">
                  <span className="flex flex-wrap items-center gap-2">
                    <span className="font-semibold tabular-nums">{sale.code}</span>
                    <SaleStatusBadge status={sale.status} />
                  </span>
                  <span className="truncate text-sm text-ink-muted">
                    {formatDateTime(sale.createdAt)} · {formatPaymentMethod(sale.paymentMethod)} · {sale.lineCount} món · {sale.createdByName}
                  </span>
                </span>
                <span className={`shrink-0 font-semibold tabular-nums ${sale.status === 'cancelled' ? 'text-ink-muted line-through' : ''}`}>{formatMoney(sale.total)}</span>
                <CaretRightIcon size="1.25rem" className="shrink-0 text-ink-muted" aria-hidden="true" />
              </Link>
            </li>
          ))}
        </ul>
      )}
      {!history.isPending && !history.isError && items.length === 0 && <p className="rounded-xl border border-dashed border-line bg-surface px-4 py-6 text-center text-ink-muted">Không có hóa đơn nào.</p>}

      {history.hasNextPage && (
        <button
          type="button"
          onClick={() => void history.fetchNextPage()}
          disabled={history.isFetchingNextPage}
          className="min-h-11 rounded-lg border border-line bg-surface px-4 font-semibold active:bg-brand-50 disabled:opacity-60 lg:self-start"
        >
          {history.isFetchingNextPage ? 'Đang tải…' : `Xem thêm (còn ${totalCount - items.length})`}
        </button>
      )}
    </div>
  )
}
