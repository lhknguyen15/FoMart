import type { ReactNode } from 'react'
import { Link, useNavigate } from 'react-router'

export interface DataColumn<T> {
  header: string
  cell: (row: T) => ReactNode
  align?: 'left' | 'right'
  className?: string // áp cho cả ô tiêu đề và ô dữ liệu, ví dụ 'hidden xl:table-cell' để ẩn cột phụ khi màn hình hẹp
}

interface DataTableProps<T> {
  caption: string // đọc cho trình đọc màn hình, ẩn trên giao diện
  rows: T[]
  columns: DataColumn<T>[]
  rowKey: (row: T) => string | number
  rowHref?: (row: T) => string | undefined // có (và trả về đường dẫn) thì cả hàng bấm được để mở chi tiết
  dimmed?: boolean // đang tải bộ lọc mới: làm mờ dữ liệu cũ
  // Có thì dưới 1024px hiện danh sách thẻ (nội dung một thẻ do nơi dùng vẽ), từ 1024px hiện bảng (DESIGN.md > Tables).
  renderCard?: (row: T) => ReactNode
}

// Bảng dữ liệu cho khung quản lý trên máy tính (DESIGN.md > Tables): viền tóc, hàng tiêu đề nền giấy,
// kẻ ngang, cột số căn phải. Cả hàng bấm được; ô đầu là liên kết thật để dùng bằng bàn phím.
export function DataTable<T>({ caption, rows, columns, rowKey, rowHref, dimmed = false, renderCard }: DataTableProps<T>) {
  const navigate = useNavigate()

  return (
    <>
      {renderCard && (
        <ul aria-label={caption} className={`flex flex-col overflow-hidden rounded-xl border border-line bg-surface lg:hidden ${dimmed ? 'opacity-60' : ''}`}>
          {rows.map((row) => {
            const href = rowHref?.(row)
            return (
              <li key={rowKey(row)} className="border-b border-line last:border-b-0">
                {href ? (
                  <Link to={href} className="flex min-h-14 flex-col gap-0.5 px-3 py-2.5 active:bg-brand-50">
                    {renderCard(row)}
                  </Link>
                ) : (
                  <div className="flex flex-col gap-0.5 px-3 py-2.5">{renderCard(row)}</div>
                )}
              </li>
            )
          })}
        </ul>
      )}
      <div className={`overflow-x-auto rounded-xl border border-line bg-surface ${renderCard ? 'hidden lg:block' : ''} ${dimmed ? 'opacity-60' : ''}`}>
      <table className="w-full border-collapse text-left">
        <caption className="sr-only">{caption}</caption>
        <thead className="bg-paper">
          <tr>
            {columns.map((column) => (
              <th
                key={column.header}
                scope="col"
                className={`border-b border-line px-3 py-2.5 text-sm font-semibold whitespace-nowrap xl:px-4 text-ink-muted ${column.align === 'right' ? 'text-right' : ''} ${column.className ?? ''}`}
              >
                {column.header}
              </th>
            ))}
          </tr>
        </thead>
        <tbody>
          {rows.map((row) => {
            const href = rowHref?.(row)
            return (
              <tr
                key={rowKey(row)}
                onClick={href ? () => void navigate(href) : undefined}
                className={`border-b border-line last:border-b-0 ${href ? 'cursor-pointer hover:bg-brand-50 focus-within:bg-brand-50' : ''}`}
              >
                {columns.map((column, index) => (
                  <td key={column.header} className={`h-12 px-3 py-2 whitespace-nowrap xl:px-4 ${column.align === 'right' ? 'text-right tabular-nums' : ''} ${column.className ?? ''}`}>
                    {index === 0 && href ? (
                      <Link to={href} onClick={(event) => event.stopPropagation()} className="font-semibold outline-none focus-visible:underline">
                        {column.cell(row)}
                      </Link>
                    ) : (
                      column.cell(row)
                    )}
                  </td>
                ))}
              </tr>
            )
          })}
        </tbody>
      </table>
      </div>
    </>
  )
}
