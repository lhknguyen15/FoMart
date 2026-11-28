# ADR-0004: Tự xác nhận chuyển khoản qua SePay (tùy chọn)

- Trạng thái: Đã chấp nhận, làm ở giai đoạn 5 (sau SignalR)
- Ngày: 28/11/2026
- Liên quan: UC-05, SC-03, SC-15, quyết định đã chốt số 4 (sửa bởi ADR này), ADR-0001 (cây thư mục backend), [docs/module-notes/sales.md](../module-notes/sales.md)

## Bối cảnh

Màn hình bán hàng đã hiện mã VietQR cho hóa đơn chuyển khoản. Nội dung VietQR dựng ngay trên máy (`lib/vietqr.ts`), gồm số tiền và nội dung là mã hóa đơn. Theo quyết định số 4, người bán phải mở app ngân hàng xem tiền đã về chưa rồi bấm "Đã nhận tiền". Cách này có ba nhược điểm:

- Chậm ở quầy: mỗi lượt khách phải chuyển qua app khác.
- Dễ sai: người bán có thể bấm nhầm hóa đơn, hoặc tin ảnh chụp màn hình "đã chuyển" của khách.
- Ở minimart, nhân viên không có quyền xem tài khoản ngân hàng của chủ tiệm.

SePay (sepay.vn) theo dõi biến động số dư của tài khoản đã liên kết và gọi webhook (HTTP POST) khi có giao dịch. Payload có mã giao dịch, số tiền, nội dung chuyển khoản, chiều tiền (vào/ra) và thời điểm.

## Quyết định

1. **Tùy chọn, không thay thế.** Tiệm bật SePay trong cài đặt (SC-15) thì hóa đơn chuyển khoản tự xác nhận khi tiền về. Không bật thì giữ nguyên nút "Đã nhận tiền". Nút này luôn còn, làm đường dự phòng khi SePay hoặc mạng gặp sự cố.
2. **Sửa quyết định số 4:** "Tiền mặt hoặc chuyển khoản VietQR. Mặc định nhân viên tự xác nhận; tiệm có thể bật tự xác nhận qua SePay."
3. **Giữ mã VietQR tự dựng**, không dùng ảnh QR của dịch vụ ngoài: vẫn chạy được ngoại tuyến và không gửi số tiền ra bên thứ ba.
4. **Webhook là nguồn báo tin, chưa phải bằng chứng.**
   - Endpoint `[AllowAnonymous]` nhưng bắt buộc có khóa API trong header, so với giá trị đặt trong User Secrets / biến môi trường.
   - Chỉ xử lý giao dịch tiền vào.
   - Khi làm, cân nhắc đối chiếu lại qua API của SePay trước khi xác nhận.
5. **Không xử lý trùng.**
   - Mỗi giao dịch SePay được lưu một dòng, ràng buộc duy nhất trên mã giao dịch.
   - SePay gửi lại cùng giao dịch thì không làm gì và vẫn trả 200.
   - Webhook trả lời nhanh; việc nặng làm trong cùng một transaction ngắn.
6. **Quy tắc khớp** viết thuần ở `Domain/Rules/TransferMatcher.cs`: tách mã hóa đơn khỏi nội dung (ngân hàng có thể thêm chữ trước/sau, đổi chữ hoa/thường), rồi so số tiền.

| Tình huống | Xử lý |
| --- | --- |
| Đúng mã, số tiền ≥ tổng | Xác nhận hóa đơn, đi chung đường với "Đã nhận tiền" (trừ kho FEFO, ghi `stock_moves`) |
| Đúng mã, thiếu tiền | Không xác nhận; hóa đơn vẫn chờ, gắn cờ "thiếu X đ" |
| Không tìm được mã | Giao dịch vào danh sách "chưa khớp"; nhân viên gán tay vào hóa đơn |
| Hóa đơn chỉ còn lô hết hạn (BR-05) | Ghi "đã nhận tiền" nhưng chưa hoàn tất; nhân viên bấm xác nhận đã kiểm hàng |
| Hóa đơn đã hủy / đã thanh toán | Vào danh sách "cần hoàn tiền", không mở lại hóa đơn (BR-03) |

7. **Báo màn hình bán hàng bằng SignalR** (giai đoạn 5): "HD… đã nhận 26.000đ" kèm tiếng bíp. Trước khi có SignalR, danh sách chờ tự tải lại mỗi 30 giây.

## Thay đổi khi làm

- **Database:**
  - Migration mới, bảng `payment_transactions`: mã giao dịch nhà cung cấp (duy nhất), `sale_id` (cho phép trống), số tiền, nội dung, payload gốc, `status` (`matched` | `unmatched` | `underpaid` | `refund_needed`), thời điểm UTC.
  - Thêm cột cấu hình bật SePay vào `core.stores`.
- **Backend:**
  - Thêm chủ đề `Payment` vào mảng `Sales` trong bảng Mảng của CLAUDE.md: `PaymentController`, `PaymentService`, `PaymentRepository`, `PaymentModels.cs`.
  - Khóa API cấu hình ở `SePay:WebhookApiKey`.
- **Frontend:** danh sách giao dịch chưa khớp / cần hoàn tiền (chủ tiệm); bật/tắt SePay ở SC-15.
- **Hạ tầng:** webhook cần URL HTTPS công khai. Khi dev dùng tunnel (Cloudflare Tunnel hoặc ngrok); khi chạy thật phải triển khai server ra mạng.

## Hệ quả

- **Lợi:** người bán không phải rời màn hình bán hàng; giảm nhầm lẫn và gian lận ảnh chụp. Dự án có thêm các phần đáng giá: webhook, chống xử lý trùng, đối soát giao dịch, đẩy realtime.
- **Rủi ro:**
  - SePay đọc được mọi giao dịch của tài khoản liên kết. Khuyên tiệm dùng một tài khoản riêng chỉ để nhận tiền bán hàng.
  - Phụ thuộc dịch vụ bên ngoài: gói miễn phí giới hạn số giao dịch, danh sách ngân hàng hỗ trợ thay đổi theo thời gian. Kiểm tra lại tài liệu và bảng giá SePay khi bắt đầu làm.
  - Chưa áp dụng được khi chạy trên máy dev không có URL công khai.
- **Không làm:** tích hợp trực tiếp API ngân hàng, cổng thanh toán thu hộ (VNPay, MoMo), hoàn tiền tự động.
