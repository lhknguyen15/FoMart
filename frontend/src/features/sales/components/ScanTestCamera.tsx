import { BrowserMultiFormatReader, type IScannerControls } from '@zxing/browser'
import { BarcodeFormat, DecodeHintType } from '@zxing/library'
import { Html5Qrcode, Html5QrcodeSupportedFormats } from 'html5-qrcode'
import { useEffect, useRef, useState } from 'react'
import { getNativeDetector, ZxingWasm, type BarcodeDetectorConstructor } from '../../../lib/barcodeDetector'

// Thử nghiệm quét mã (ADR-0003): bốn engine, cùng một giao diện. Bỏ sau khi chốt thư viện.

export type ScanEngine = 'zxing' | 'html5' | 'native' | 'wasm'

// Khả năng điều khiển camera (chỉ có trên một số điện thoại / trình duyệt).
interface CameraControls {
  zoom?: { min: number; max: number }
  torch: boolean
}

// at = performance.now() lúc đọc được mã / lúc camera sẵn sàng, để trang đo thời gian.
interface ScanTestCameraProps {
  engine: ScanEngine
  onCode: (code: string, at: number) => void
  onReady: (at: number) => void
  onError: (message: string) => void
}

// Số liệu chẩn đoán do engine ghi, overlay đọc lại mỗi 500 ms.
interface ScanStats {
  attempts: number
  // Lỗi giải mã gần nhất khác "không thấy mã": ChecksumException/FormatException = thấy mã nhưng đọc sai.
  lastDecodeError: string | null
}

interface Diagnostics {
  resolution: string
  camera: string
  focus: string
  rate: number
  attempts: number
  lastDecodeError: string | null
}

// Mã hàng tạp hóa: EAN-13/8 (hàng Việt Nam 893...), UPC (hàng nhập Mỹ), Code 128 (mã nội bộ).
const ZXING_FORMATS = [BarcodeFormat.EAN_13, BarcodeFormat.EAN_8, BarcodeFormat.UPC_A, BarcodeFormat.UPC_E, BarcodeFormat.CODE_128]
const HTML5_FORMATS = [
  Html5QrcodeSupportedFormats.EAN_13,
  Html5QrcodeSupportedFormats.EAN_8,
  Html5QrcodeSupportedFormats.UPC_A,
  Html5QrcodeSupportedFormats.UPC_E,
  Html5QrcodeSupportedFormats.CODE_128,
]
const NATIVE_FORMATS = ['ean_13', 'ean_8', 'upc_a', 'upc_e', 'code_128']
const CAMERA: MediaStreamConstraints = {
  video: { facingMode: { ideal: 'environment' }, width: { ideal: 1920 }, height: { ideal: 1080 } },
  audio: false,
}
const HTML5_REGION_ID = 'scan-test-html5'

// Bật camera sau và quét liên tục bằng engine đã chọn; tắt camera khi component bị gỡ.
export function ScanTestCamera({ engine, onCode, onReady, onError }: ScanTestCameraProps) {
  const videoRef = useRef<HTMLVideoElement>(null)
  const regionRef = useRef<HTMLDivElement>(null)
  const stats = useRef<ScanStats>({ attempts: 0, lastDecodeError: null })
  const [diagnostics, setDiagnostics] = useState<Diagnostics | null>(null)
  const [controls, setControls] = useState<CameraControls | null>(null)
  const [zoom, setZoom] = useState(1)
  const [torchOn, setTorchOn] = useState(false)
  // Track video đang chạy (interval bên dưới cập nhật), để nút zoom / đèn điều khiển camera.
  const [track, setTrack] = useState<MediaStreamTrack | null>(null)
  // Giữ callback mới nhất mà không khởi động lại camera mỗi lần cha vẽ lại.
  const callbacks = useRef({ onCode, onReady, onError })
  useEffect(() => {
    callbacks.current = { onCode, onReady, onError }
  })

  useEffect(() => {
    const video = videoRef.current
    stats.current = { attempts: 0, lastDecodeError: null }
    if (engine === 'zxing' && video) return startZxing(video, callbacks, stats)
    if (engine === 'html5') return startHtml5(callbacks, stats)
    if (engine === 'native' && video) return startDetectorLoop(video, getNativeDetector(), callbacks, stats)
    if (engine === 'wasm' && video) return startDetectorLoop(video, ZxingWasm, callbacks, stats)
    return undefined
  }, [engine])

  const applyZoom = (value: number) => {
    setZoom(value)
    track?.applyConstraints({ advanced: [{ zoom: value } as unknown as MediaTrackConstraintSet] }).catch(() => undefined)
  }

  const toggleTorch = () => {
    const next = !torchOn
    setTorchOn(next)
    track?.applyConstraints({ advanced: [{ torch: next } as unknown as MediaTrackConstraintSet] }).catch(() => undefined)
  }

  // Đọc thông số camera và tốc độ quét mỗi 500 ms để hiện lên hình.
  useEffect(() => {
    let previousAttempts = 0
    const timer = window.setInterval(() => {
      const video = videoRef.current ?? regionRef.current?.querySelector('video') ?? null
      const track = video?.srcObject instanceof MediaStream ? video.srcObject.getVideoTracks()[0] : undefined
      setTrack(track ?? null)
      const settings = track?.getSettings() as (MediaTrackSettings & { focusMode?: string }) | undefined
      const capabilities = track?.getCapabilities?.() as
        | (MediaTrackCapabilities & { zoom?: { min: number; max: number }; torch?: boolean })
        | undefined
      if (capabilities) {
        setControls((previous) =>
          previous ?? { zoom: capabilities.zoom && capabilities.zoom.max > 1 ? capabilities.zoom : undefined, torch: capabilities.torch === true },
        )
      }
      const { attempts, lastDecodeError } = stats.current
      setDiagnostics({
        resolution: video && video.videoWidth ? `${video.videoWidth}×${video.videoHeight}` : 'chưa có hình',
        camera: track?.label || 'không rõ',
        focus: settings?.focusMode ?? 'không rõ',
        rate: (attempts - previousAttempts) * 2,
        attempts,
        lastDecodeError,
      })
      previousAttempts = attempts
    }, 500)
    return () => window.clearInterval(timer)
  }, [engine])

  return (
    <div className="flex flex-col gap-2">
      <div className="relative overflow-hidden rounded-xl bg-ink">
        {engine === 'html5' ? (
          <div id={HTML5_REGION_ID} ref={regionRef} className="aspect-4/3 w-full" />
        ) : (
          <video ref={videoRef} className="aspect-4/3 w-full object-cover" muted playsInline />
        )}
        {engine !== 'html5' && (
          <div className="pointer-events-none absolute inset-x-[12%] top-1/2 h-0.5 -translate-y-1/2 rounded-full bg-warn/90" />
        )}
      </div>
      {controls && (controls.zoom || controls.torch) && (
        <div className="flex flex-wrap gap-2">
          {controls.zoom &&
            [1, 2, 3]
              .filter((value) => value >= (controls.zoom?.min ?? 1) && value <= (controls.zoom?.max ?? 1))
              .map((value) => (
                <button key={value} type="button" onClick={() => applyZoom(value)} aria-pressed={zoom === value}
                  className={`min-h-11 min-w-14 rounded-lg border px-3 font-medium ${zoom === value ? 'border-brand-600 bg-brand-50 text-brand-700' : 'border-line bg-surface'}`}>
                  {value}×
                </button>
              ))}
          {controls.torch && (
            <button type="button" onClick={toggleTorch} aria-pressed={torchOn}
              className={`min-h-11 rounded-lg border px-4 font-medium ${torchOn ? 'border-warn bg-warn/15' : 'border-line bg-surface'}`}>
              {torchOn ? 'Tắt đèn' : 'Bật đèn'}
            </button>
          )}
        </div>
      )}
      {diagnostics && (
        <p className="rounded-lg bg-ink px-3 py-2 font-mono text-xs leading-relaxed text-white tabular-nums">
          Hình: {diagnostics.resolution} · lấy nét: {diagnostics.focus}
          <br />
          Camera: {diagnostics.camera}
          <br />
          Đã thử {diagnostics.attempts} khung (~{diagnostics.rate}/giây)
          <br />
          Lỗi giải mã gần nhất: {diagnostics.lastDecodeError ?? 'chưa có (chưa thấy mã nào)'}
        </p>
      )}
    </div>
  )
}

type Callbacks = React.RefObject<Pick<ScanTestCameraProps, 'onCode' | 'onReady' | 'onError'>>
type Stats = React.RefObject<ScanStats>

function describe(error: unknown): string {
  if (error instanceof DOMException && error.name === 'NotAllowedError') return 'Chưa cho phép dùng camera.'
  if (error instanceof DOMException && error.name === 'NotFoundError') return 'Không tìm thấy camera.'
  if (error instanceof Error) return error.message
  return String(error)
}

// Bật lấy nét liên tục nếu camera hỗ trợ (nhiều điện thoại mặc định lấy nét một lần, mã ở gần bị mờ).
function enableContinuousFocus(stream: MediaStream | null | undefined) {
  const track = stream?.getVideoTracks()[0]
  const capabilities = track?.getCapabilities?.() as (MediaTrackCapabilities & { focusMode?: string[] }) | undefined
  if (!track || !capabilities?.focusMode?.includes('continuous')) return
  track.applyConstraints({ advanced: [{ focusMode: 'continuous' } as unknown as MediaTrackConstraintSet] }).catch(() => undefined)
}

function startZxing(video: HTMLVideoElement, callbacks: Callbacks, stats: Stats): () => void {
  let controls: IScannerControls | undefined
  let cancelled = false
  const hints = new Map<DecodeHintType, unknown>([
    [DecodeHintType.POSSIBLE_FORMATS, ZXING_FORMATS],
    [DecodeHintType.TRY_HARDER, true],
  ])
  const reader = new BrowserMultiFormatReader(hints, { delayBetweenScanAttempts: 80 })
  reader
    .decodeFromConstraints(CAMERA, video, (result, error) => {
      stats.current.attempts++
      if (result) callbacks.current.onCode(result.getText(), performance.now())
      else if (error && error.name !== 'NotFoundException') stats.current.lastDecodeError = error.name
    })
    .then((started) => {
      if (cancelled) started.stop()
      else {
        controls = started
        enableContinuousFocus(video.srcObject as MediaStream | null)
        callbacks.current.onReady(performance.now())
      }
    })
    .catch((error: unknown) => callbacks.current.onError(describe(error)))
  return () => {
    cancelled = true
    controls?.stop()
  }
}

function startHtml5(callbacks: Callbacks, stats: Stats): () => void {
  const scanner = new Html5Qrcode(HTML5_REGION_ID, {
    formatsToSupport: HTML5_FORMATS,
    useBarCodeDetectorIfSupported: false, // đo đúng phần giải mã JavaScript của thư viện
    verbose: false,
  })
  let started = false
  let cancelled = false
  const stop = () => {
    scanner
      .stop()
      .then(() => scanner.clear())
      .catch(() => undefined)
  }
  scanner
    .start(
      { facingMode: 'environment' },
      { fps: 15, qrbox: { width: 280, height: 150 } },
      (text) => callbacks.current.onCode(text, performance.now()),
      (message) => {
        stats.current.attempts++
        if (!message.includes('NotFoundException') && !message.includes('No MultiFormat Readers')) {
          stats.current.lastDecodeError = message.slice(0, 80)
        }
      },
    )
    .then(() => {
      started = true
      if (cancelled) stop()
      else {
        const video = document.querySelector<HTMLVideoElement>(`#${HTML5_REGION_ID} video`)
        enableContinuousFocus(video?.srcObject as MediaStream | null)
        callbacks.current.onReady(performance.now())
      }
    })
    .catch((error: unknown) => callbacks.current.onError(describe(error)))
  return () => {
    cancelled = true
    if (started) stop()
  }
}

// Vòng quét dùng giao diện BarcodeDetector: bộ đọc có sẵn của trình duyệt hoặc zxing-cpp (WASM).
function startDetectorLoop(
  video: HTMLVideoElement,
  Detector: BarcodeDetectorConstructor | undefined,
  callbacks: Callbacks,
  stats: Stats,
): () => void {
  if (!Detector) {
    callbacks.current.onError('Trình duyệt này không có BarcodeDetector.')
    return () => undefined
  }
  const detector = new Detector({ formats: NATIVE_FORMATS })
  let stream: MediaStream | undefined
  let timer: number | undefined
  let cancelled = false

  const loop = async () => {
    if (cancelled) return
    try {
      if (video.readyState >= 2) {
        stats.current.attempts++
        const codes = await detector.detect(video)
        const first = codes[0]
        if (first) callbacks.current.onCode(first.rawValue, performance.now())
      }
    } catch (error) {
      stats.current.lastDecodeError = error instanceof Error ? error.name : String(error)
    }
    timer = window.setTimeout(loop, 80)
  }

  navigator.mediaDevices
    .getUserMedia(CAMERA)
    .then(async (media) => {
      if (cancelled) {
        media.getTracks().forEach((track) => track.stop())
        return
      }
      stream = media
      video.srcObject = media
      await video.play()
      enableContinuousFocus(media)
      callbacks.current.onReady(performance.now())
      void loop()
    })
    .catch((error: unknown) => callbacks.current.onError(describe(error)))

  return () => {
    cancelled = true
    window.clearTimeout(timer)
    stream?.getTracks().forEach((track) => track.stop())
  }
}
