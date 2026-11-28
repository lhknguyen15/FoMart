import { useQueryClient } from '@tanstack/react-query'
import { useRef, useState, type ChangeEvent, type FormEvent } from 'react'
import { getProductByBarcode, type ProductBarcodeResponse } from '../../../api/catalog'
import { ApiError } from '../../../api/types'
import { decodeImageFile, getNativeDetector } from '../../../lib/barcodeDetector'
import { formatMoney } from '../../../lib/format'
import { measure } from '../../../lib/timing'
import { ScanTestCamera, type ScanEngine } from '../components/ScanTestCamera'

// Trang thử quét mã (ADR-0003): đo bộ đọc zxing-cpp đã chọn và bộ đọc có sẵn trên điện thoại thật.
// Tạm thời, bỏ khi màn hình bán hàng có component quét chính thức.

const ENGINES: { id: ScanEngine; label: string }[] = [
  { id: 'wasm', label: 'ZXing-C++ (đã chọn)' },
  { id: 'native', label: 'Có sẵn (Chrome)' },
]
// Cùng một mã trong khoảng này coi là một lần quét (camera đọc liên tục).
const DUPLICATE_WINDOW_MS = 2000

interface ScanResult {
  id: number
  engine: ScanEngine | 'photo' | 'manual'
  code: string
  // Thời gian quét: lần đầu tính từ lúc camera sẵn sàng, các lần sau tính từ lần đọc trước; ảnh chụp là thời gian giải mã ảnh.
  scanMs: number | null
  isFirst: boolean
  lookup: 'pending' | 'found' | 'missing' | 'error' | 'skipped'
  // Thời gian tra mã qua API; 0 = lấy từ bộ nhớ đệm của TanStack Query.
  lookupMs?: number
  fromCache?: boolean
  product?: ProductBarcodeResponse
  detail?: string
}

function sourceLabel(engine: ScanResult['engine']): string {
  if (engine === 'photo') return 'Ảnh chụp'
  if (engine === 'manual') return 'Nhập tay'
  return ENGINES.find((e) => e.id === engine)?.label ?? engine
}

export function ScanTestPage() {
  const queryClient = useQueryClient()
  const [engine, setEngine] = useState<ScanEngine>('wasm')
  const [running, setRunning] = useState(false)
  const [cameraError, setCameraError] = useState<string | null>(null)
  const [results, setResults] = useState<ScanResult[]>([])
  const [photoStatus, setPhotoStatus] = useState<string | null>(null)
  // Tăng mỗi lần đọc được mã để vẽ lại viền chớp quanh camera.
  const [flashKey, setFlashKey] = useState(0)
  const readyAt = useRef<number | null>(null)
  const last = useRef<{ code: string; at: number } | null>(null)
  const nextId = useRef(1)
  const audio = useRef<AudioContext | null>(null)

  const hasNative = getNativeDetector() !== undefined
  const latest = results[0]

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

  // Một mã vừa đọc được (camera, ảnh chụp hoặc nhập tay): báo ngay, rồi tra mã.
  function handleCode(code: string, now: number, source: ScanResult['engine'] = engine, measuredMs?: number) {
    const previous = last.current
    if (source !== 'manual' && previous && previous.code === code && now - previous.at < DUPLICATE_WINDOW_MS) {
      last.current = { code, at: now }
      return
    }
    const isFirst = previous === null
    const scanMs = source === 'manual' ? null : (measuredMs ?? Math.round(now - (isFirst ? (readyAt.current ?? now) : previous.at)))
    last.current = { code, at: now }
    beep()
    setFlashKey((key) => key + 1)

    const id = nextId.current++
    const entry: ScanResult = { id, engine: source, code, scanMs, isFirst, lookup: 'pending' }
    setResults((list) => [entry, ...list].slice(0, 30))

    // Tra mã qua TanStack Query: quét lại cùng mã trong 30 giây thì lấy từ bộ nhớ đệm, không gọi API.
    const queryKey = ['products', 'barcode', code]
    const fromCache = queryClient.getQueryState(queryKey)?.status === 'success' && !queryClient.getQueryState(queryKey)?.isInvalidated
    void measure(() =>
      queryClient.fetchQuery({
        queryKey,
        queryFn: ({ signal }) => getProductByBarcode(code, signal),
      }),
    ).then((outcome) => {
      if (outcome.ok) {
        update(id, { lookup: 'found', product: outcome.value, lookupMs: outcome.ms, fromCache })
      } else if (outcome.error instanceof ApiError && outcome.error.status === 404) {
        update(id, { lookup: 'missing', detail: 'Mã chưa có trong danh mục', lookupMs: outcome.ms })
      } else {
        update(id, { lookup: 'error', detail: outcome.error instanceof Error ? outcome.error.message : String(outcome.error), lookupMs: outcome.ms })
      }
    })
  }

  function update(id: number, patch: Partial<ScanResult>) {
    setResults((list) => list.map((r) => (r.id === id ? { ...r, ...patch } : r)))
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

  // Nhập mã tay: kiểm tra riêng phần tra mã (API) khi camera chưa đọc được.
  function submitManual(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const input = event.currentTarget.elements.namedItem('code')
    if (!(input instanceof HTMLInputElement) || !input.value.trim()) return
    handleCode(input.value.trim(), event.timeStamp, 'manual')
    input.value = ''
  }

  const cameraReads = results.filter((r) => r.engine === engine)
  const average = (values: number[]) => (values.length ? `${Math.round(values.reduce((a, b) => a + b, 0) / values.length)} ms` : '—')
  const firstAverage = average(cameraReads.filter((r) => r.isFirst && r.scanMs !== null).map((r) => r.scanMs ?? 0))
  const repeatAverage = average(cameraReads.filter((r) => !r.isFirst && r.scanMs !== null).map((r) => r.scanMs ?? 0))
  const apiAverage = average(results.filter((r) => r.lookupMs !== undefined && !r.fromCache).map((r) => r.lookupMs ?? 0))

  return (
    <section className="flex flex-col gap-4">
      <h1 className="text-xl font-semibold">Thử quét mã vạch</h1>

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

      <div className="relative">
        {running ? (
          <ScanTestCamera key={engine} engine={engine} onCode={handleCode}
            onReady={(at) => { readyAt.current = at }}
            onError={(message) => { setCameraError(message); setRunning(false) }} />
        ) : (
          <div className="flex aspect-4/3 w-full items-center justify-center rounded-xl border border-dashed border-line bg-surface text-ink-muted">
            Camera đang tắt
          </div>
        )}
        {flashKey > 0 && (
          <div key={flashKey} aria-hidden="true" className="pointer-events-none absolute inset-x-0 top-0 aspect-4/3 animate-scan-flash rounded-xl ring-8 ring-ok ring-inset" />
        )}
      </div>
      {cameraError && <p className="text-sm text-bad">{cameraError}</p>}

      <button type="button" onClick={running ? () => setRunning(false) : start}
        className={`min-h-14 rounded-xl text-lg font-semibold text-white ${running ? 'bg-ink' : 'bg-brand-600'}`}>
        {running ? 'Dừng quét' : 'Bật camera và quét'}
      </button>

      <div aria-live="polite" className={`rounded-xl border-2 p-4 ${
        !latest ? 'border-dashed border-line bg-surface'
        : latest.lookup === 'found' ? 'border-ok bg-ok/10'
        : latest.lookup === 'missing' ? 'border-warn bg-warn/10'
        : latest.lookup === 'error' ? 'border-bad bg-bad/10'
        : 'border-line bg-surface'}`}>
        {!latest ? (
          <p className="text-ink-muted">Chưa quét được mã nào. Kết quả mới nhất sẽ hiện ở đây.</p>
        ) : (
          <div className="flex flex-col gap-1">
            <p className="font-mono text-2xl font-bold tabular-nums">{latest.code}</p>
            {latest.product ? (
              <>
                <p className="text-lg font-semibold">{latest.product.productName}</p>
                <p className="text-base">{latest.product.unitName} · <span className="font-semibold tabular-nums">{formatMoney(latest.product.salePrice)}</span></p>
              </>
            ) : (
              <p className="text-base">
                {latest.lookup === 'pending' ? 'Đang tra mã…' : latest.lookup === 'skipped' ? 'Chưa đăng nhập: không tra mã' : latest.detail}
              </p>
            )}
            <p className="text-sm text-ink-muted tabular-nums">
              {sourceLabel(latest.engine)}
              {latest.scanMs !== null && ` · quét ${latest.scanMs} ms`}
              {latest.lookupMs !== undefined && (latest.fromCache ? ' · tra mã: bộ nhớ đệm' : ` · tra mã ${latest.lookupMs} ms`)}
            </p>
          </div>
        )}
      </div>

      <dl className="grid grid-cols-3 gap-2 text-center">
        <div className="rounded-xl border border-line bg-surface p-2">
          <dt className="text-xs text-ink-muted">Quét lần đầu (TB)</dt>
          <dd className="font-semibold tabular-nums">{firstAverage}</dd>
        </div>
        <div className="rounded-xl border border-line bg-surface p-2">
          <dt className="text-xs text-ink-muted">Giữa 2 mã (TB)</dt>
          <dd className="font-semibold tabular-nums">{repeatAverage}</dd>
        </div>
        <div className="rounded-xl border border-line bg-surface p-2">
          <dt className="text-xs text-ink-muted">Tra mã API (TB)</dt>
          <dd className="font-semibold tabular-nums">{apiAverage}</dd>
        </div>
      </dl>

      <p className="text-sm text-ink-muted">
        Cầm cách mã 20–25 cm, bật zoom 2× nếu có, mã nằm ngang theo vạch vàng, tránh lóa đèn trên chai nhựa.
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

      {results.length > 1 && <h2 className="text-base font-semibold">Lịch sử quét</h2>}
      <ol className="flex flex-col gap-2">
        {results.slice(1).map((r) => (
          <li key={r.id} className="rounded-xl border border-line bg-surface p-3">
            <div className="flex items-baseline justify-between gap-2">
              <span className="font-mono font-semibold tabular-nums">{r.code}</span>
              <span className="text-xs text-ink-muted tabular-nums">
                {sourceLabel(r.engine)}
                {r.scanMs !== null && ` · quét ${r.scanMs} ms`}
                {r.lookupMs !== undefined && (r.fromCache ? ' · đệm' : ` · API ${r.lookupMs} ms`)}
              </span>
            </div>
            <p className={`text-sm ${r.lookup === 'found' ? 'text-ok' : r.lookup === 'missing' ? 'text-warn' : r.lookup === 'error' ? 'text-bad' : 'text-ink-muted'}`}>
              {r.product
                ? `${r.product.productName} · ${r.product.unitName} · ${formatMoney(r.product.salePrice)}`
                : r.lookup === 'pending' ? 'Đang tra…' : r.lookup === 'skipped' ? 'Chưa đăng nhập: không tra mã' : r.detail}
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
