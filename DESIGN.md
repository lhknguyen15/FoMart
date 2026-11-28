---
name: FoMart
description: Quản lý bán hàng và tồn kho theo lô cho tiệm tạp hóa, bán bằng quét mã trên điện thoại.
colors:
  awning-green: "#0f6b54"
  awning-green-deep: "#0b5342"
  awning-green-mid: "#1f8a6d"
  awning-green-mist: "#eaf5f1"
  awning-green-wash: "#cfe8de"
  awning-amber: "#f2b33d"
  awning-cream: "#fdf3dc"
  shop-paper: "#f6f8f7"
  surface: "#ffffff"
  hairline: "#dde4e1"
  ink: "#17211e"
  ink-muted: "#5b6763"
  state-ok: "#1b7a44"
  state-warn: "#8f5b10"
  state-bad: "#c0392b"
typography:
  display:
    fontFamily: "Be Vietnam Pro, system-ui, sans-serif"
    fontSize: "2.5rem"
    fontWeight: 700
    lineHeight: 1.1
    fontFeature: "tnum"
  headline:
    fontFamily: "Be Vietnam Pro, system-ui, sans-serif"
    fontSize: "1.5rem"
    fontWeight: 700
    lineHeight: 1.25
  title:
    fontFamily: "Be Vietnam Pro, system-ui, sans-serif"
    fontSize: "1.125rem"
    fontWeight: 600
    lineHeight: 1.35
  body:
    fontFamily: "Be Vietnam Pro, system-ui, sans-serif"
    fontSize: "1rem"
    fontWeight: 400
    lineHeight: 1.5
  label:
    fontFamily: "Be Vietnam Pro, system-ui, sans-serif"
    fontSize: "0.875rem"
    fontWeight: 600
    lineHeight: 1.35
rounded:
  sm: "8px"
  md: "12px"
  lg: "16px"
  full: "9999px"
spacing:
  xs: "4px"
  sm: "8px"
  md: "16px"
  lg: "24px"
  xl: "32px"
components:
  button-primary:
    backgroundColor: "{colors.awning-green}"
    textColor: "{colors.surface}"
    typography: "{typography.title}"
    rounded: "{rounded.md}"
    height: "56px"
    padding: "0 24px"
  button-primary-pressed:
    backgroundColor: "{colors.awning-green-deep}"
    textColor: "{colors.surface}"
  button-secondary:
    backgroundColor: "{colors.surface}"
    textColor: "{colors.ink}"
    typography: "{typography.label}"
    rounded: "{rounded.md}"
    height: "48px"
    padding: "0 16px"
  input:
    backgroundColor: "{colors.surface}"
    textColor: "{colors.ink}"
    typography: "{typography.body}"
    rounded: "{rounded.sm}"
    height: "48px"
    padding: "0 12px"
  card:
    backgroundColor: "{colors.surface}"
    textColor: "{colors.ink}"
    rounded: "{rounded.md}"
    padding: "16px"
  app-bar:
    backgroundColor: "{colors.awning-green}"
    textColor: "{colors.surface}"
    typography: "{typography.title}"
    height: "56px"
---

# Design System: FoMart

Token gốc nằm ở `frontend/src/index.css` (`@theme` của Tailwind v4). File này mô tả cách dùng chúng; khi hai nơi lệch nhau thì sửa cả hai trong cùng một commit.

## Overview

**Creative North Star: "Quầy tạp hóa đầu ngõ"**

FoMart trông và cư xử như một quầy tạp hóa quen: sạch, sáng, mọi thứ ở đúng chỗ tay với tới. Người bán đứng, một tay cầm hàng, khách đang chờ. Vì vậy giao diện ưu tiên những thứ phải thấy trong một cái liếc: món vừa quét, tổng tiền, nút "Xong". Phong cách chỉ nằm ở vài chi tiết chính xác. Màu xanh lá đậm và sọc vàng của mái hiên (logo) làm nền nhận diện, chữ Be Vietnam Pro hiển thị tiếng Việt chuẩn, số tiền in đậm thẳng cột.

Mật độ vừa phải trên điện thoại, dày hơn trên máy tính (danh sách, bảng). Bề mặt phẳng, tách lớp bằng viền mảnh và nền giấy hơi ngả xanh, không dùng bóng đổ. Màu trạng thái luôn đi kèm chữ, và khi quét thì kèm cả âm thanh hoặc rung, để không ai phải đoán.

**Key Characteristics:**
- Nền giấy sáng, chữ gần đen, xanh mái hiên cho hành động chính và thanh đầu trang.
- Vàng mái hiên rất hiếm, chỉ ở chỗ mắt phải đến trước.
- Nút to, chắc tay, phẳng; nhấn xuống thấy rõ.
- Số (tiền, số lượng, mã) in đậm, cùng độ rộng chữ số, căn thẳng.
- Trạng thái nói bằng chữ + màu + (khi quét) âm thanh, không bằng màu đơn thuần.

## Colors

Một màu thương hiệu (xanh mái hiên) trên nền trung tính hơi ngả xanh, một màu nhấn hiếm (vàng mái hiên), và ba màu trạng thái đủ tương phản để làm chữ.

Tên token ở đây ↔ class Tailwind trong code: `awning-green` = `brand-600`, `awning-green-deep` = `brand-700`, `awning-green-mid` = `brand-500`, `awning-green-mist` = `brand-50`, `awning-green-wash` = `brand-100`, `awning-amber` = `accent`, `awning-cream` = `cream`, `shop-paper` = `paper`, `surface` = `surface`, `hairline` = `line`, `ink` / `ink-muted` giữ tên, `state-ok|warn|bad` = `ok|warn|bad`.

### Primary
- **Xanh mái hiên** (`awning-green`): nền ô logo, thanh đầu trang, nút chính, viền ô đang chọn. Chữ trắng trên nền này đạt 6.46:1.
- **Xanh mái hiên đậm** (`awning-green-deep`): thanh treo mái hiên trên logo; trạng thái nhấn của nút chính; chữ nhấn mạnh trên nền sáng.
- **Xanh lá vừa** (`awning-green-mid`): vòng focus quanh ô nhập và nút.
- **Sương xanh / Rửa xanh** (`awning-green-mist`, `awning-green-wash`): nền ô đang chọn, nền nút phụ mang màu thương hiệu.

### Secondary
- **Vàng mái hiên** (`awning-amber`): sọc mái hiên trên logo, vạch ngắm khi quét, một điểm nhấn duy nhất trên màn hình (ví dụ khung tổng tiền). Không dùng làm màu chữ (trên nền trắng quá nhạt); chữ đặt trên nó dùng `ink` (8.87:1).
- **Kem mái hiên** (`awning-cream`): sọc kem và chữ F trên logo; nền nhấn ấm khi cần tách một khối khỏi `surface`.

### Neutral
- **Giấy tiệm** (`shop-paper`): nền trang.
- **Mặt quầy** (`surface`): nền thẻ, ô nhập, nút phụ.
- **Viền tóc** (`hairline`): viền thẻ, ô nhập, đường kẻ danh sách.
- **Mực** (`ink`): chữ chính (16.5:1 trên trắng).
- **Mực nhạt** (`ink-muted`): chữ phụ, nhãn, chú thích (5.89:1 trên trắng).

### State
- **Đủ hàng / thành công** (`state-ok`, 5.37:1), **cảnh báo** (sắp hết hạn, tồn âm, mã chưa có: `state-warn`, 5.72:1), **lỗi** (`state-bad`, 5.44:1). Cả ba dùng được cho chữ thường trên nền trắng; làm nền thì pha nhạt (10–15%) và giữ chữ `ink`.

### Named Rules
**The One Awning Rule.** Vàng mái hiên xuất hiện tối đa ở một chỗ trên mỗi màn hình (ngoài logo). Nó hiếm nên mắt tìm thấy ngay.

**The Not-Only-Color Rule.** Không trạng thái nào chỉ được báo bằng màu. Luôn có chữ ("Mã chưa có trong danh mục", "Sắp hết hạn") và khi quét thì có tiếng bíp + rung.

## Typography

**Display / Body Font:** Be Vietnam Pro (tự lưu qua `@fontsource/be-vietnam-pro`, độ đậm 400 / 600 / 700), dự phòng `system-ui, sans-serif`.

**Character:** Một họ chữ sans hình học thiết kế cho tiếng Việt: dấu thanh rõ, không chồng lên dòng trên. Thứ bậc tạo bằng độ đậm và cỡ, không cần họ chữ thứ hai.

### Hierarchy
- **Display** (700, 2.5rem, 1.1): tổng tiền ở màn hình bán hàng, số chính cần đọc từ khoảng cách của khách. Luôn `tabular-nums`.
- **Headline** (700, 1.5rem, 1.25): tiêu đề màn hình.
- **Title** (600, 1.125rem, 1.35): tên sản phẩm trong giỏ, tiêu đề thẻ, chữ trên nút chính và thanh đầu trang.
- **Body** (400, 1rem, 1.5): nội dung, ô nhập (không nhỏ hơn 16px để iPhone không tự phóng to khi chạm ô).
- **Label** (600, 0.875rem, 1.35): nhãn ô nhập, nút phụ, chú thích trạng thái. Chú thích phụ có thể 0.8125rem / 400 màu `ink-muted`.

### Named Rules
**The Tabular Number Rule.** Mọi số tiền, số lượng, mã vạch, mã chứng từ dùng `tabular-nums`; trong danh sách căn phải và cùng cỡ để so cột được.

## Layout

Hai khung theo bề rộng màn hình, cùng một bộ token và component.

- **Điện thoại và máy tính bảng (< 1024px): khung tác vụ.** Thanh đầu trang xanh dính trên cùng (56px, tôn trọng `safe-area-inset`) + hàng tab màn hình chính ngay dưới. Một cột, lề ngang 16px, khoảng cách giữa khối 16px (nhóm chặt 8px). Danh sách là thẻ xếp dọc.
- **Vùng ngón cái:** hành động chính của màn hình thao tác nằm ở nửa dưới, rộng hết cột; trên điện thoại thanh thanh toán / hoàn tất dính đáy.
- **Máy tính (≥ 1024px, `lg`): khung quản lý kiểu dashboard.** Menu bên trái rộng 240px, nền `awning-green`, dính toàn chiều cao; vùng nội dung nền `shop-paper` dùng hết chiều rộng còn lại, lề 32px, tối đa 1440px. Không còn thanh đầu trang: tên cửa hàng, người đăng nhập, đăng xuất nằm trong menu bên.
  - **Trang danh sách** (hóa đơn, phiếu nhập, tồn kho): tiêu đề + bộ lọc một hàng, bên dưới là **bảng** (xem Components > Tables).
  - **Trang chi tiết:** hai cột, thông tin tóm tắt bên trái (tối đa 420px), các dòng bên phải.
  - **Trang thao tác** (bán hàng, phiếu nhập nháp): hai cột, trái là quét / tìm / form, phải là giỏ hoặc các dòng + tổng + nút chính, dính khi cuộn. Màn hình bán hàng vẫn là màn hình tác vụ, không thành dashboard.
- **Không kéo dài dòng chữ quá 75 ký tự**; bảng thì được rộng.
- **Nhịp khoảng cách:** bội số 4px; dùng các bước 4 / 8 / 16 / 24 / 32.

## Elevation & Depth

Phẳng hoàn toàn. Độ sâu tạo bằng nền (`shop-paper` dưới `surface`) và viền tóc 1px, không dùng bóng đổ. Ngoại lệ duy nhất là thanh đầu trang dính có bóng mảnh (`shadow-sm`) để tách khỏi nội dung cuộn bên dưới. Khay trượt từ dưới lên (bottom sheet) và hộp thoại tách bằng lớp nền tối mờ phía sau, không bằng bóng.

### Named Rules
**The Flat Counter Rule.** Bề mặt phẳng khi nghỉ. Muốn tách một khối thì đổi nền hoặc thêm viền tóc, không thêm bóng.

## Shapes

Bo góc vừa và nhất quán: ô nhập 8px, nút và thẻ 12px, khay/hộp thoại 16px, chip và huy hiệu bo tròn hết. Logo là ô vuông bo 24/96 (25%). Viền luôn 1px `hairline`; ô đang chọn hoặc đang focus đổi sang viền màu thương hiệu, không dày hơn 2px.

## Components

### Buttons
- **Chắc tay, phẳng:** màu đặc, không viền nổi, không bóng; nhấn xuống đổi sang màu đậm hơn và thu nhỏ rất nhẹ (`scale(0.98)`).
- **Shape:** bo 12px.
- **Primary:** nền `awning-green`, chữ trắng Title 600, cao 56px trên màn hình thao tác (tối thiểu 48px, NFR-07), rộng hết cột trên điện thoại. Mỗi màn hình tối đa một nút chính.
- **Secondary:** nền `surface`, viền `hairline`, chữ `ink`, cao 48px.
- **Ghost / liên kết:** chữ `awning-green-deep`, không nền, vùng chạm vẫn ≥ 44px.
- **Focus:** vòng 2px `awning-green-mid` cách 2px; không bao giờ bỏ outline mà không thay thế.
- **Disabled / đang xử lý:** giữ nguyên kích thước, độ mờ 60%, chữ đổi sang việc đang làm ("Đang đăng nhập…").

### Inputs / Fields
- **Style:** nền `surface`, viền 1px `hairline`, bo 8px, cao 48px, chữ Body 16px; nhãn Label phía trên (không dùng placeholder thay nhãn).
- **Focus:** viền `awning-green` + vòng 2px `awning-green-mid` nhạt.
- **Error:** viền `state-bad`, thông báo lỗi Label màu `state-bad` ngay dưới ô, nói lỗi gì và sửa thế nào.
- **Số:** `inputMode="numeric"` / `"tel"` để điện thoại mở bàn phím số.

### Cards / Containers
- **Corner Style:** 12px.
- **Background:** `surface` trên nền `shop-paper`.
- **Shadow Strategy:** không (The Flat Counter Rule).
- **Border:** 1px `hairline`.
- **Internal Padding:** 16px.
- Không lồng thẻ trong thẻ.

### Navigation
- **Thanh đầu trang (< 1024px):** nền `awning-green`, cao 56px, logo 32px + tên cửa hàng (Title, trắng). Bên phải: nút đăng xuất (icon Phosphor, vùng chạm 44px); từ 640px hiện thêm tên người đăng nhập.
- **Tab màn hình chính (< 1024px):** ngay dưới thanh đầu trang, chữ `awning-green-mist` trên nền xanh, icon `bold`; tab đang mở chữ trắng + gạch chân 4px vàng mái hiên (chỗ vàng duy nhất của màn hình).
- **Menu bên (≥ 1024px):** 240px, nền `awning-green`. Trên cùng: logo 40px + tên cửa hàng. Giữa: các mục (icon `bold` + Label 1rem, cao 48px, bo 8px); mục đang mở nền `awning-green-deep`, chữ trắng, chấm tròn 8px vàng mái hiên bên phải (chỗ vàng duy nhất). Mục chỉ dành cho chủ tiệm không hiện với nhân viên. Dưới cùng: tên + vai trò người đăng nhập và nút đăng xuất.

### Tables (≥ 1024px)
- Thẻ `surface` viền tóc bo 12px bọc cả bảng; hàng tiêu đề Label `ink-muted` trên nền `shop-paper`, kẻ ngang 1px `hairline`, không kẻ dọc.
- Hàng cao tối thiểu 48px, cả hàng bấm được (mở chi tiết) với nền `awning-green-mist` khi rê chuột / focus; ô đầu là liên kết thật để dùng bàn phím.
- Cột số (tiền, số lượng) căn phải, `tabular-nums`; trạng thái là huy hiệu có chữ.
- Dưới 1024px cùng dữ liệu đó hiện thành danh sách thẻ.

### Scan Result Card (thành phần đặc trưng)
Thẻ kết quả ngay dưới khung camera: mã số Display-cỡ-nhỏ (1.5rem, 700, tabular), tên sản phẩm Title, đơn vị · giá. Viền 2px và nền pha 10% theo trạng thái (`state-ok` có trong danh mục, `state-warn` chưa có, `state-bad` lỗi), kèm chữ mô tả trạng thái. Khung camera chớp viền `state-ok` 600ms mỗi lần đọc được mã (tắt hiệu ứng khi `prefers-reduced-motion`).

### Icons
Chỉ bộ Phosphor: `regular` mặc định, `bold` cho nút chính và màn hình bán hàng, `duotone` cho điểm nhấn hiếm (trạng thái rỗng). Cỡ theo chữ đi kèm (1.25em), màu `currentColor`. Không tự vẽ icon, không dùng emoji.

## Do's and Don'ts

### Do:
- **Do** đặt hành động chính của màn hình thao tác ở nửa dưới, rộng hết cột, cao 56px.
- **Do** dùng `tabular-nums` cho mọi số tiền, số lượng, mã; định dạng tiền `18.000đ`, ngày `dd/mm/yyyy`.
- **Do** viết nhãn và lỗi bằng tiếng Việt đời thường của tiệm (thùng, lốc, lô, hạn dùng), nói rõ việc cần làm.
- **Do** cho mọi trạng thái (đang tải, lỗi, rỗng) một hình dạng riêng ngay tại chỗ, không chỉ đổi màu.
- **Do** tôn trọng `safe-area-inset` và `prefers-reduced-motion`.

### Don't:
- **Don't** dùng bóng đổ cho thẻ, nút, ô nhập (The Flat Counter Rule).
- **Don't** dùng vàng mái hiên quá một chỗ mỗi màn hình, hoặc làm màu chữ trên nền sáng.
- **Don't** báo trạng thái chỉ bằng màu (The Not-Only-Color Rule).
- **Don't** dùng nhãn nhỏ viết hoa phía trên tiêu đề (eyebrow), chữ chuyển màu gradient, viền màu một bên của thẻ, emoji thay icon.
- **Don't** đặt ô nhập chữ nhỏ hơn 16px, hoặc nút chạm nhỏ hơn 44px (48px cho nút chính).
- **Don't** dùng thư viện component dựng sẵn (MUI, Ant Design...); chỉ Tailwind + token ở `index.css`.
