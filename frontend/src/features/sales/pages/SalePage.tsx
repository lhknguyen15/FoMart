import { useStoreSettings } from '../../store/hooks/useStore'

// Màn hình bán hàng (UC-05, SC-03): mở sẵn khi vào ứng dụng. Hiện chỉ kiểm tra kết nối máy chủ;
// giỏ hàng và quét mã làm ở các bước sau.
export function SalePage() {
  const store = useStoreSettings()

  return (
    <section className="flex flex-col gap-4">
      <h1 className="text-xl font-semibold">Bán hàng</h1>

      <div className="rounded-xl border border-line bg-surface p-4">
        <p className="text-sm text-ink-muted">Kết nối máy chủ</p>
        {store.isPending && <p className="mt-1 font-medium">Đang kiểm tra…</p>}
        {store.isError && <p className="mt-1 font-medium text-bad">{store.error.message}</p>}
        {store.isSuccess && (
          <p className="mt-1 font-medium text-ok">
            Đã kết nối: {store.data.name}
            {store.data.address ? ` · ${store.data.address}` : ''}
          </p>
        )}
      </div>
    </section>
  )
}
