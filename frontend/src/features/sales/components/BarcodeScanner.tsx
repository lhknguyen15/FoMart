import { FlashlightIcon } from '@phosphor-icons/react'
import { useEffect, useEffectEvent, useRef, useState } from 'react'
import { ZxingWasm } from '../../../lib/barcodeDetector'

// Camera quét liên tục bằng zxing-cpp WebAssembly (ADR-0003). Tải lười: SalePage import component này bằng React.lazy,
// nên mã quét và file .wasm chỉ tải khi người bán bật camera.

interface BarcodeScannerProps {
  onCode: (code: string) => void
  onError: (message: string) => void
}

const FORMATS = ['ean_13', 'ean_8', 'upc_a', 'upc_e', 'code_128']
const CAMERA: MediaStreamConstraints = {
  video: { facingMode: { ideal: 'environment' }, width: { ideal: 1920 }, height: { ideal: 1080 } },
  audio: false,
}
// Camera đọc liên tục: cùng một mã trong khoảng này chỉ tính một lần.
const SAME_CODE_WINDOW_MS = 1500
const SCAN_INTERVAL_MS = 80

interface Capabilities {
  zoom?: { min: number; max: number }
  torch: boolean
}

export function BarcodeScanner({ onCode, onError }: BarcodeScannerProps) {
  const videoRef = useRef<HTMLVideoElement>(null)
  const [track, setTrack] = useState<MediaStreamTrack | null>(null)
  const [capabilities, setCapabilities] = useState<Capabilities | null>(null)
  const [zoom, setZoom] = useState(1)
  const [torchOn, setTorchOn] = useState(false)
  const [flashKey, setFlashKey] = useState(0)

  // useEffectEvent: vòng quét luôn gọi onCode / onError mới nhất mà không phải khởi động lại camera.
  const emitCode = useEffectEvent((code: string) => {
    setFlashKey((key) => key + 1)
    onCode(code)
  })
  const emitError = useEffectEvent((message: string) => onError(message))

  useEffect(() => {
    const video = videoRef.current
    if (!video) return undefined
    const detector = new ZxingWasm({ formats: FORMATS })
    let stream: MediaStream | undefined
    let timer: number | undefined
    let cancelled = false
    let last: { code: string; at: number } | null = null

    const loop = async () => {
      if (cancelled) return
      try {
        if (video.readyState >= 2) {
          const found = (await detector.detect(video))[0]
          const now = performance.now()
          if (found && !(last && last.code === found.rawValue && now - last.at < SAME_CODE_WINDOW_MS)) emitCode(found.rawValue)
          if (found) last = { code: found.rawValue, at: now }
        }
      } catch {
        // khung hình lỗi: bỏ qua, thử khung sau
      }
      timer = window.setTimeout(loop, SCAN_INTERVAL_MS)
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
        const current = media.getVideoTracks()[0] ?? null
        if (current) {
          const caps = current.getCapabilities?.() as (MediaTrackCapabilities & { zoom?: { min: number; max: number }; torch?: boolean; focusMode?: string[] }) | undefined
          if (caps?.focusMode?.includes('continuous')) {
            current.applyConstraints({ advanced: [{ focusMode: 'continuous' } as unknown as MediaTrackConstraintSet] }).catch(() => undefined)
          }
          setCapabilities({ zoom: caps?.zoom && caps.zoom.max > 1 ? caps.zoom : undefined, torch: caps?.torch === true })
        }
        setTrack(current)
        void loop()
      })
      .catch((error: unknown) => {
        if (error instanceof DOMException && error.name === 'NotAllowedError') emitError('Chưa cho phép dùng camera. Mở cài đặt trình duyệt để cho phép.')
        else if (error instanceof DOMException && error.name === 'NotFoundError') emitError('Không tìm thấy camera trên máy này.')
        else emitError('Không bật được camera.')
      })

    return () => {
      cancelled = true
      window.clearTimeout(timer)
      stream?.getTracks().forEach((t) => t.stop())
    }
  }, [])

  const applyZoom = (value: number) => {
    setZoom(value)
    track?.applyConstraints({ advanced: [{ zoom: value } as unknown as MediaTrackConstraintSet] }).catch(() => undefined)
  }
  const toggleTorch = () => {
    const next = !torchOn
    setTorchOn(next)
    track?.applyConstraints({ advanced: [{ torch: next } as unknown as MediaTrackConstraintSet] }).catch(() => undefined)
  }

  const zoomSteps = capabilities?.zoom ? [1, 2, 3].filter((v) => v >= (capabilities.zoom?.min ?? 1) && v <= (capabilities.zoom?.max ?? 1)) : []

  return (
    <div className="relative overflow-hidden rounded-xl bg-ink">
      <video ref={videoRef} className="aspect-4/3 max-h-[38dvh] w-full object-cover" muted playsInline aria-label="Camera quét mã vạch" />
      <div aria-hidden="true" className="pointer-events-none absolute inset-x-[12%] top-1/2 h-0.5 -translate-y-1/2 rounded-full bg-accent" />
      {flashKey > 0 && (
        <div key={flashKey} aria-hidden="true" className="pointer-events-none absolute inset-0 animate-scan-flash rounded-xl ring-8 ring-ok ring-inset" />
      )}
      {(zoomSteps.length > 1 || capabilities?.torch) && (
        <div className="absolute right-2 bottom-2 flex gap-1.5">
          {zoomSteps.length > 1 &&
            zoomSteps.map((value) => (
              <button
                key={value}
                type="button"
                onClick={() => applyZoom(value)}
                aria-pressed={zoom === value}
                aria-label={`Phóng to ${value} lần`}
                className={`flex size-11 items-center justify-center rounded-full text-sm font-bold tabular-nums ${zoom === value ? 'bg-white text-ink' : 'bg-ink/60 text-white'}`}
              >
                {value}×
              </button>
            ))}
          {capabilities?.torch && (
            <button
              type="button"
              onClick={toggleTorch}
              aria-pressed={torchOn}
              aria-label={torchOn ? 'Tắt đèn' : 'Bật đèn'}
              className={`flex size-11 items-center justify-center rounded-full ${torchOn ? 'bg-accent text-ink' : 'bg-ink/60 text-white'}`}
            >
              <FlashlightIcon size="1.375rem" weight="bold" />
            </button>
          )}
        </div>
      )}
    </div>
  )
}
