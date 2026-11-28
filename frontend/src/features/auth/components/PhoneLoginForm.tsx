import { zodResolver } from '@hookform/resolvers/zod'
import { useForm, useWatch } from 'react-hook-form'
import { ApiError } from '../../../api/types'
import type { LoginMethod } from '../../../stores/useAuthStore'
import { phoneLoginSchema, type PhoneLoginValues } from '../schemas/loginSchema'

interface PhoneLoginFormProps {
  onSubmit: (values: PhoneLoginValues) => void
  busy: boolean
  // Lỗi từ server (sai mật khẩu, khóa tài khoản, quá nhiều lần thử...): hiện chung dưới form.
  serverError: unknown
  // Người đã biết trên máy: số điện thoại cố định, ẩn lựa chọn Mật khẩu/PIN (PIN dùng bàn phím số riêng).
  fixedPhone?: string
  defaultMethod?: LoginMethod
}

const METHODS: { value: LoginMethod; label: string }[] = [
  { value: 'password', label: 'Mật khẩu' },
  { value: 'pin', label: 'PIN' },
]

const inputClass =
  'h-12 w-full rounded-lg border bg-surface px-3 text-base text-ink placeholder:text-ink-muted/70 ' +
  'focus:border-brand-600 focus:outline-2 focus:outline-offset-0 focus:outline-brand-500/40 aria-invalid:border-bad'

// Đăng nhập bằng số điện thoại + mật khẩu hoặc PIN. React Hook Form giữ giá trị form, Zod kiểm tra.
export function PhoneLoginForm({ onSubmit, busy, serverError, fixedPhone, defaultMethod = 'password' }: PhoneLoginFormProps) {
  const {
    register,
    handleSubmit,
    control,
    formState: { errors },
  } = useForm<PhoneLoginValues>({
    resolver: zodResolver(phoneLoginSchema),
    defaultValues: { method: defaultMethod, phone: fixedPhone ?? '', secret: '' },
  })
  // useWatch (không dùng watch()) để React Compiler vẽ lại đúng khi đổi Mật khẩu ↔ PIN.
  const method = useWatch({ control, name: 'method' })

  // Lỗi 400 của server theo trường thì đã được chặn trước bởi Zod; còn lại là lỗi chung (401, 403, 429, mạng).
  const serverMessage = serverError instanceof ApiError ? serverError.message : serverError ? 'Có lỗi khi đăng nhập.' : null

  return (
    <form onSubmit={handleSubmit(onSubmit)} noValidate className="flex flex-col gap-5">
      {fixedPhone ? (
        <input type="hidden" {...register('phone')} />
      ) : (
        <div className="flex flex-col gap-1.5">
          <label htmlFor="login-phone" className="text-sm font-semibold">
            Số điện thoại
          </label>
          <input
            id="login-phone"
            type="tel"
            inputMode="tel"
            autoComplete="username"
            aria-invalid={errors.phone ? true : undefined}
            aria-describedby={errors.phone ? 'login-phone-error' : undefined}
            className={`${inputClass} tabular-nums ${errors.phone ? 'border-bad' : 'border-line'}`}
            {...register('phone')}
          />
          {errors.phone && (
            <p id="login-phone-error" className="text-sm font-semibold text-bad">
              {errors.phone.message}
            </p>
          )}
        </div>
      )}

      {fixedPhone ? (
        <input type="hidden" {...register('method')} />
      ) : (
        <fieldset className="flex flex-col gap-1.5">
          <legend className="mb-1.5 text-sm font-semibold">Đăng nhập bằng</legend>
          <div className="grid grid-cols-2 gap-1 rounded-xl bg-line/70 p-1">
            {METHODS.map((item) => (
              <label
                key={item.value}
                className="flex min-h-11 cursor-pointer items-center justify-center rounded-lg text-sm font-semibold text-ink-muted has-checked:bg-surface has-checked:text-ink has-focus-visible:outline-2 has-focus-visible:outline-brand-500"
              >
                <input type="radio" value={item.value} className="sr-only" {...register('method')} />
                {item.label}
              </label>
            ))}
          </div>
        </fieldset>
      )}

      <div className="flex flex-col gap-1.5">
        <label htmlFor="login-secret" className="text-sm font-semibold">
          {method === 'pin' ? 'PIN (4–6 số)' : 'Mật khẩu'}
        </label>
        <input
          id="login-secret"
          type="password"
          inputMode={method === 'pin' ? 'numeric' : 'text'}
          autoComplete={method === 'pin' ? 'off' : 'current-password'}
          aria-invalid={errors.secret ? true : undefined}
          aria-describedby={errors.secret ? 'login-secret-error' : undefined}
          className={`${inputClass} ${errors.secret ? 'border-bad' : 'border-line'}`}
          {...register('secret')}
        />
        {errors.secret && (
          <p id="login-secret-error" className="text-sm font-semibold text-bad">
            {errors.secret.message}
          </p>
        )}
      </div>

      {serverMessage && (
        <p role="alert" className="rounded-lg border border-bad/40 bg-bad/10 px-3 py-2 text-sm font-semibold text-bad">
          {serverMessage}
        </p>
      )}

      <button
        type="submit"
        disabled={busy}
        className="h-14 rounded-xl bg-brand-600 text-lg font-semibold text-white transition-transform active:scale-[0.98] active:bg-brand-700 disabled:opacity-60 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-brand-500"
      >
        {busy ? 'Đang đăng nhập…' : 'Đăng nhập'}
      </button>
    </form>
  )
}
