// BarcodeDetector có sẵn trong Chrome Android, chưa có kiểu trong lib DOM của TypeScript.

export interface DetectedBarcode {
  rawValue: string
}

export interface BarcodeDetectorLike {
  detect(source: CanvasImageSource): Promise<DetectedBarcode[]>
}

export type BarcodeDetectorConstructor = new (options?: { formats?: string[] }) => BarcodeDetectorLike

// Trả lớp BarcodeDetector của trình duyệt, undefined nếu không có (Safari, Firefox).
export function getNativeDetector(): BarcodeDetectorConstructor | undefined {
  return (globalThis as { BarcodeDetector?: BarcodeDetectorConstructor }).BarcodeDetector
}
