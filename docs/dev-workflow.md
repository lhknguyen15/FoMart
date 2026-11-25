# Quy trình phát triển FoMart

## Bước 0 – Chuẩn bị (2–3 ngày)

1. Tạo repo GitHub theo cấu trúc trong `CLAUDE.md`.
2. ~~`docker-compose.yml` chạy SQL Server~~ (đã có): `docker compose up -d` rồi `./database/migrate.ps1 -Docker -Seed`. Mật khẩu SA trong `.env` (mẫu `.env.example`), cổng 14330.
3. GitHub Project: mỗi giai đoạn là một milestone, mỗi việc nhỏ là một issue, ghi rõ UC/BR liên quan (ví dụ "UC-01, BR-09, BR-20").
4. CI tối thiểu bằng GitHub Actions: build + test mỗi lần push.

## Vòng lặp cho mỗi tính năng

1. Đọc lại use case và quy tắc liên quan trong `docs/spec.md` và sơ đồ trong `docs/module-workflows.md`.
2. Thiết kế API (endpoint, request, response) và bảng/cột cần thêm.
3. Viết quy tắc nghiệp vụ thuần trong `Domain/Rules` (thêm unit test khi đã có project `FoMart.Tests`).
4. Nếu cần đổi schema: thêm `database/migrations/V00x__*.sql`, chạy `migrate.ps1`, scaffold lại EF. Rồi code backend (Domain → Application → Infrastructure → API), thử trên Swagger.
5. Code frontend cho màn hình tương ứng.
6. Thử tay trên điện thoại thật, nhất là màn hình có quét mã.
7. Nhánh `feature/<mảng>-<mô-tả>` tách từ `develop` → Pull Request về `develop` → tự đọc lại diff → Squash and merge. Cuối giai đoạn: PR `develop` → `main` và gắn tag (chi tiết: CLAUDE.md, mục "Git flow").
8. Cập nhật spec hoặc thêm ADR nếu đổi ý về nghiệp vụ/kỹ thuật.

## Lộ trình (spec mục 10)

1. Nền tảng (tuần 1–2)
2. Nhập và bán (tuần 3–5) – mốc "dùng thử được". Bắt đầu bằng trừ kho FEFO + sổ biến động, test kỹ trước khi làm màn hình bán hàng.
3. Giữ dữ liệu đúng (tuần 6–7): xuất hủy, hạn dùng, kiểm kê, hủy hóa đơn/phiếu nhập.
4. Báo cáo và đặt hàng (tuần 8–9)
5. Minimart (tuần 10–11): nhân viên, PIN, phân quyền, nhiều vị trí, chuyển kho, máy quét bàn phím, SignalR.
6. Hoàn thiện (tuần 12+): offline, PWA, xuất Excel, chạy thử ở tiệm thật.

## Giai đoạn 1 – thứ tự cụ thể

| # | Việc | Lý do |
|---|---|---|
| 1 | Thử quét mã bằng camera điện thoại (trang React đơn giản, HTTPS) | Rủi ro lớn nhất, cần biết sớm |
| 2 | Skeleton backend + frontend chạy được, gọi được nhau | Khung cho các phần sau |
| 3 | ~~Script database~~ (đã có ở `database/`) → scaffold DbContext + entity, Repository/UoW nền | Nền dữ liệu |
| 4 | Quy tắc sản phẩm/đơn vị trong `Domain/Rules` (BR-09, 10, 12, 20) | Quy tắc lõi |
| 5 | API danh mục, Swagger, seed dữ liệu mẫu | Có dữ liệu làm giao diện |
| 6 | Đăng nhập chủ tiệm bằng JWT | Bảo vệ API |
| 7 | Màn hình danh sách/chi tiết sản phẩm, khai báo mã vạch bằng camera | Hoàn thành UC-01 |
| 8 | Deploy lần đầu | Có link demo sớm |

## Kiểm thử bắt buộc

- Tổng `stock_moves` luôn bằng `stock`.
- Bán 1 thùng trừ đúng 24 lon (theo hệ số).
- FEFO trừ đúng lô hết hạn trước.
- Hai máy bán cùng lúc không sai tồn.
- Hóa đơn offline đồng bộ không bị trùng.

## Nguyên tắc giữ nhịp

- Mỗi issue xong trong 1–2 buổi; lớn hơn thì chia nhỏ.
- Cuối mỗi tuần có thứ chạy được để demo.
- Ghi lại khó khăn và cách giải ngay khi gặp (nguyên liệu cho README, ADR và phỏng vấn).
