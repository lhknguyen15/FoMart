import { BackspaceIcon } from '@phosphor-icons/react'
import { useEffect, useEffectEvent, useRef, useState } from 'react'

interface PinPadProps {
  // Gọi khi người dùng bấm "Vào" (hoặc Enter) với 4–6 số.
  onSubmit: (pin: string) => void
  busy?: boolean
}

const MIN_LENGTH = 4
const MAX_LENGTH = 6
const KEYS = ['1', '2', '3', '4', '5', '6', '7', '8', '9'] as const

const keyClass =
  'flex h-16 items-center justify-center rounded-xl bg-surface text-2xl font-semibold tabular-nums text-ink ring-1 ring-line ' +
  'transition-transform active:scale-[0.97] active:bg-brand-50 disabled:opacity-60 ' +
  'focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-brand-500'

// Bàn phím số cho PIN đổi ca: phím to bấm bằng ngón cái, hỗ trợ cả bàn phím máy tính.
// Nhập sai thì component cha gắn key mới để làm trống lại.
export function PinPad({ onSubmit, busy = false }: PinPadProps) {
  const [pin, setPinState] = useState('')
  // Bản sao để handler đọc giá trị mới nhất mà không gọi onSubmit bên trong hàm cập nhật state.
  const pinRef = useRef('')
  const setPin = (next: string) => {
    pinRef.current = next
    setPinState(next)
  }

  const press = (digit: string) => {
    if (!busy && pinRef.current.length < MAX_LENGTH) setPin(pinRef.current + digit)
  }
  const erase = () => {
    if (!busy) setPin(pinRef.current.slice(0, -1))
  }
  const submit = () => {
    if (!busy && pinRef.current.length >= MIN_LENGTH) onSubmit(pinRef.current)
  }

  // Bàn phím máy tính: số, Backspace, Enter. useEffectEvent luôn thấy busy / onSubmit mới nhất.
  const onKey = useEffectEvent((event: KeyboardEvent) => {
    // Enter/Backspace trên một nút đang được chọn: để nút tự xử lý, tránh gửi PIN hai lần.
    if (event.target instanceof HTMLButtonElement && (event.key === 'Enter' || event.key === 'Backspace')) return
    if (/^[0-9]$/.test(event.key)) press(event.key)
    else if (event.key === 'Backspace') erase()
    else if (event.key === 'Enter') submit()
  })
  useEffect(() => {
    window.addEventListener('keydown', onKey)
    return () => window.removeEventListener('keydown', onKey)
  }, [])

  return (
    <div className="flex flex-col gap-6">
      <div className="flex flex-col items-center gap-2">
        <div className="flex gap-3" aria-hidden="true">
          {Array.from({ length: MAX_LENGTH }, (_, index) => (
            <span
              key={index}
              className={`size-3.5 rounded-full transition-colors ${
                index < pin.length ? 'bg-brand-600' : index < MIN_LENGTH ? 'bg-line' : 'border border-line'
              }`}
            />
          ))}
        </div>
        <p className="text-sm text-ink-muted" aria-live="polite">
          {pin.length === 0 ? 'Nhập PIN 4–6 số' : `Đã nhập ${pin.length} số`}
        </p>
      </div>

      <div className="grid grid-cols-3 gap-3">
        {KEYS.map((digit) => (
          <button key={digit} type="button" className={keyClass} onClick={() => press(digit)} disabled={busy}>
            {digit}
          </button>
        ))}
        <button type="button" className={keyClass} onClick={erase} disabled={busy || pin.length === 0} aria-label="Xóa số cuối">
          <BackspaceIcon size="1.75rem" />
        </button>
        <button type="button" className={keyClass} onClick={() => press('0')} disabled={busy}>
          0
        </button>
        <button
          type="button"
          onClick={submit}
          disabled={busy || pin.length < MIN_LENGTH}
          className="flex h-16 items-center justify-center rounded-xl bg-brand-600 text-lg font-semibold text-white transition-transform active:scale-[0.97] active:bg-brand-700 disabled:bg-brand-100 disabled:text-brand-700 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-brand-500"
        >
          {busy ? 'Đang vào…' : 'Vào'}
        </button>
      </div>
    </div>
  )
}
