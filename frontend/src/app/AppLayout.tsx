import { PackageIcon, ReceiptIcon, ShoppingCartIcon, SignOutIcon } from '@phosphor-icons/react'
import { NavLink, Outlet } from 'react-router'
import { useLogout } from '../features/auth/hooks/useAuth'
import { useStoreSettings } from '../features/store/hooks/useStore'
import { useAuthStore } from '../stores/useAuthStore'

// Các màn hình chính trên thanh điều hướng (thêm báo cáo, danh mục... khi có).
const navItems = [
  { to: '/', label: 'Bán hàng', Icon: ShoppingCartIcon },
  { to: '/hoa-don', label: 'Hóa đơn', Icon: ReceiptIcon },
  { to: '/nhap-hang', label: 'Nhập hàng', Icon: PackageIcon },
]

// Khung chung mọi màn hình: thanh đầu trang có tên cửa hàng, người đang đăng nhập, nút đăng xuất,
// thanh chuyển giữa các màn hình chính.
export function AppLayout() {
  const store = useStoreSettings()
  const user = useAuthStore((state) => state.session?.user)
  const logout = useLogout()

  return (
    <div className="flex min-h-dvh flex-col">
      <header className="sticky top-0 z-10 bg-brand-600 pt-[env(safe-area-inset-top)] text-white shadow-sm">
        <div className="mx-auto flex h-14 max-w-5xl items-center gap-3 px-4">
          <img src="/logo-mark.svg" alt="" className="size-8 shrink-0" />
          <span className="min-w-0 flex-1 truncate text-lg font-semibold">{store.data?.name ?? 'FoMart'}</span>
          {user && (
            <span className="hidden max-w-40 truncate text-sm text-brand-50 sm:inline" title={user.name}>
              {user.name}
            </span>
          )}
          <button
            type="button"
            onClick={() => logout.mutate()}
            disabled={logout.isPending}
            aria-label={user ? `Đăng xuất ${user.name}` : 'Đăng xuất'}
            className="-mr-2 flex size-11 shrink-0 items-center justify-center rounded-lg text-white hover:bg-brand-700 disabled:opacity-60 focus-visible:outline-2 focus-visible:outline-offset-0 focus-visible:outline-white"
          >
            <SignOutIcon size="1.5rem" />
          </button>
        </div>
        <nav aria-label="Màn hình chính" className="mx-auto flex max-w-5xl gap-1 px-2">
          {navItems.map(({ to, label, Icon }) => (
            <NavLink
              key={to}
              to={to}
              end={to === '/'}
              className="flex min-h-11 items-center gap-1.5 border-b-4 border-transparent px-3 font-semibold text-brand-50 aria-[current=page]:border-accent aria-[current=page]:text-white focus-visible:outline-2 focus-visible:outline-white"
            >
              <Icon size="1.25em" weight="bold" aria-hidden="true" />
              {label}
            </NavLink>
          ))}
        </nav>
      </header>
      <main className="mx-auto w-full max-w-5xl flex-1 px-4 py-4 pb-[max(1rem,env(safe-area-inset-bottom))]">
        <Outlet />
      </main>
    </div>
  )
}
