import { Outlet } from 'react-router'
import { useStoreSettings } from '../features/store/hooks/useStore'

// Khung chung mọi màn hình: thanh đầu trang có tên cửa hàng, nội dung route bên dưới.
export function AppLayout() {
  const store = useStoreSettings()

  return (
    <div className="flex min-h-dvh flex-col">
      <header className="sticky top-0 z-10 bg-brand-600 pt-[env(safe-area-inset-top)] text-white shadow-sm">
        <div className="mx-auto flex h-14 max-w-3xl items-center gap-3 px-4">
          <img src="/logo-mark.svg" alt="" className="size-8" />
          <span className="truncate text-lg font-semibold">{store.data?.name ?? 'FoMart'}</span>
        </div>
      </header>
      <main className="mx-auto w-full max-w-3xl flex-1 px-4 py-4 pb-[max(1rem,env(safe-area-inset-bottom))]">
        <Outlet />
      </main>
    </div>
  )
}
