import { CaretRightIcon, XIcon } from '@phosphor-icons/react'
import type { KnownUser } from '../../../stores/useAuthStore'

interface KnownUserListProps {
  users: KnownUser[]
  onPick: (user: KnownUser) => void
  onForget: (user: KnownUser) => void
}

const ROLE_LABEL: Record<KnownUser['role'], string> = { owner: 'Chủ tiệm', staff: 'Nhân viên' }

// Danh sách người đã đăng nhập trên máy này: chạm tên để đổi ca (UC-11).
export function KnownUserList({ users, onPick, onForget }: KnownUserListProps) {
  return (
    <ul className="flex flex-col overflow-hidden rounded-xl border border-line bg-surface">
      {users.map((user) => (
        <li key={user.id} className="flex items-center border-b border-line last:border-b-0">
          <button
            type="button"
            onClick={() => onPick(user)}
            className="flex min-h-16 min-w-0 flex-1 items-center gap-3 px-4 py-3 text-left active:bg-brand-50 focus-visible:outline-2 focus-visible:-outline-offset-2 focus-visible:outline-brand-500"
          >
            <span
              aria-hidden="true"
              className="flex size-11 shrink-0 items-center justify-center rounded-full bg-brand-100 text-lg font-bold text-brand-700"
            >
              {user.name.trim().charAt(0).toUpperCase()}
            </span>
            <span className="flex min-w-0 flex-1 flex-col">
              <span className="truncate text-lg font-semibold">{user.name}</span>
              <span className="text-sm text-ink-muted tabular-nums">
                {ROLE_LABEL[user.role]} · ••• {user.phone.slice(-4)}
              </span>
            </span>
            <CaretRightIcon size="1.25rem" className="shrink-0 text-ink-muted" aria-hidden="true" />
          </button>
          <button
            type="button"
            onClick={() => onForget(user)}
            aria-label={`Bỏ ${user.name} khỏi danh sách trên máy này`}
            className="flex size-11 shrink-0 items-center justify-center text-ink-muted hover:text-bad focus-visible:outline-2 focus-visible:-outline-offset-2 focus-visible:outline-brand-500"
          >
            <XIcon size="1.125rem" />
          </button>
        </li>
      ))}
    </ul>
  )
}
