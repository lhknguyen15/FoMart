import { Link } from 'react-router'

// Đường dẫn không tồn tại.
export function NotFoundPage() {
  return (
    <div className="flex flex-col items-start gap-3 py-10">
      <h1 className="text-xl font-semibold">Không tìm thấy trang</h1>
      <p className="text-ink-muted">Đường dẫn này không có trong FoMart.</p>
      <Link to="/" className="rounded-lg bg-brand-600 px-4 py-3 font-medium text-white">
        Về màn hình bán hàng
      </Link>
    </div>
  )
}
