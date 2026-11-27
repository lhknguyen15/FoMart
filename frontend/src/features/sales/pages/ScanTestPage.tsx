import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useRef, useState, type ChangeEvent, type FormEvent } from 'react'
import { login, type AuthResponse } from '../../../api/auth'
import { getProductByBarcode } from '../../../api/catalog'
import { ApiError } from '../../../api/types'
import { formatMoney } from '../../../lib/format'
import { decodeImageFile, getNativeDetector } from '../../../lib/barcodeDetector'
import { ScanTestCamera, type ScanEngine } from '../components/ScanTestCamera'

// Trang thử quét mã (ADR-0003): so sánh tốc độ và độ ổn định của các thư viện trên điện thoại thật.
// Tạm thời, bỏ khi đã chốt thư viện.

const ENGINES: { id: ScanEngine; label: string }[] = [
  { id: 'zxing', label: 'ZXing' },
  { id: 'html5', label: 'html5-qrcode' },
  { id: 'native', label: 'Có sẵn (Chrome)' },
  { id: 'wasm', label: 'ZXing-C++ (WASM)' },
]
// Cùng một mã trong khoảng này coi là một lần quét (camera đọc liên tục).
const DUPLICATE_WINDOW_MS = 2000

interface ScanResult {
  id: number
  engine: ScanEngine | 'photo'
  code: string
  // Lần đọc đầu sau khi bật camera: thời gian từ lúc camera sẵn sàng. Các lần sau: thời gian từ lần đọc trước.
  elapsedMs: number
  isFirst: boolean
  lookup: 'pending' | 'found' | 'missing' | 'error' | 'skipped'
  detail?: string
}

export function ScanTestPage() {
  const queryClient = useQueryClient()
  const [session, setSession] = useState<AuthResponse | null>(null)
  const [engine, setEngine] = useState<ScanEngine>('zxing')
  const [running, setRunning] = useState(false)
  const [cameraError, setCameraError] = useState<string | null>(null)
  const [results, setResults] = useState<ScanResult[]>([])
  const [photoStatus, setPhotoStatus] = useState<string | null>(null)
  const readyAt = useRef<number | null>(null)
  const last = useRef<{ code: string; at: number } | null>(null)
  const nextId = useRef(1)
  const audio = useRef<AudioContext | null>(null)

  const signIn = useMutation({
    mutationFn: login,
    onSuccess: (data) => setSession(data),
  })

  const hasNative = getNativeDetector() !== undefined

  function start() {
    // AudioContext phải tạo trong thao tác bấm của người dùng thì điện thoại mới cho phát tiếng.
    audio.current ??= new AudioContext()
    readyAt.current = null
    last.current = null
    setCameraError(null)
    setRunning(true)
  }

  function beep() {
    navigator.vibrate?.(60)
    const ctx = audio.current
    if (!ctx) return
    const osc = ctx.createOscillator()
    const gain = ctx.createGain()
    osc.frequency.value = 1500
    gain.gain.value = 0.15
    osc.connect(gain).connect(ctx.destination)
    osc.start()
    osc.stop(ctx.currentTime + 0.08)
  }

  // Chụp ảnh bằng app camera gốc rồi đọc mã: tách lỗi "camera web mờ" khỏi lỗi "mã trên hàng khó đọc".
  function submitPhoto(event: ChangeEvent<HTMLInputElement>) {
    const input = event.currentTarget
    const file = input.files?.[0]
    input.value = ''
    if (!file) return
    setPhotoStatus('Đang đọc ảnh…')
    decodeImageFile(file)
      .then((result) => {
        const size = `ảnh ${result.width}×${result.height}, ${result.elapsedMs} ms`
        if (result.code) {
          setPhotoStatus(`Đọc được bằng ${result.reader === 'native' ? 'bộ đọc có sẵn' : 'ZXing-C++'} (${size}).`)
          handleCode(result.code, event.timeStamp, 'photo', result.elapsedMs)
        } else {
          setPhotoStatus(`Không tìm thấy mã trong ảnh (${size}). Thử chụp gần hơn, mã nằm ngang, không lóa.`)
        }
      })
      .catch((error: unknown) => setPhotoStatus(error instanceof Error ? error.message : String(error)))
  }

  function handleCode(code: string, now: number, source: ScanResult['engine'] = engine, measuredMs?: number) {
    const previous = last.current
    if (previous && previous.code === code && now - previous.at < DUPLICATE_WINDOW_MS) {
      last.current = { code, at: now }
      return
    }
    const isFirst = previous === null
    const elapsedMs = measuredMs ?? Math.round(now - (isFirst ? (readyAt.current ?? now) : previous.at))
    last.current = { code, at: now }
    beep()

    const id = nextId.current++
    const token = session?.accessToken
    const entry: ScanResult = { id, engine: source, code, elapsedMs, isFirst, lookup: token ? 'pending' : 'skipped' }
    setResults((list) => [entry, ...list].slice(0, 30))
    if (!token) return

    // Tra mã qua TanStack Query: quét lại cùng mã trong 30 giây thì lấy từ cache, không gọi API.
    queryClient
      .fetchQuery({
        queryKey: ['products', 'barcode', code],
        queryFn: ({ signal }) => getProductByBarcode(code, { accessToken: token, signal }),
      })
      .then((product) => update(id, { lookup: 'found', detail: `${product.productName} · ${product.unitName} · ${formatMoney(product.salePrice)}` }))
      .catch((error: unknown) =>
        update(id, error instanceof ApiError && error.status === 404
          ? { lookup: 'missing', detail: 'Mã chưa có trong danh mục' }
          : { lookup: 'error', detail: error instanceof Error ? error.message : String(error) }),
      )
  }

  function update(id: number, patch: Partial<ScanResult>) {
    setResults((list) => list.map((r) => (r.id === id ? { ...r, ...patch } : r)))
  }

  // Nhập mã tay: kiểm tra riêng phần tra mã (API) khi camera chưa đọc được.
  function submitManual(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const input = event.currentTarget.elements.namedItem('code')
    if (!(input instanceof HTMLInputElement) || !input.value.trim()) return
    handleCode(input.value.trim(), event.timeStamp)
    input.value = ''
  }

  function submitLogin(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const form = new FormData(event.currentTarget)
    signIn.mutate({ phone: String(form.get('phone') ?? ''), password: String(form.get('password') ?? '') })
  }

  const firstReads = results.filter((r) => r.isFirst && r.engine === engine)
  const repeatReads = results.filter((r) => !r.isFirst && r.engine === engine)
  const average = (list: ScanResult[]) =>
    list.length ? `${Math.round(list.reduce((sum, r) => sum + r.elapsedMs, 0) / list.length)} ms` : '—'

  return (
    <section className="flex flex-col gap-4">
      <h1 className="text-xl font-semibold">Thử quét mã vạch</h1>

      {session ? (
        <p className="text-sm text-ink-muted">Đã đăng nhập: {session.user.name}. Quét xong sẽ tra mã trong danh mục.</p>
      ) : (
        <form onSubmit={submitLogin} className="flex flex-col gap-2 rounded-xl border border-line bg-surface p-3">
          <p className="text-sm text-ink-muted">Đăng nhập để tra mã trong danh mục (không bắt buộc để đo tốc độ).</p>
          <div className="flex flex-wrap gap-2">
            <input name="phone" inputMode="tel" placeholder="Số điện thoại" defaultValue="0900000001" aria-label="Số điện thoại"
              className="min-h-11 min-w-0 flex-1 rounded-lg border border-line px-3" />
            <input name="password" type="password" placeholder="Mật khẩu" aria-label="Mật khẩu"
              className="min-h-11 min-w-0 flex-1 rounded-lg border border-line px-3" />
            <button type="submit" disabled={signIn.isPending}
              className="min-h-11 rounded-lg bg-brand-600 px-4 font-medium text-white disabled:opacity-60">
              {signIn.isPending ? 'Đang vào…' : 'Đăng nhập'}
            </button>
          </div>
          {signIn.isError && <p className="text-sm text-bad">{signIn.error.message}</p>}
        </form>
      )}

      <div role="radiogroup" aria-label="Thư viện quét" className="grid grid-cols-2 gap-1 rounded-xl bg-line p-1">
        {ENGINES.map((item) => {
          const disabled = item.id === 'native' && !hasNative
          return (
            <button key={item.id} type="button" role="radio" aria-checked={engine === item.id} disabled={disabled || running}
              onClick={() => setEngine(item.id)}
              className={`min-h-11 rounded-lg text-sm font-medium disabled:opacity-40 ${engine === item.id ? 'bg-surface shadow-sm' : 'text-ink-muted'}`}>
              {item.label}
            </button>
          )
        })}
      </div>

      {running ? (
        <ScanTestCamera key={engine} engine={engine} onCode={handleCode}
          onReady={(at) => { readyAt.current = at }}
          onError={(message) => { setCameraError(message); setRunning(false) }} />
      ) : (
        <div className="flex aspect-4/3 w-full items-center justify-center rounded-xl border border-dashed border-line bg-surface text-ink-muted">
          Camera đang tắt
        </div>
      )}
      {cameraError && <p className="text-sm text-bad">{cameraError}</p>}
      <p className="text-sm text-ink-muted">
        Cầm cách mã 10–20 cm, mã nằm ngang theo vạch vàng, tránh lóa đèn trên chai nhựa. Chai tròn: xoay sao cho vạch mã thẳng, không cong.
      </p>

      <label className="flex min-h-12 cursor-pointer items-center justify-center rounded-xl border border-brand-600 bg-brand-50 font-medium text-brand-700">
        Chụp ảnh mã để đọc
        <input type="file" accept="image/*" capture="environment" onChange={submitPhoto} className="sr-only" />
      </label>
      {photoStatus && <p className="text-sm text-ink-muted">{photoStatus}</p>}

      <form onSubmit={submitManual} className="flex gap-2">
        <input name="code" inputMode="numeric" placeholder="Nhập mã tay (thử tra mã)" aria-label="Nhập mã vạch tay"
          className="min-h-11 min-w-0 flex-1 rounded-lg border border-line bg-surface px-3 font-mono" />
        <button type="submit" className="min-h-11 rounded-lg border border-line bg-surface px-4 font-medium">Tra</button>
      </form>

      <button type="button" onClick={running ? () => setRunning(false) : start}
        className={`min-h-14 rounded-xl text-lg font-semibold text-white ${running ? 'bg-ink' : 'bg-brand-600'}`}>
        {running ? 'Dừng quét' : 'Bật camera và quét'}
      </button>

      <dl className="grid grid-cols-3 gap-2 text-center">
        <div className="rounded-xl border border-line bg-surface p-2">
          <dt className="text-xs text-ink-muted">Lần đầu (TB)</dt>
          <dd className="font-semibold tabular-nums">{average(firstReads)}</dd>
        </div>
        <div className="rounded-xl border border-line bg-surface p-2">
          <dt className="text-xs text-ink-muted">Giữa 2 mã (TB)</dt>
          <dd className="font-semibold tabular-nums">{average(repeatReads)}</dd>
        </div>
        <div className="rounded-xl border border-line bg-surface p-2">
          <dt className="text-xs text-ink-muted">Số lần đọc</dt>
          <dd className="font-semibold tabular-nums">{firstReads.length + repeatReads.length}</dd>
        </div>
      </dl>

      <ol className="flex flex-col gap-2">
        {results.map((r) => (
          <li key={r.id} className="rounded-xl border border-line bg-surface p-3">
            <div className="flex items-baseline justify-between gap-2">
              <span className="font-mono font-semibold tabular-nums">{r.code}</span>
              <span className="text-xs text-ink-muted tabular-nums">
                {ENGINES.find((e) => e.id === r.engine)?.label ?? 'Ảnh chụp'} · {r.engine === 'photo' ? 'đọc ảnh ' : r.isFirst ? 'lần đầu ' : ''}{r.elapsedMs} ms
              </span>
            </div>
            <p className={`text-sm ${r.lookup === 'found' ? 'text-ok' : r.lookup === 'missing' ? 'text-warn' : r.lookup === 'error' ? 'text-bad' : 'text-ink-muted'}`}>
              {r.lookup === 'pending' ? 'Đang tra…' : r.lookup === 'skipped' ? 'Chưa đăng nhập: không tra mã' : r.detail}
            </p>
          </li>
        ))}
      </ol>

      <p className="text-xs text-ink-muted">
        Thiết bị: {window.isSecureContext ? 'HTTPS' : 'KHÔNG phải HTTPS (camera sẽ bị chặn)'} · BarcodeDetector: {hasNative ? 'có' : 'không'}
      </p>
    </section>
  )
}
