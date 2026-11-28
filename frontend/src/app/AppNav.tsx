import { CalendarXIcon, PackageIcon, TrashIcon, ReceiptIcon, ShoppingCartIcon, WarehouseIcon, type Icon } from '@phosphor-icons/react'
import { NavLink } from 'react-router'
import type { UserRole } from '../api/auth'
import { useAuthStore } from '../stores/useAuthStore'

interface NavItem {
  to: string
  label: string
  Icon: Icon
  ownerOnly?: boolean // Tổng quan, Báo cáo, Cài đặt (giai đoạn 4): nhân viên không thấy
  sidebarOnly?: boolean // chỉ ở menu bên; trên điện thoại vào từ trang khác để hàng tab không tràn (DESIGN.md > Navigation)
}

// Các màn hình chính. Thêm Sản phẩm, Báo cáo... ở đây khi có.
const items: NavItem[] = [
  { to: '/', label: 'Bán hàng', Icon: ShoppingCartIcon },
  { to: '/hoa-don', label: 'Hóa đơn', Icon: ReceiptIcon },
  { to: '/nhap-hang', label: 'Nhập hàng', Icon: PackageIcon },
  { to: '/ton-kho', label: 'Tồn kho', Icon: WarehouseIcon },
  { to: '/han-dung', label: 'Hạn dùng', Icon: CalendarXIcon, sidebarOnly: true },
  { to: '/xuat-huy', label: 'Xuất hủy', Icon: TrashIcon, sidebarOnly: true },
]

const visibleFor = (role: UserRole | undefined) => items.filter((item) => !item.ownerOnly || role === 'owner')

// Menu màn hình chính (DESIGN.md > Navigation): "tabs" là hàng tab dưới thanh đầu trang (< 1024px),
// "sidebar" là danh sách dọc trong menu bên (≥ 1024px). Mục đang mở có một điểm vàng mái hiên.
export function AppNav({ variant }: { variant: 'tabs' | 'sidebar' }) {
  const role = useAuthStore((state) => state.session?.user.role)
  const visible = visibleFor(role)

  if (variant === 'tabs') {
    return (
      <nav aria-label="Màn hình chính" className="mx-auto flex max-w-5xl gap-1 overflow-x-auto px-2 scrollbar-none">
        {visible.filter((item) => !item.sidebarOnly).map(({ to, label, Icon }) => (
          <NavLink
            key={to}
            to={to}
            end={to === '/'}
            className="flex min-h-11 shrink-0 items-center gap-1.5 border-b-4 border-transparent px-2.5 font-semibold sm:px-3 text-brand-50 aria-[current=page]:border-accent aria-[current=page]:text-white focus-visible:outline-2 focus-visible:outline-white"
          >
            <Icon size="1.25em" weight="bold" aria-hidden="true" className="hidden sm:block" />
            {label}
          </NavLink>
        ))}
      </nav>
    )
  }

  return (
    // 1024-1279px: thanh icon (tên hiện khi rê chuột, trình đọc màn hình vẫn đọc tên). Từ 1280px: icon + tên.
    <nav aria-label="Màn hình chính" className="flex flex-col gap-1 px-3">
      {visible.map(({ to, label, Icon }) => (
        <NavLink
          key={to}
          to={to}
          end={to === '/'}
          title={label}
          className="group flex min-h-12 items-center justify-center gap-3 rounded-lg px-3 font-semibold text-brand-50 hover:bg-brand-700/60 aria-[current=page]:bg-brand-700 aria-[current=page]:text-white focus-visible:outline-2 focus-visible:outline-white xl:justify-start"
        >
          <Icon size="1.375rem" weight="bold" aria-hidden="true" />
          <span className="sr-only xl:not-sr-only xl:flex-1">{label}</span>
          <span aria-hidden="true" className="hidden size-2 rounded-full bg-accent opacity-0 group-aria-[current=page]:opacity-100 xl:block" />
        </NavLink>
      ))}
    </nav>
  )
}
