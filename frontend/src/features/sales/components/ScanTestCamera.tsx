import { BrowserMultiFormatReader, type IScannerControls } from '@zxing/browser'
import { BarcodeFormat, DecodeHintType } from '@zxing/library'
import { Html5Qrcode, Html5QrcodeSupportedFormats } from 'html5-qrcode'
import { useEffect, useRef } from 'react'
import { getNativeDetector } from '../../../lib/barcodeDetector'

// Thử nghiệm quét mã (ADR-0003): ba engine, cùng một giao diện. Bỏ sau khi chốt thư viện.

export type ScanEngine = 'zxing' | 'html5' | 'native'

// at = performance.now() lúc đọc được mã / lúc camera sẵn sàng, để trang đo thời gian.
interface ScanTestCameraProps {
  engine: ScanEngine
  onCode: (code: string, at: number) => void
  onReady: (at: number) => void
  onError: (message: string) => void
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
  video: { facingMode: { ideal: 'environment' }, width: { ideal: 1280 }, height: { ideal: 720 } },
  audio: false,
}
const HTML5_REGION_ID = 'scan-test-html5'

// Bật camera sau và quét liên tục bằng engine đã chọn; tắt camera khi component bị gỡ.
export function ScanTestCamera({ engine, onCode, onReady, onError }: ScanTestCameraProps) {
  const videoRef = useRef<HTMLVideoElement>(null)
  // Giữ callback mới nhất mà không khởi động lại camera mỗi lần cha vẽ lại.
  const callbacks = useRef({ onCode, onReady, onError })
  useEffect(() => {
    callbacks.current = { onCode, onReady, onError }
  })

  useEffect(() => {
    const video = videoRef.current
    if (engine === 'zxing' && video) return startZxing(video, callbacks)
    if (engine === 'html5') return startHtml5(callbacks)
    if (engine === 'native' && video) return startNative(video, callbacks)
    return undefined
  }, [engine])

  return (
    <div className="relative overflow-hidden rounded-xl bg-ink">
      {engine === 'html5' ? (
        <div id={HTML5_REGION_ID} className="aspect-4/3 w-full" />
      ) : (
        <video ref={videoRef} className="aspect-4/3 w-full object-cover" muted playsInline />
      )}
      {engine !== 'html5' && (
        <div className="pointer-events-none absolute inset-x-[12%] top-1/2 h-0.5 -translate-y-1/2 rounded-full bg-warn/90" />
      )}
    </div>
  )
}

type Callbacks = React.RefObject<Pick<ScanTestCameraProps, 'onCode' | 'onReady' | 'onError'>>

function describe(error: unknown): string {
  if (error instanceof DOMException && error.name === 'NotAllowedError') return 'Chưa cho phép dùng camera.'
  if (error instanceof DOMException && error.name === 'NotFoundError') return 'Không tìm thấy camera.'
  if (error instanceof Error) return error.message
  return String(error)
}

function startZxing(video: HTMLVideoElement, callbacks: Callbacks): () => void {
  let controls: IScannerControls | undefined
  let cancelled = false
  const hints = new Map<DecodeHintType, unknown>([
    [DecodeHintType.POSSIBLE_FORMATS, ZXING_FORMATS],
    [DecodeHintType.TRY_HARDER, true],
  ])
  const reader = new BrowserMultiFormatReader(hints, { delayBetweenScanAttempts: 80 })
  reader
    .decodeFromConstraints(CAMERA, video, (result) => {
      if (result) callbacks.current.onCode(result.getText(), performance.now())
    })
    .then((started) => {
      if (cancelled) started.stop()
      else {
        controls = started
        callbacks.current.onReady(performance.now())
      }
    })
    .catch((error: unknown) => callbacks.current.onError(describe(error)))
  return () => {
    cancelled = true
    controls?.stop()
  }
}

function startHtml5(callbacks: Callbacks): () => void {
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
      () => undefined,
    )
    .then(() => {
      started = true
      if (cancelled) stop()
      else callbacks.current.onReady(performance.now())
    })
    .catch((error: unknown) => callbacks.current.onError(describe(error)))
  return () => {
    cancelled = true
    if (started) stop()
  }
}

function startNative(video: HTMLVideoElement, callbacks: Callbacks): () => void {
  const Detector = getNativeDetector()
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
        const codes = await detector.detect(video)
        const first = codes[0]
        if (first) callbacks.current.onCode(first.rawValue, performance.now())
      }
    } catch {
      // khung hình lỗi: bỏ qua, thử khung sau
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
