import type { ProductListItemResponse } from '../../../api/catalog'

const badge = 'rounded px-1.5 text-xs font-semibold whitespace-nowrap'

// Nhãn tình trạng tồn của một sản phẩm, bằng chữ (DESIGN.md: Not-Only-Color): tồn âm, sắp hết, cần kiểm kê, ngừng bán.
export function StockBadges({ item }: { item: ProductListItemResponse }) {
  const negative = item.totalStock < 0
  const low = !negative && item.minStock > 0 && item.totalStock < item.minStock
  return (
    <span className="flex flex-wrap gap-1">
      {negative && <span className={`${badge} bg-bad/10 text-bad`}>Tồn âm</span>}
      {low && <span className={`${badge} bg-warn/10 text-warn`}>Sắp hết</span>}
      {item.needsCount && <span className={`${badge} bg-warn/10 text-warn`}>Cần kiểm kê</span>}
      {item.status === 'inactive' && <span className={`${badge} bg-line text-ink-muted`}>Ngừng bán</span>}
      {item.status === 'incomplete' && <span className={`${badge} bg-line text-ink-muted`}>Cần bổ sung</span>}
    </span>
  )
}
