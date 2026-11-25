# Sơ đồ workflow theo module

Bản có hình vẽ: https://claude.ai/artifact/DszUWnwqBqLE91zYA5Nd2v (mục 13). Bản dưới đây dùng Mermaid để đọc được trong repo. Bước có ghi chú "(đổi tồn)" là bước làm thay đổi tồn kho.

## Luồng hàng tổng quát

```mermaid
flowchart LR
  NCC[Nhà cung cấp] -->|Nhập hàng| KHO[Kho sau]
  KHO -->|Chuyển kho| KE[Kệ bán]
  KE -->|Bán hàng| KH[Khách hàng]
  KHO --> HUY[Xuất hủy: bể, hư, hết hạn]
  KE --> HUY
  KK[[Kiểm kê định kỳ: điều chỉnh sai lệch chưa biết]] -.-> KHO
  KK -.-> KE
```

Tiệm chỉ có một vị trí thì kho sau và kệ bán là một, bước chuyển kho biến mất.

## 13.1. Danh mục sản phẩm (UC-01)

```mermaid
flowchart TD
  A[Quét mã vạch] --> B{Mã đã có?}
  B -->|có| C[Mở sản phẩm để sửa]
  B -->|chưa| D[Tạo sản phẩm]
  D --> E[Khai báo đơn vị: hệ số, mã, giá]
  E --> F{Giá đơn vị lớn hợp lý?}
  F -->|quá cao| G[Cảnh báo, xác nhận]
  F -->|hợp lý| H[Lưu, ghi lịch sử giá]
  G --> H
  C --> H
```

Đã có giao dịch thì khóa hệ số quy đổi (BR-12).

## 13.2. Đặt hàng và nhập hàng (UC-03, UC-04)

```mermaid
flowchart TD
  A[Gợi ý đặt hàng] --> B[Lập phiếu đặt - chưa đổi tồn]
  B --> C[Gửi NCC qua Zalo]
  C --> D[Nhà cung cấp giao]
  D --> E[Quét mã, nhập số lượng, giá]
  E --> F{Có quản lý hạn dùng?}
  F -->|có| G[Nhập hạn dùng, số lô]
  F -->|không| H[Hoàn tất phiếu nhập]
  G --> H
  H --> I[Đối chiếu phiếu đặt - nếu có]
  I --> J[Tạo lô, cộng tồn, ghi stock_moves - đổi tồn]
```

Có thể bắt đầu thẳng từ "Nhà cung cấp giao" mà không cần phiếu đặt. Giá vốn quy về đơn vị cơ sở (BR-11).

## 13.3. Bán hàng (UC-05)

```mermaid
flowchart TD
  A[Quét mã / nút nhanh] --> B{Mã đã có?}
  B -->|chưa| C[Tạo nhanh sản phẩm]
  B -->|có| D[Thêm vào giỏ]
  C --> D
  D --> E{Còn món?}
  E -->|còn| A
  E -->|hết| F{Thanh toán?}
  F -->|tiền mặt| G[Bấm Xong]
  F -->|chuyển khoản| H[Hiện mã VietQR]
  H --> I[Bấm Đã nhận]
  G --> J[Lưu hóa đơn, trừ kho FEFO - đổi tồn]
  I --> J
  J --> A
```

Tồn không đủ không chặn bán: tự chuyển kho (BR-18) hoặc đánh dấu "cần kiểm kê" (BR-01). Khách chưa chuyển khoản xong thì giữ giỏ ở trạng thái "chờ thanh toán".

## 13.4. Chuyển kho (UC-07)

```mermaid
flowchart TD
  A[Gợi ý cần chuyển] --> B[Chọn nơi đi, nơi đến]
  B --> C[Quét mã, số lượng]
  C --> D{Nơi đi đủ hàng?}
  D -->|đủ| E[Chuyển lô theo FEFO - đổi tồn]
  D -->|thiếu| F[Báo thiếu, sửa số lượng]
```

Chỉ hiện khi có từ 2 vị trí (BR-16). Bán hàng thiếu trên kệ cũng tự tạo phiếu này (BR-18).

## 13.5. Xuất hủy (UC-08)

```mermaid
flowchart TD
  A[Lô sắp/đã hết hạn] --> C[Chọn lô, số lượng, lý do]
  B[Quét hàng bể, hư] --> C
  C --> D{Chủ tiệm lập?}
  D -->|có| E[Trừ tồn, ghi tổn thất - đổi tồn]
  D -->|không| F[Chờ chủ duyệt]
  F --> G{Duyệt?}
  G -->|duyệt| E
  G -->|từ chối| H[Hủy phiếu, giữ tồn]
```

## 13.6. Kiểm kê (UC-09)

```mermaid
flowchart TD
  A[Tạo phiếu kiểm kê] --> B[Chốt số hệ thống - người đếm không thấy]
  B --> C[Đếm mù, nhập số đếm]
  C --> D[Gửi phiếu]
  D --> E[Tính chênh lệch - BR-19]
  E --> F{Có lệch?}
  F -->|có| G[Chủ gán nguyên nhân]
  F -->|không| I[Đóng phiếu, bỏ cờ cần kiểm kê]
  G --> H[Điều chỉnh tồn - đổi tồn]
  H --> I
```

Nguyên nhân: âm → bán không ghi nhận / thất thoát / không rõ; dương → nhập hàng chưa ghi. Chênh lệch = số đếm − (số hệ thống lúc tạo phiếu + biến động trong lúc đếm).
