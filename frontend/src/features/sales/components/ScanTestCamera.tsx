import { useEffect, useRef, useState } from 'react'
import { getNativeDetector, ZxingWasm, type BarcodeDetectorConstructor } from '../../../lib/barcodeDetector'

// Trang thử quét mã (ADR-0003): zxing-cpp (WASM, bộ đọc đã chọn) và bộ đọc có sẵn của trình duyệt để đo tiếp trên các máy khác.
// Bỏ khi màn hình bán hàng có component quét chính thức.

export type ScanEngine = 'wasm' | 'native'

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

// Số liệu chẩn đoán do vòng quét ghi, overlay đọc lại mỗi 500 ms.
interface ScanStats {
  attempts: number
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
const FORMATS = ['ean_13', 'ean_8', 'upc_a', 'upc_e', 'code_128']
const CAMERA: MediaStreamConstraints = {
  video: { facingMode: { ideal: 'environment' }, width: { ideal: 1920 }, height: { ideal: 1080 } },
  audio: false,
}

// Bật camera sau và quét liên tục bằng bộ đọc đã chọn; tắt camera khi component bị gỡ.
export function ScanTestCamera({ engine, onCode, onReady, onError }: ScanTestCameraProps) {
  const videoRef = useRef<HTMLVideoElement>(null)
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
    if (!video) return undefined
    stats.current = { attempts: 0, lastDecodeError: null }
    return startDetectorLoop(video, engine === 'native' ? getNativeDetector() : ZxingWasm, callbacks, stats)
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
      const video = videoRef.current
      const current = video?.srcObject instanceof MediaStream ? video.srcObject.getVideoTracks()[0] : undefined
      setTrack(current ?? null)
      const settings = current?.getSettings() as (MediaTrackSettings & { focusMode?: string }) | undefined
      const capabilities = current?.getCapabilities?.() as
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
        camera: current?.label || 'không rõ',
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
        <video ref={videoRef} className="aspect-4/3 w-full object-cover" muted playsInline />
        <div className="pointer-events-none absolute inset-x-[12%] top-1/2 h-0.5 -translate-y-1/2 rounded-full bg-warn/90" />
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
          Lỗi gần nhất: {diagnostics.lastDecodeError ?? 'không có'}
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
function enableContinuousFocus(stream: MediaStream) {
  const current = stream.getVideoTracks()[0]
  const capabilities = current?.getCapabilities?.() as (MediaTrackCapabilities & { focusMode?: string[] }) | undefined
  if (!current || !capabilities?.focusMode?.includes('continuous')) return
  current.applyConstraints({ advanced: [{ focusMode: 'continuous' } as unknown as MediaTrackConstraintSet] }).catch(() => undefined)
}

// Vòng quét dùng giao diện BarcodeDetector: zxing-cpp (WASM) hoặc bộ đọc có sẵn của trình duyệt.
function startDetectorLoop(
  video: HTMLVideoElement,
  Detector: BarcodeDetectorConstructor | undefined,
  callbacks: Callbacks,
  stats: Stats,
): () => void {
  if (!Detector) {
    callbacks.current.onError('Trình duyệt này không có bộ đọc mã có sẵn.')
    return () => undefined
  }
  const detector = new Detector({ formats: FORMATS })
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
        media.getTracks().forEach((t) => t.stop())
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
    stream?.getTracks().forEach((t) => t.stop())
  }
}
