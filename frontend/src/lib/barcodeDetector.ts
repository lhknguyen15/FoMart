import { BarcodeDetector as ZxingWasmDetector, prepareZXingModule } from 'barcode-detector/ponyfill'
import zxingReaderWasmUrl from 'zxing-wasm/reader/zxing_reader.wasm?url'

// Hai bộ đọc mã cùng giao diện BarcodeDetector:
// - getNativeDetector(): bộ đọc có sẵn của trình duyệt (Chrome Android), chưa có kiểu trong lib DOM của TypeScript.
// - ZxingWasmDetector: zxing-cpp biên dịch sang WebAssembly (gói barcode-detector), chạy trên mọi trình duyệt.

export interface DetectedBarcode {
  rawValue: string
}

export interface BarcodeDetectorLike {
  detect(source: HTMLVideoElement | ImageBitmap): Promise<DetectedBarcode[]>
}

export type BarcodeDetectorConstructor = new (options?: { formats?: string[] }) => BarcodeDetectorLike

// Trả lớp BarcodeDetector của trình duyệt, undefined nếu không có (Safari, Firefox).
export function getNativeDetector(): BarcodeDetectorConstructor | undefined {
  return (globalThis as { BarcodeDetector?: BarcodeDetectorConstructor }).BarcodeDetector
}

// Mặc định thư viện tải file .wasm từ CDN jsDelivr; FoMart tự phục vụ file này (đi kèm bản build, chạy được ngoại tuyến).
prepareZXingModule({
  overrides: {
    locateFile: (path: string, prefix: string) => (path.endsWith('.wasm') ? zxingReaderWasmUrl : prefix + path),
  },
})

export const ZxingWasm: BarcodeDetectorConstructor = ZxingWasmDetector as unknown as BarcodeDetectorConstructor

const PHOTO_FORMATS = ['ean_13', 'ean_8', 'upc_a', 'upc_e', 'code_128']

export interface PhotoDecodeResult {
  code: string | null
  reader: 'zxing-cpp' | 'native' | null
  width: number
  height: number
  elapsedMs: number
}

// Đọc mã từ ảnh chụp (app camera gốc của điện thoại: lấy nét tốt, đủ độ phân giải). Thử zxing-cpp trước, rồi bộ đọc có sẵn.
export async function decodeImageFile(file: File): Promise<PhotoDecodeResult> {
  const startedAt = performance.now()
  const bitmap = await createImageBitmap(file)
  try {
    const readers: [PhotoDecodeResult['reader'], BarcodeDetectorConstructor | undefined][] = [
      ['zxing-cpp', ZxingWasm],
      ['native', getNativeDetector()],
    ]
    for (const [reader, Detector] of readers) {
      if (!Detector) continue
      const found = await new Detector({ formats: PHOTO_FORMATS }).detect(bitmap).catch(() => [])
      const first = found[0]
      if (first) {
        return { code: first.rawValue, reader, width: bitmap.width, height: bitmap.height, elapsedMs: Math.round(performance.now() - startedAt) }
      }
    }
    return { code: null, reader: null, width: bitmap.width, height: bitmap.height, elapsedMs: Math.round(performance.now() - startedAt) }
  } finally {
    bitmap.close()
  }
}
