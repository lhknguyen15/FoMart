import { ArrowLeftIcon, PhoneIcon } from '@phosphor-icons/react'
import { useState } from 'react'
import { Navigate, useLocation } from 'react-router'
import { ApiError } from '../../../api/types'
import { useAuthStore, type KnownUser, type LoginMethod } from '../../../stores/useAuthStore'
import { useStoreSettings } from '../../store/hooks/useStore'
import { KnownUserList } from '../components/KnownUserList'
import { PhoneLoginForm } from '../components/PhoneLoginForm'
import { PinPad } from '../components/PinPad'
import { useLogin, type LoginInput } from '../hooks/useAuth'

// Đăng nhập và đổi ca (UC-11, SC-01).
// Máy đã có người đăng nhập: chạm tên → PIN. Người mới / chủ tiệm: số điện thoại + mật khẩu hoặc PIN.

type View = { kind: 'users' } | { kind: 'known'; user: KnownUser; method: LoginMethod } | { kind: 'phone' }

const ROLE_LABEL: Record<KnownUser['role'], string> = { owner: 'Chủ tiệm', staff: 'Nhân viên' }

// Đổi khung nhìn: đưa tiêu điểm vào tiêu đề mới để trình đọc màn hình đọc ngay, không rơi về <body>.
const focusOnMount = (element: HTMLHeadingElement | null) => element?.focus({ preventScroll: true })
const headingClass = 'text-2xl font-bold outline-none'

export function LoginPage() {
  const session = useAuthStore((state) => state.session)
  const knownUsers = useAuthStore((state) => state.knownUsers)
  const forgetUser = useAuthStore((state) => state.forgetUser)
  const store = useStoreSettings()
  const location = useLocation()
  const from = (location.state as { from?: string } | null)?.from ?? '/'
  const login = useLogin()
  const [view, setView] = useState<View>(knownUsers.length > 0 ? { kind: 'users' } : { kind: 'phone' })
  // Tăng sau mỗi lần đăng nhập lỗi để bàn phím PIN làm trống lại.
  const [attempt, setAttempt] = useState(0)

  // Đã đăng nhập (vừa xong hoặc mở lại trang): về trang đang định vào.
  if (session) return <Navigate to={from} replace />

  const current: View = view.kind === 'users' && knownUsers.length === 0 ? { kind: 'phone' } : view

  const go = (next: View) => {
    login.reset()
    setView(next)
  }
  const submit = (input: LoginInput) => login.mutate(input, { onError: () => setAttempt((value) => value + 1) })
  const errorMessage = login.error instanceof ApiError ? login.error.message : login.error ? 'Có lỗi khi đăng nhập.' : null

  return (
    <main className="flex min-h-dvh justify-center bg-paper px-4 pt-[max(2.5rem,env(safe-area-inset-top))] pb-[max(2rem,env(safe-area-inset-bottom))]">
      <div className="flex w-full max-w-sm flex-col gap-8">
        <header className="flex items-center gap-3">
          <img src="/logo-mark.svg" alt="" className="size-12" />
          <div className="min-w-0">
            <p className="text-2xl leading-tight font-bold">FoMart</p>
            <p className="truncate text-sm text-ink-muted">{store.data?.name ?? 'Bán hàng và tồn kho'}</p>
          </div>
        </header>

        {current.kind === 'users' && (
          <section className="flex flex-col gap-4" aria-labelledby="login-title">
            <h1 id="login-title" tabIndex={-1} ref={focusOnMount} className={headingClass}>
              Ai đang bán?
            </h1>
            <KnownUserList
              users={knownUsers}
              onPick={(user) => go({ kind: 'known', user, method: user.method })}
              onForget={(user) => forgetUser(user.id)}
            />
            <button
              type="button"
              onClick={() => go({ kind: 'phone' })}
              className="flex h-12 items-center justify-center gap-2 rounded-xl border border-line bg-surface font-semibold text-ink active:bg-brand-50 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-brand-500"
            >
              <PhoneIcon size="1.25rem" aria-hidden="true" />
              Đăng nhập bằng số điện thoại
            </button>
          </section>
        )}

        {current.kind === 'known' && (
          <section className="flex flex-col gap-6" aria-labelledby="login-title">
            <div className="flex flex-col gap-3">
              <BackButton onClick={() => go({ kind: 'users' })}>Đổi người</BackButton>
              <div>
                <h1 id="login-title" tabIndex={-1} ref={focusOnMount} className={headingClass}>
                  {current.user.name}
                </h1>
                <p className="text-sm text-ink-muted tabular-nums">
                  {ROLE_LABEL[current.user.role]} · ••• {current.user.phone.slice(-4)}
                </p>
              </div>
            </div>

            {current.method === 'pin' ? (
              <>
                {errorMessage && <ErrorNote>{errorMessage}</ErrorNote>}
                <PinPad
                  key={attempt}
                  busy={login.isPending}
                  onSubmit={(pin) => submit({ phone: current.user.phone, secret: pin, method: 'pin' })}
                />
                <button
                  type="button"
                  onClick={() => go({ ...current, method: 'password' })}
                  className="min-h-11 self-center px-3 font-semibold text-brand-700 underline-offset-4 hover:underline focus-visible:outline-2 focus-visible:outline-brand-500"
                >
                  Dùng mật khẩu
                </button>
              </>
            ) : (
              <>
                <PhoneLoginForm
                  key={`password-${current.user.id}`}
                  fixedPhone={current.user.phone}
                  defaultMethod="password"
                  busy={login.isPending}
                  serverError={login.error}
                  onSubmit={(values) => submit(values)}
                />
                <button
                  type="button"
                  onClick={() => go({ ...current, method: 'pin' })}
                  className="min-h-11 self-center px-3 font-semibold text-brand-700 underline-offset-4 hover:underline focus-visible:outline-2 focus-visible:outline-brand-500"
                >
                  Dùng PIN
                </button>
              </>
            )}
          </section>
        )}

        {current.kind === 'phone' && (
          <section className="flex flex-col gap-6" aria-labelledby="login-title">
            <div className="flex flex-col gap-3">
              {knownUsers.length > 0 && <BackButton onClick={() => go({ kind: 'users' })}>Chọn người đã đăng nhập</BackButton>}
              <h1 id="login-title" tabIndex={-1} ref={focusOnMount} className={headingClass}>
                Đăng nhập
              </h1>
            </div>
            <PhoneLoginForm busy={login.isPending} serverError={login.error} onSubmit={(values) => submit(values)} />
          </section>
        )}
      </div>
    </main>
  )
}

function BackButton({ onClick, children }: { onClick: () => void; children: string }) {
  return (
    <button
      type="button"
      onClick={onClick}
      className="-ml-2 flex min-h-11 items-center gap-1.5 self-start rounded-lg px-2 text-sm font-semibold text-brand-700 focus-visible:outline-2 focus-visible:outline-brand-500"
    >
      <ArrowLeftIcon size="1.125rem" aria-hidden="true" />
      {children}
    </button>
  )
}

function ErrorNote({ children }: { children: string }) {
  return (
    <p role="alert" className="rounded-lg border border-bad/40 bg-bad/10 px-3 py-2 text-center text-sm font-semibold text-bad">
      {children}
    </p>
  )
}
