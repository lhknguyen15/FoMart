# Product

<!-- impeccable:product-schema 1 -->

## Platform

web

## Users

- **Chủ tiệm tạp hóa** (tiệm 1–5 người, 300–1.000 mã hàng, trước đây ghi sổ hoặc Excel).
  - Bán hàng ở quầy.
  - Nhập hàng khi nhà cung cấp giao.
  - Kiểm kê, xem báo cáo lãi và tồn.
  - Quản lý tài khoản nhân viên.
  - Nhập hàng và xem báo cáo thường làm lúc vắng khách, có khi trên máy tính kèm máy quét cầm tay.
- **Nhân viên bán hàng** (0–4 người).
  - Đăng nhập nhanh bằng PIN khi đổi ca.
  - Bán hàng, nhập hàng, chuyển kho, lập phiếu hủy và kiểm kê (chờ chủ duyệt).
  - Không xem lãi và giá vốn; giá nhập chỉ xem được khi chủ tiệm cho phép.
- **Khách mua** không dùng hệ thống, chỉ thấy tổng tiền và mã VietQR khi trả bằng chuyển khoản.

**Tình huống chính:** người bán đứng ở quầy, một tay cầm hàng, một tay cầm điện thoại, khách đang chờ. Một lượt 1–3 món trả tiền mặt phải xong trong khoảng 5 giây, không quá 3 lần chạm (NFR-04).

## Product Purpose

FoMart cho tiệm tạp hóa nhỏ biết chính xác còn bao nhiêu hàng, ở đâu, lô nào sắp hết hạn, lãi thật bao nhiêu. Người bán vẫn giữ được nhịp bán nhanh như khi nhẩm giá.

Lý do tồn tại:
- Chủ tiệm không biết tồn thật.
- Hàng hết hạn mới phát hiện.
- Nhập hàng theo cảm tính.
- Từ 1/1/2026 hộ kinh doanh tự kê khai doanh thu thực tế (Nghị quyết 198/2025/QH15).

Thành công nghĩa là:
- bán nhanh không kém sổ tay;
- tồn kho theo từng lô và vị trí luôn khớp sổ biến động;
- hàng hết hạn giảm nhờ xuất lô gần hết hạn trước;
- gợi ý đặt hàng dựa trên lượng bán thật.

## Positioning

So với app bán hàng phổ biến, FoMart khác ở bốn điểm:
- **Quản lý tồn theo lô và hạn dùng ngay từ lúc bán.** Trừ kho lô gần hết hạn trước (FEFO), lô không hạn thì theo thứ tự nhập (FIFO).
- **Chỉ cần điện thoại quét mã**, không bắt mua máy POS. Có máy quét cầm tay thì dùng thêm.
- **Gợi ý nhập hàng** từ dữ liệu bán thật.
- **Không bao giờ chặn lượt bán chỉ vì tồn trên hệ thống chưa khớp** (BR-01, xác nhận lại 27/11/2026): vẫn bán, cảnh báo nhẹ, tồn âm tạm thời, đánh dấu sản phẩm "cần kiểm kê". Sai lệch được sửa bằng kiểm kê định kỳ ("phương án C").

## Operating Context

**Nơi dùng**
- Quầy tiệm tạp hóa: đứng, ánh sáng không đều (buổi thử quét cho thấy chỗ tối thì camera không đọc được mã), ồn, có khách chờ.
- Kho sau (minimart), lúc nhà cung cấp giao hàng.

**Thiết bị**
- Điện thoại có camera là mức tối thiểu; có thể là máy đời cũ (trình duyệt thử nghiệm thiếu `AbortSignal.any`).
- Tablet, máy tính.
- Máy quét mã cầm tay USB/Bluetooth dạng bàn phím.

**Quy trình chính**
- **Bán hàng:** quét mã hoặc bấm nút nhanh → giỏ → tiền mặt hoặc VietQR.
- **Nhập hàng:** quét → số lượng, giá nhập, hạn dùng → hoàn tất tạo lô.
- **Còn lại:** chuyển kho, xuất hủy, kiểm kê và duyệt chênh lệch, hủy hóa đơn, báo cáo.

**Chứng từ và thuật ngữ:** phiếu nhập `PN…`, hóa đơn `HD…`, phiếu chuyển `CK…`, phiếu hủy `XH…`, phiếu kiểm kê `KK…`. Đơn vị cơ sở (lon) và đơn vị lớn (lốc, thùng) có hệ số quy đổi. Lô, lô tạm, thẻ kho.

**Nhịp làm việc:** đổi ca bằng PIN; tự khóa sau thời gian không dùng; chủ tiệm duyệt phiếu do nhân viên lập.

## Capabilities and Constraints

**Đã có API** (ASP.NET Core): danh mục sản phẩm/đơn vị/nhóm hàng/nhà cung cấp/vị trí; đăng nhập mật khẩu và PIN, phân quyền chủ tiệm/nhân viên; nhập hàng tạo lô và hủy bằng chứng từ đảo ngược; bán hàng FEFO, tồn âm, lô tạm, tiền mặt và chuyển khoản chờ xác nhận, hủy hóa đơn; tồn theo lô, thẻ kho, đối soát sổ kho.

**Ràng buộc**
- Không sửa hoặc giảm giá trên giỏ ở phiên bản 1; giá lấy theo đơn vị lúc bán.
- Chứng từ đã hoàn tất không sửa, chỉ hủy bằng chứng từ đảo ngược.
- Tiền là số nguyên đồng, hiển thị `18.000đ`; ngày `dd/mm/yyyy`; giờ Việt Nam; giao diện tiếng Việt (NFR-08).
- Màn hình bán hàng phải chạy khi mất mạng (NFR-05).
- Phân quyền kiểm ở máy chủ (NFR-09).

**Nguồn quét:** camera (zxing-cpp WebAssembly trên web, ADR-0003), máy quét bàn phím, tìm theo tên hoặc gõ số mã, chụp ảnh để đọc. Tất cả đi vào cùng một sự kiện "đã nhận mã X".

**Sau này:** app mobile riêng làm sau cùng, dùng chung backend, quét bằng camera native. Đến lúc đó web vẫn phải dùng được trên điện thoại.

**Chưa quyết**
- Xử lý tồn âm khi hóa đơn ngoại tuyến đồng bộ trễ.
- Nguồn dữ liệu ban đầu (nhập Excel hay khai báo dần).
- Tồn đầu kỳ (schema đã hỗ trợ kiểm kê toàn bộ).

## Brand Commitments

- **Tên:** FoMart.
- **Logo** (đã chọn, phương án A "Mái hiên"): mái che sọc vàng `#f2b33d` / kem `#fdf3dc` trên chữ F, nền xanh lá `#0f6b54`. File trong `frontend/public/` (`logo-mark.svg`, favicon, icon PWA).
- **Icon:** chỉ dùng bộ Phosphor, không tự vẽ icon.
- **Giọng văn:** tiếng Việt đời thường của tiệm tạp hóa, dùng đúng thuật ngữ người bán dùng (thùng, lốc, lô, hạn dùng, kiểm kê). Câu ngắn, nói việc cần làm. Không dùng giọng marketing.

## Evidence on Hand

- **Chỉ có dữ liệu mẫu:** tiệm "Tạp hóa Mẫu", 2 tài khoản (chủ tiệm, nhân viên), 5 sản phẩm (nước ngọt, nước suối, mì tôm, nước mắm, trứng) với mã vạch giả `893000000xxxx`, 2 vị trí (Kệ bán, Kho sau). Nguồn: `database/seed/dev_sample_data.sql`.
- **Không có tiệm thật để chạy thử.** Không dùng tên, giá, ảnh hay lời nhận xét của tiệm thật. Không bịa khách hàng, lời chứng thực, số liệu doanh thu hay tỷ lệ cải thiện.
- **Đây là dự án cá nhân đưa vào CV.**

## Product Principles

1. **Không bao giờ làm người bán chậm lại trước mặt khách.** Mỗi lần chạm thêm, mỗi hộp thoại chặn lượt bán đều là lỗi thiết kế. Sai lệch thì ghi nhận để xử lý sau.
2. **Mọi con số tồn kho đều truy được về chứng từ.** Giao diện cho thấy con số đến từ đâu (phiếu, lô, thẻ kho), không cho sửa tay.
3. **Hạn dùng và lô là thông tin hạng nhất,** không phải trường phụ. Người bán phải thấy ngay hàng sắp hết hạn và lô nào đang bị trừ.
4. **Một tiệm một người và minimart nhiều người dùng cùng sản phẩm.** Khác nhau bằng cấu hình (vị trí, nhân viên, quyền), không phải hai sản phẩm.

## Accessibility & Inclusion

Theo NFR-07 và buổi thử quét:
- màn hình bán hàng dùng được bằng một tay;
- nút chính tối thiểu 48 px;
- chữ tổng tiền đọc được từ khoảng cách của khách;
- phản hồi khi quét được không chỉ dựa vào màu: có tiếng bíp và rung;
- chữ tiếng Việt có dấu hiển thị đầy đủ.

Chưa xác lập chuẩn WCAG cụ thể. Mặc định đủ tương phản 4.5:1 cho chữ thường.
