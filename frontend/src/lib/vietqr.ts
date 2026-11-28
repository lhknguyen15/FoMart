// Nội dung mã VietQR (chuẩn EMVCo QR của NAPAS 247) để khách quét bằng app ngân hàng:
// số tài khoản, số tiền và nội dung chuyển khoản đã điền sẵn (UC-05 bước 5, quyết định số 4: không tích hợp ngân hàng).

export interface VietQrInput {
  bankBin: string // mã BIN ngân hàng 6 số, ví dụ 970436
  accountNo: string
  amount: number // đồng, số nguyên
  content: string // nội dung chuyển khoản: mã hóa đơn (chỉ chữ không dấu, số)
}

// Một trường TLV: mã 2 số + độ dài 2 số + giá trị.
function field(id: string, value: string): string {
  return `${id}${String(value.length).padStart(2, '0')}${value}`
}

// CRC-16/CCITT-FALSE (đa thức 0x1021, khởi tạo 0xFFFF) mà chuẩn EMVCo dùng cho trường 63.
export function crc16(text: string): string {
  let crc = 0xffff
  for (let i = 0; i < text.length; i++) {
    crc ^= text.charCodeAt(i) << 8
    for (let bit = 0; bit < 8; bit++) crc = crc & 0x8000 ? ((crc << 1) ^ 0x1021) & 0xffff : (crc << 1) & 0xffff
  }
  return crc.toString(16).toUpperCase().padStart(4, '0')
}

// Bỏ dấu và ký tự lạ khỏi nội dung chuyển khoản (app ngân hàng chỉ nhận ASCII), tối đa 25 ký tự.
function toTransferContent(text: string): string {
  return text
    .normalize('NFD')
    .replace(/[̀-ͯ]/g, '')
    .replace(/đ/g, 'd')
    .replace(/Đ/g, 'D')
    .replace(/[^A-Za-z0-9 ]/g, '')
    .slice(0, 25)
}

export function buildVietQrPayload({ bankBin, accountNo, amount, content }: VietQrInput): string {
  const beneficiary = field('00', bankBin) + field('01', accountNo)
  const merchant = field('00', 'A000000727') + field('01', beneficiary) + field('02', 'QRIBFTTA') // NAPAS, chuyển đến tài khoản
  const body =
    field('00', '01') + // phiên bản
    field('01', '12') + // mã động: có số tiền, dùng một lần
    field('38', merchant) +
    field('53', '704') + // VND
    field('54', String(Math.round(amount))) +
    field('58', 'VN') +
    field('62', field('08', toTransferContent(content))) +
    '6304'
  return body + crc16(body)
}

// Tên ngân hàng theo mã BIN (danh sách NAPAS, các ngân hàng phổ biến); không có thì hiện mã BIN.
const BANK_NAMES: Record<string, string> = {
  '970436': 'Vietcombank',
  '970415': 'VietinBank',
  '970418': 'BIDV',
  '970405': 'Agribank',
  '970422': 'MB Bank',
  '970407': 'Techcombank',
  '970416': 'ACB',
  '970432': 'VPBank',
  '970423': 'TPBank',
  '970403': 'Sacombank',
  '970441': 'VIB',
  '970443': 'SHB',
  '970437': 'HDBank',
}

export function bankName(bin: string): string {
  return BANK_NAMES[bin] ?? `Ngân hàng (BIN ${bin})`
}
