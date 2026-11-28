import { MagnifyingGlassIcon } from '@phosphor-icons/react'
import { useState } from 'react'
import type { ProductListItemResponse, ProductStockFilter } from '../../../api/catalog'
import { ChoiceChips, type Choice } from '../../../components/ui/ChoiceChips'
import { DataTable, type DataColumn } from '../../../components/ui/DataTable'
import { useProductBrowse } from '../../catalog/hooks/useProducts'
import { StockBadges } from '../components/StockBadges'

const filterChoices: Choice<ProductStockFilter>[] = [
  { value: '', label: 'Tất cả' },
  { value: 'low', label: 'Sắp hết' },
  { value: 'negative', label: 'Tồn âm' },
  { value: 'needs_count', label: 'Cần kiểm kê' },
]

// Tồn tính theo đơn vị cơ sở (quyết định số 2); số âm in màu lỗi kèm nhãn "Tồn âm".
const stockText = (item: ProductListItemResponse) => (
  <span className={`font-semibold ${item.totalStock < 0 ? 'text-bad' : ''}`}>
    {item.totalStock} {item.baseUnitName}
  </span>
)

// Cột của bảng tồn kho trên máy tính (DESIGN.md > Tables).
const columns: DataColumn<ProductListItemResponse>[] = [
  { header: 'Sản phẩm', cell: (item) => item.name },
  { header: 'Nhóm hàng', cell: (item) => item.categoryName ?? <span className="text-ink-muted">—</span>, className: 'hidden xl:table-cell' },
  { header: 'Tồn', cell: stockText, align: 'right' },
  { header: 'Tối thiểu', cell: (item) => (item.minStock > 0 ? item.minStock : '—'), align: 'right' },
  { header: 'Tình trạng', cell: (item) => <StockBadges item={item} /> },
]

// Tồn kho (UC-10, SC-06/07): danh sách sản phẩm và tổng tồn, lọc sắp hết / tồn âm / cần kiểm kê; chạm để xem lô và thẻ kho.
export function StockListPage() {
  const [keyword, setKeyword] = useState('')
  const [filter, setFilter] = useState<ProductStockFilter | ''>('')
  const products = useProductBrowse({ keyword: keyword.trim() || undefined, stockFilter: filter || undefined, pageSize: 30 })
  const items = products.data?.pages.flatMap((page) => page.items) ?? []
  const totalCount = products.data?.pages[0]?.totalCount ?? 0

  return (
    <div className="mx-auto flex max-w-2xl flex-col gap-4 lg:max-w-none">
      <h1 className="text-2xl font-bold">Tồn kho</h1>

      <div className="flex flex-col gap-3 rounded-xl border border-line bg-surface p-3 lg:flex-row lg:items-end lg:gap-6 lg:p-4">
        <div className="flex flex-col gap-1 lg:w-96">
          <label htmlFor="stock-search" className="text-sm font-semibold">
            Tìm sản phẩm
          </label>
          <div className="relative">
            <MagnifyingGlassIcon size="1.125rem" aria-hidden="true" className="pointer-events-none absolute top-1/2 left-3 -translate-y-1/2 text-ink-muted" />
            <input
              id="stock-search"
              type="search"
              autoComplete="off"
              placeholder="Tên không dấu hoặc mã vạch"
              value={keyword}
              onChange={(event) => setKeyword(event.target.value)}
              className="h-12 w-full rounded-lg border border-line bg-surface pr-3 pl-9 text-base focus:border-brand-600 focus:outline-2 focus:outline-brand-500/40"
            />
          </div>
        </div>
        <ChoiceChips label="Lọc theo tồn" choices={filterChoices} value={filter} onChange={setFilter} />
      </div>

      <p className="text-sm text-ink-muted" aria-live="polite">
        {products.isPending ? 'Đang tải…' : `${totalCount} sản phẩm`}
      </p>
      {products.isError && <p className="font-semibold text-bad">{products.error.message}</p>}

      {items.length > 0 && (
        <DataTable
          caption="Tồn kho theo sản phẩm"
          rows={items}
          columns={columns}
          rowKey={(item) => item.id}
          rowHref={(item) => `/ton-kho/${item.id}`}
          dimmed={products.isPlaceholderData}
          renderCard={(item) => (
            <>
              <span className="flex items-baseline justify-between gap-2">
                <span className="min-w-0 font-semibold">{item.name}</span>
                <span className="shrink-0 tabular-nums">{stockText(item)}</span>
              </span>
              <StockBadges item={item} />
            </>
          )}
        />
      )}
      {!products.isPending && !products.isError && items.length === 0 && (
        <p className="rounded-xl border border-dashed border-line bg-surface px-4 py-6 text-center text-ink-muted">Không có sản phẩm nào khớp.</p>
      )}

      {products.hasNextPage && (
        <button
          type="button"
          onClick={() => void products.fetchNextPage()}
          disabled={products.isFetchingNextPage}
          className="min-h-11 rounded-lg border border-line bg-surface px-4 font-semibold active:bg-brand-50 disabled:opacity-60 lg:self-start"
        >
          {products.isFetchingNextPage ? 'Đang tải…' : `Xem thêm (còn ${totalCount - items.length})`}
        </button>
      )}
    </div>
  )
}
