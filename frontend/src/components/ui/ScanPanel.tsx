import { CameraIcon, CameraSlashIcon, ImageIcon } from '@phosphor-icons/react'
import { lazy, Suspense, useState, type ChangeEvent } from 'react'
import { scanFeedback, unlockAudio } from '../../lib/feedback'
import { KeyboardScanner } from './KeyboardScanner'

// Camera quét tải lười: mã quét + file .wasm (~400 KB) chỉ tải khi bật camera (ADR-0003).
const BarcodeScanner = lazy(() => import('./BarcodeScanner').then((module) => ({ default: module.BarcodeScanner })))

// Máy nhớ người dùng đã bật camera: lần sau mở màn hình quét là camera bật sẵn, quyền camera đã cấp từ lần đầu.
const CAMERA_PREF_KEY = 'fomart-camera-on'
function readCameraPref(): boolean {
  try {
    return localStorage.getItem(CAMERA_PREF_KEY) === '1'
  } catch {
    return false
  }
}
function writeCameraPref(on: boolean): void {
  try {
    localStorage.setItem(CAMERA_PREF_KEY, on ? '1' : '0')
  } catch {
    // Trình duyệt chặn lưu: lần sau bấm bật lại.
  }
}

interface ScanPanelProps {
  // Một mã vừa nhận được, từ camera, máy quét bàn phím hoặc ảnh chụp (ADR-0003 mục 5).
  onCode: (code: string) => void
}

// Khối quét mã dùng chung cho bán hàng và nhập hàng: camera liên tục, chụp ảnh khi quét không được,
// và lắng nghe máy quét cầm tay dạng bàn phím.
export function ScanPanel({ onCode }: ScanPanelProps) {
  const [cameraOn, setCameraOnState] = useState(readCameraPref)
  const setCameraOn = (on: boolean) => {
    writeCameraPref(on)
    setCameraOnState(on)
  }
  const [message, setMessage] = useState<string | null>(null)

  // Chụp ảnh bằng app camera gốc khi quét trực tiếp không được (chỗ tối, chai tròn).
  const readPhoto = async (event: ChangeEvent<HTMLInputElement>) => {
    const file = event.currentTarget.files?.[0]
    event.currentTarget.value = ''
    if (!file) return
    setMessage(null)
    const { decodeImageFile } = await import('../../lib/barcodeDetector')
    const result = await decodeImageFile(file).catch(() => null)
    if (result?.code) onCode(result.code)
    else {
      scanFeedback('warn')
      setMessage('Không thấy mã vạch trong ảnh. Chụp gần hơn, đủ sáng, mã nằm ngang.')
    }
  }

  return (
    <section aria-label="Quét mã" className="flex flex-col gap-2">
      <KeyboardScanner onCode={onCode} />
      {cameraOn ? (
        <Suspense fallback={<div className="flex aspect-4/3 max-h-[38dvh] items-center justify-center rounded-xl bg-ink text-white">Đang bật camera…</div>}>
          <BarcodeScanner
            onCode={onCode}
            onError={(error) => {
              setMessage(error)
              setCameraOn(false)
            }}
          />
        </Suspense>
      ) : (
        <button
          type="button"
          onClick={() => {
            unlockAudio()
            setMessage(null)
            setCameraOn(true)
          }}
          className="flex h-28 flex-col items-center justify-center gap-1 rounded-xl border-2 border-dashed border-brand-600 bg-brand-50 font-semibold text-brand-700 active:bg-brand-100"
        >
          <CameraIcon size="2rem" weight="duotone" aria-hidden="true" />
          Bật camera quét mã
        </button>
      )}
      {message && <p className="text-sm font-semibold text-bad">{message}</p>}
      <div className="flex gap-2">
        {cameraOn && (
          <button type="button" onClick={() => setCameraOn(false)} className="flex min-h-11 items-center gap-1.5 rounded-lg border border-line bg-surface px-3 text-sm font-semibold">
            <CameraSlashIcon size="1.125rem" aria-hidden="true" />
            Tắt camera
          </button>
        )}
        <label className="flex min-h-11 cursor-pointer items-center gap-1.5 rounded-lg border border-line bg-surface px-3 text-sm font-semibold has-focus-visible:outline-2 has-focus-visible:outline-brand-500">
          <ImageIcon size="1.125rem" aria-hidden="true" />
          Chụp ảnh mã
          <input type="file" accept="image/*" capture="environment" onChange={(event) => void readPhoto(event)} className="sr-only" />
        </label>
      </div>
    </section>
  )
}
