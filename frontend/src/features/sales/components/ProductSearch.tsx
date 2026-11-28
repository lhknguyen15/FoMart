import { MagnifyingGlassIcon } from '@phosphor-icons/react'
import { useState, type FormEvent } from 'react'
import type { ProductListItemResponse, ProductResponse } from '../../../api/catalog'
import { ApiError } from '../../../api/types'
import { formatMoney } from '../../../lib/format'
import type { DeclaredItem } from '../../../stores/useCartStore'
import { useProductLoader, useProductSearch } from '../../catalog/hooks/useProducts'

interface ProductSearchProps {
  onAdd: (item: DeclaredItem) => void
  // Gõ đủ một mã số rồi Enter (hoặc máy quét bàn phím khi ô đang được chọn): xử lý như quét mã.
  onBarcode: (code: string) => void
}

// Tìm hàng không có mã hoặc không quét được (UC-05 nhánh phụ): gõ tên không dấu, chạm để thêm.
// Sản phẩm có nhiều đơn vị đang bán (lon, lốc, thùng) thì chọn đơn vị trước khi thêm.
export function ProductSearch({ onAdd, onBarcode }: ProductSearchProps) {
  const [keyword, setKeyword] = useState('')
  const [choosing, setChoosing] = useState<ProductResponse | null>(null)
  const [loadingId, setLoadingId] = useState<number | null>(null)
  const [error, setError] = useState<string | null>(null)
  const search = useProductSearch(keyword)
  const loadProduct = useProductLoader()

  const clear = () => {
    setKeyword('')
    setChoosing(null)
    setError(null)
  }

  const submit = (event: FormEvent) => {
    event.preventDefault()
    const text = keyword.trim()
    if (/^[0-9]{6,14}$/.test(text)) {
      onBarcode(text)
      clear()
    }
  }

  const pick = async (item: ProductListItemResponse) => {
    setError(null)
    setLoadingId(item.id)
    try {
      const product = await loadProduct(item.id)
      const units = product.units.filter((unit) => unit.isActive)
      const only = units.length === 1 ? units[0] : undefined
      if (only) {
        onAdd({ unitId: only.id, productName: product.name, unitName: only.name, unitPrice: only.salePrice })
        clear()
      } else {
        setChoosing(product)
      }
    } catch (caught) {
      setError(caught instanceof ApiError ? caught.message : 'Không tải được sản phẩm.')
    } finally {
      setLoadingId(null)
    }
  }

  const results = search.data?.items ?? []
  const showResults = keyword.trim().length >= 2 && !choosing

  return (
    <div className="flex flex-col gap-2">
      <form onSubmit={submit} role="search">
        <label htmlFor="sale-search" className="sr-only">
          Tìm hàng theo tên hoặc mã
        </label>
        <div className="relative">
          <MagnifyingGlassIcon size="1.25rem" aria-hidden="true" className="pointer-events-none absolute top-1/2 left-3 -translate-y-1/2 text-ink-muted" />
          <input
            id="sale-search"
            type="search"
            autoComplete="off"
            enterKeyHint="search"
            placeholder="Tìm theo tên hoặc gõ mã"
            value={keyword}
            onChange={(event) => {
              setKeyword(event.target.value)
              setChoosing(null)
            }}
            className="h-12 w-full rounded-lg border border-line bg-surface pr-3 pl-10 text-base focus:border-brand-600 focus:outline-2 focus:outline-brand-500/40"
          />
        </div>
      </form>

      {error && <p className="text-sm font-semibold text-bad">{error}</p>}

      {choosing && (
        <div className="flex flex-col gap-2 rounded-xl border border-brand-600 bg-brand-50 p-3">
          <p className="font-semibold">{choosing.name}: chọn đơn vị</p>
          <div className="flex flex-wrap gap-2">
            {choosing.units
              .filter((unit) => unit.isActive)
              .map((unit) => (
                <button
                  key={unit.id}
                  type="button"
                  onClick={() => {
                    onAdd({ unitId: unit.id, productName: choosing.name, unitName: unit.name, unitPrice: unit.salePrice })
                    clear()
                  }}
                  className="flex min-h-12 flex-col items-start justify-center rounded-lg border border-line bg-surface px-3 py-1.5 text-left active:bg-brand-100"
                >
                  <span className="font-semibold">{unit.name}{unit.factor > 1 ? ` (${unit.factor})` : ''}</span>
                  <span className="text-sm tabular-nums text-ink-muted">{formatMoney(unit.salePrice)}</span>
                </button>
              ))}
          </div>
        </div>
      )}

      {showResults && (
        <ul className="flex flex-col overflow-hidden rounded-xl border border-line bg-surface" aria-live="polite">
          {search.isPending && <li className="px-4 py-3 text-sm text-ink-muted">Đang tìm…</li>}
          {search.isError && <li className="px-4 py-3 text-sm font-semibold text-bad">{search.error.message}</li>}
          {search.isSuccess && results.length === 0 && (
            <li className="px-4 py-3 text-sm text-ink-muted">Không có sản phẩm nào khớp “{keyword.trim()}”.</li>
          )}
          {results.map((item) => (
            <li key={item.id} className="border-b border-line last:border-b-0">
              <button
                type="button"
                onClick={() => void pick(item)}
                disabled={loadingId !== null}
                className="flex min-h-14 w-full items-center gap-3 px-4 py-2 text-left active:bg-brand-50 disabled:opacity-60"
              >
                <span className="flex min-w-0 flex-1 flex-col">
                  <span className="truncate font-semibold">{item.name}</span>
                  <span className="text-sm text-ink-muted tabular-nums">
                    {item.baseUnitName} · {formatMoney(item.baseSalePrice)} · tồn {item.totalStock}
                  </span>
                </span>
                {loadingId === item.id && <span className="text-sm text-ink-muted">Đang mở…</span>}
              </button>
            </li>
          ))}
        </ul>
      )}
    </div>
  )
}
