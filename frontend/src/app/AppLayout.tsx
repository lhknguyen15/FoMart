import { SignOutIcon } from '@phosphor-icons/react'
import { Outlet } from 'react-router'
import { useLogout } from '../features/auth/hooks/useAuth'
import { useStoreSettings } from '../features/store/hooks/useStore'
import { useAuthStore } from '../stores/useAuthStore'
import { AppNav } from './AppNav'

const roleText = { owner: 'Chủ tiệm', staff: 'Nhân viên' } as const

// Khung chung mọi màn hình (DESIGN.md > Layout).
// < 1024px: thanh đầu trang xanh + hàng tab, một cột (khung tác vụ, điện thoại trước).
// ≥ 1024px: menu bên trái dính toàn chiều cao + vùng nội dung dùng hết chiều rộng (khung quản lý).
export function AppLayout() {
  const store = useStoreSettings()
  const user = useAuthStore((state) => state.session?.user)
  const logout = useLogout()
  const storeName = store.data?.name ?? 'FoMart'
  const logoutLabel = user ? `Đăng xuất ${user.name}` : 'Đăng xuất'

  return (
    <div className="min-h-dvh lg:grid lg:grid-cols-[5rem_minmax(0,1fr)] xl:grid-cols-[15rem_minmax(0,1fr)]">
      <aside className="sticky top-0 hidden h-dvh flex-col gap-6 bg-brand-600 py-5 text-white lg:flex">
        <div className="flex items-center justify-center gap-3 px-5 xl:justify-start">
          <img src="/logo-mark.svg" alt="" className="size-10 shrink-0" />
          <div className="hidden min-w-0 flex-col xl:flex">
            <span className="truncate text-lg leading-tight font-semibold" title={storeName}>
              {storeName}
            </span>
            <span className="text-sm text-brand-50">FoMart</span>
          </div>
        </div>
        <div className="flex-1 overflow-y-auto">
          <AppNav variant="sidebar" />
        </div>
        {user && (
          <div className="mx-3 flex items-center justify-center gap-2 border-t border-white/20 pt-4 xl:justify-start xl:px-2">
            <div className="hidden min-w-0 flex-1 flex-col xl:flex">
              <span className="truncate font-semibold" title={user.name}>
                {user.name}
              </span>
              <span className="text-sm text-brand-50">{roleText[user.role]}</span>
            </div>
            <button
              type="button"
              onClick={() => logout.mutate()}
              disabled={logout.isPending}
              aria-label={logoutLabel}
              title="Đăng xuất"
              className="flex size-11 shrink-0 items-center justify-center rounded-lg hover:bg-brand-700 disabled:opacity-60 focus-visible:outline-2 focus-visible:outline-white"
            >
              <SignOutIcon size="1.5rem" />
            </button>
          </div>
        )}
      </aside>

      <div className="flex min-h-dvh min-w-0 flex-col">
        <header className="sticky top-0 z-10 bg-brand-600 pt-[env(safe-area-inset-top)] text-white shadow-sm lg:hidden">
          <div className="mx-auto flex h-14 max-w-5xl items-center gap-3 px-4">
            <img src="/logo-mark.svg" alt="" className="size-8 shrink-0" />
            <span className="min-w-0 flex-1 truncate text-lg font-semibold">{storeName}</span>
            {user && (
              <span className="hidden max-w-40 truncate text-sm text-brand-50 sm:inline" title={user.name}>
                {user.name}
              </span>
            )}
            <button
              type="button"
              onClick={() => logout.mutate()}
              disabled={logout.isPending}
              aria-label={logoutLabel}
              className="-mr-2 flex size-11 shrink-0 items-center justify-center rounded-lg text-white hover:bg-brand-700 disabled:opacity-60 focus-visible:outline-2 focus-visible:outline-offset-0 focus-visible:outline-white"
            >
              <SignOutIcon size="1.5rem" />
            </button>
          </div>
          <AppNav variant="tabs" />
        </header>
        <main className="mx-auto w-full max-w-5xl flex-1 px-4 py-4 pb-[max(1rem,env(safe-area-inset-bottom))] lg:max-w-360 lg:px-8 lg:py-6">
          <Outlet />
        </main>
      </div>
    </div>
  )
}
