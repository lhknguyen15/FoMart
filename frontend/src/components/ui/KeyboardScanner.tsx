import { useEffect, useEffectEvent } from 'react'

// Máy quét cầm tay USB/Bluetooth dạng bàn phím (NFR-02, NFR-03): gõ cả mã rất nhanh rồi Enter.
// Nhận ra máy quét theo tốc độ gõ (các ký tự cách nhau dưới 50 ms), không cần chọn ô nhập.
// Khi đang gõ trong một ô nhập thì để ô đó xử lý (ô tìm kiếm tự nhận mã toàn số khi Enter).

interface KeyboardScannerProps {
  onCode: (code: string) => void
}

const MAX_GAP_MS = 50
const MIN_LENGTH = 4

export function KeyboardScanner({ onCode }: KeyboardScannerProps) {
  const emit = useEffectEvent((code: string) => onCode(code))

  useEffect(() => {
    let buffer = ''
    let lastAt = 0
    const onKey = (event: KeyboardEvent) => {
      const target = event.target
      if (target instanceof HTMLInputElement || target instanceof HTMLTextAreaElement || target instanceof HTMLSelectElement) return
      const now = event.timeStamp
      if (now - lastAt > MAX_GAP_MS) buffer = ''
      lastAt = now
      if (event.key === 'Enter') {
        if (buffer.length >= MIN_LENGTH) {
          event.preventDefault()
          emit(buffer)
        }
        buffer = ''
      } else if (event.key.length === 1) {
        buffer += event.key
      }
    }
    window.addEventListener('keydown', onKey)
    return () => window.removeEventListener('keydown', onKey)
  }, [])

  return null
}
