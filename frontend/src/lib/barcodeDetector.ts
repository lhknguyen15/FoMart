import { BarcodeDetector as ZxingWasmDetector, prepareZXingModule } from 'barcode-detector/ponyfill'
import zxingReaderWasmUrl from 'zxing-wasm/reader/zxing_reader.wasm?url'

// Hai bộ đọc mã cùng giao diện BarcodeDetector:
// - getNativeDetector(): bộ đọc có sẵn của trình duyệt (Chrome Android), chưa có kiểu trong lib DOM của TypeScript.
// - ZxingWasmDetector: zxing-cpp biên dịch sang WebAssembly (gói barcode-detector), chạy trên mọi trình duyệt.

export interface DetectedBarcode {
  rawValue: string
}

export interface BarcodeDetectorLike {
  detect(source: HTMLVideoElement): Promise<DetectedBarcode[]>
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
