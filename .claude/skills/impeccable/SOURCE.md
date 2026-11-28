# Nguồn của skill này

- Gốc: https://github.com/pbakaus/impeccable, thư mục `.claude/skills/impeccable/`
- Commit: `2fc5e1459efbbf06d92c7306517c286bf12490cf` (26/11/2026), SKILL.md version 4.11.1
- Giấy phép: Apache-2.0 (`LICENSE`, `NOTICE.md` chép nguyên từ gốc repo)
- Chép ngày 27/11/2026, không sửa nội dung.

## Đã bỏ có chủ đích

`scripts/` (launcher `impeccable` / `impeccable.cmd`, `live-browser*.js`, `data/font-index.json`...). Launcher tải binary engine từ GitHub Releases của tác giả; engine gọi `impeccable.style/api`, kiểm tra cập nhật và gửi telemetry mặc định. FoMart dùng skill theo nhánh "Launcher unavailable" của `SKILL.md`: đọc thẳng `PRODUCT.md`, `DESIGN.md` và `reference/`. Không chạy được: `shot`, hook detector, `font-fetch`, `live`, `generate`, roll API.

## Cập nhật

Muốn lấy bản mới: tải repo gốc về thư mục tạm, đọc phần thay đổi của `SKILL.md` và `reference/` (đặc biệt `craft-floor.md`), rồi mới chép đè và sửa commit ở trên. Không chép `scripts/`.
