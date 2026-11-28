// Chuẩn hóa mã vạch trước khi tra (ADR-0003 mục 4), giống Domain/Rules/BarcodeRules.cs ở backend:
// bỏ khoảng trắng; mã UPC-A 12 chữ số thêm "0" đầu thành EAN-13.
export function normalizeBarcode(raw: string): string {
  const code = raw.trim()
  return /^[0-9]{12}$/.test(code) ? `0${code}` : code
}
