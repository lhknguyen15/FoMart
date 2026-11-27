# Frontend – ghi chú

Khung SPA/PWA và các phần dùng chung của frontend (`frontend/`). Tính năng theo mảng ghi thêm ở file mảng tương ứng (`sales.md`, `catalog.md`...). Quy tắc: CLAUDE.md mục "Kiến trúc frontend".

### 27/11/2026 – Dựng khung frontend

Nhánh `feature/frontend-scaffold`. Đã chốt với người dùng: Vite + React + TypeScript + Tailwind CSS, TanStack Query + Zustand, React Hook Form + Zod, Dexie, PWA; gọi API bằng `fetch` (không axios).

**Thư mục mới**

| Thư mục | Vì sao |
| --- | --- |
| `frontend/` | Ứng dụng web (Vite) |
| `frontend/public/` | Icon |
| `frontend/src/app/`, `api/`, `lib/` | Khởi tạo ứng dụng, lớp gọi API, tiện ích định dạng |
| `frontend/src/features/store/hooks/`, `features/sales/pages/` | Chủ đề đầu tiên chạy thật: thông tin cửa hàng, màn hình bán hàng (khung) |

**File mới**

| Đường dẫn | Vai trò |
| --- | --- |
| `package.json`, `package-lock.json` | Package ghim đúng phiên bản (không `^`), đã phát hành ≥ 2 tuần |
| `vite.config.ts` | React, Tailwind, HTTPS tự ký (`plugin-basic-ssl`), mở ra LAN, proxy `/api` và `/health` sang `http://localhost:5224` |
| `tsconfig.json`, `tsconfig.app.json`, `tsconfig.node.json` | TypeScript strict, `noUncheckedIndexedAccess`, `erasableSyntaxOnly` |
| `eslint.config.js` | ESLint + typescript-eslint + react-hooks + react-refresh |
| `index.html`, `.env.example`, `public/favicon.svg` | Trang gốc (`lang="vi"`), mẫu biến môi trường, icon |
| `src/main.tsx` | Gắn `QueryClientProvider`, `RouterProvider`, devtools của TanStack Query (tự bỏ khi build) |
| `src/index.css` | `@import "tailwindcss"` và `@theme`: màu brand, paper/surface/line, ink, ok/warn/bad |
| `src/app/router.tsx`, `AppLayout.tsx`, `NotFoundPage.tsx`, `queryClient.ts` | Route, khung trang, trang 404, cấu hình TanStack Query |
| `src/api/types.ts` | `HTTPResponseData<T>`, `PagedResult<T>`, `ApiError` |
| `src/api/client.ts` | `request()` bọc fetch: JSON, timeout 15 s, đổi lỗi thành `ApiError` (kèm lỗi 400 theo trường, camelCase) |
| `src/api/store.ts` | `StoreResponse`, `getStore()` |
| `src/features/store/hooks/useStore.ts` | `storeKeys`, `useStoreSettings()` |
| `src/features/sales/pages/SalePage.tsx` | Trang mặc định, hiện trạng thái kết nối máy chủ |
| `src/lib/format.ts` | `formatMoney` (18.000đ), `formatDate`, `formatDateTime` theo giờ Việt Nam |

**File sửa**

| Đường dẫn | Thay đổi |
| --- | --- |
| `CLAUDE.md` | Mục "Kiến trúc frontend" (state, cây thư mục, luồng, quy ước, chạy khi dev, danh sách cấm); dòng công nghệ frontend; trạng thái |

**Ghi chú**

- **Phiên bản:** Vite 8.3.8, React 19.3.1, React Router 8.5.1, TanStack Query 5.107.1, Tailwind 4.3.5. Hai chỗ không lấy bản mới nhất:
  - TypeScript ghim 6.0.3: typescript-eslint 8.72 chỉ hỗ trợ TypeScript `<6.1`, chưa có TypeScript 7.
  - ESLint ghim 9.39.5: ESLint 10 không hỗ trợ Node 23 (máy dev đang chạy Node 23.7, bản đã hết hỗ trợ). npm vẫn cảnh báo `EBADENGINE` cho một package con.
- **Package cài theo lúc dùng:** Zustand, React Hook Form, Zod, `@hookform/resolvers`, `uuid`, Dexie, `vite-plugin-pwa` chưa cài, sẽ thêm khi màn hình đầu tiên dùng tới.
- **`request()` trả nguyên `HTTPResponseData`** để đọc được `message` và cảnh báo. Hàm trong `api/<mảng>.ts` trả `dataResponse`. Lỗi mạng và hết thời gian chờ là `ApiError` có `status = 0`.
- **TanStack Query:**
  - Chỉ thử lại lỗi mạng và lỗi 5xx (tối đa 2 lần); 4xx không thử lại.
  - Mutation không tự thử lại, để tránh ghi hai lần.
  - `staleTime` mặc định 30 s; thông tin cửa hàng 5 phút.
- **Chưa có:** đăng nhập và gắn JWT (bước kế tiếp: `useAuthStore` + refresh trong `client.ts`), `RequireAuth`, `components/ui/`, PWA.
- **Kiểm tra:**
  - `npm run build` (tsc + vite) và `npm run lint` không lỗi.
  - Chạy API + `npm run dev`: `https://localhost:5173/api/stores` qua proxy trả dữ liệu cửa hàng thật.
  - Route lạ trả trang SPA.
  - Vite mở tại `https://<IP máy>:5173` cho điện thoại.
- **Việc người dùng tự làm:**
  - Nên cài Node 24 LTS, rồi xóa `node_modules` và chạy `npm install` lại.
  - Lần đầu mở trên điện thoại, trình duyệt cảnh báo chứng chỉ tự ký: chọn "Vẫn tiếp tục".

### 27/11/2026 – Thêm skill thiết kế Impeccable

Nhánh `feature/frontend-scaffold`. Người dùng đề xuất 3 skill. Sau khi đọc nguồn thì chỉ chọn Impeccable:
- Taste Skill dựng website giới thiệu (banner lớn, ảnh, font ngẫu nhiên), không hợp ứng dụng thao tác.
- Astryx (Meta) là thư viện component React, trái quy tắc "không thư viện component UI" và còn beta.

**Thư mục mới:** `.claude/skills/impeccable/` – skill dùng cho dự án, có version theo repo.

**File mới**

| Đường dẫn | Vai trò |
| --- | --- |
| `.claude/skills/impeccable/SKILL.md`, `reference/**/*.md` (50 file) | Chỉ dẫn của skill, chép nguyên từ commit `2fc5e14` |
| `.claude/skills/impeccable/LICENSE`, `NOTICE.md` | Giấy phép Apache-2.0 và ghi công bên thứ ba (bắt buộc khi phân phối lại) |
| `.claude/skills/impeccable/SOURCE.md` | Nguồn, commit, phần đã bỏ, cách cập nhật |

**File sửa**

| Đường dẫn | Thay đổi |
| --- | --- |
| `CLAUDE.md` | Mục "Thiết kế giao diện: skill Impeccable"; cây repo thêm `.claude/skills/impeccable/`, `PRODUCT.md`, `DESIGN.md` |
| `.gitignore` | Bỏ qua `.impeccable/` |

**Ghi chú**

- **Đã bỏ `scripts/`.** Launcher tải binary engine. Mã nguồn Rust của engine công khai, nhưng engine:
  - gọi `impeccable.style/api` (roll API, gọi cả khi đã tắt telemetry);
  - kiểm tra bản cập nhật;
  - gửi telemetry lựa chọn thiết kế theo mặc định;
  - có tính năng tài khoản / gói Pro.

  Không có dấu hiệu độc hại, nhưng FoMart không cần gọi dịch vụ ngoài. Skill tự hỗ trợ chạy không có engine (nhánh "Launcher unavailable").
- **Mất so với cài đầy đủ:** chụp màn hình tự động (`shot`), hook detector, `font-fetch`, `live`, `generate`.
- **Điểm vênh đã biết:** `craft-floor.md` coi font hệ thống làm font tiêu đề là lỗi. FoMart giữ font hệ thống (ngoại tuyến, tiếng Việt, tốc độ) và sẽ ghi rõ trong `DESIGN.md` ("The brief wins").
- **Chưa làm:** `PRODUCT.md` / `DESIGN.md`, làm cùng màn hình đăng nhập.

### 27/11/2026 – Logo và quy tắc icon

Nhánh `feature/frontend-scaffold`.

- **Bộ icon:** người dùng chọn Phosphor (cân nhắc cùng React Icons, Tabler, Lucide, Heroicons). Quy tắc ghi ở CLAUDE.md mục "Icon và logo": một bộ duy nhất, không tự vẽ icon. Package chưa cài, cài khi màn hình đầu tiên dùng tới.
- **Logo:** đưa 3 phương án trên canvas claude.ai "FoMart Logo" (A Mái hiên, B Mã vạch, C Lô hàng). Người dùng chọn **A**.

**File mới / sửa**

| Đường dẫn | Vai trò |
| --- | --- |
| `frontend/public/logo-mark.svg` | Biểu tượng dùng trong giao diện (ô vuông bo góc 24/96) |
| `frontend/public/favicon.svg` | Thay favicon tạm hình giỏ |
| `frontend/public/apple-touch-icon.png` | 180×180, vuông tràn viền (iOS tự bo góc) |
| `frontend/public/pwa-192.png`, `pwa-512.png` | Icon PWA "any" |
| `frontend/public/pwa-maskable-512.png` | Icon PWA "maskable": nền tràn viền, hình thu 75% vào vùng an toàn |
| `frontend/index.html` | Thêm `apple-touch-icon` |
| `frontend/src/app/AppLayout.tsx` | Thanh đầu trang dùng `logo-mark.svg` |
| `CLAUDE.md` | Mục "Icon và logo": Phosphor, logo đã chốt, danh sách file logo |

**Ghi chú**

- **Cách sinh PNG:** dùng `@resvg/resvg-js` 2.6.2 cài trong thư mục tạm, không thêm vào dependency của dự án. Đổi logo thì sinh lại từ SVG.
- **Hạn chế:**
  - Chữ "FoMart" chưa chuyển thành path. Hiện viết bằng font của app; logo chữ cố định cần chuyển chữ thành path, với Be Vietnam Pro (giấy phép OFL).
  - Manifest PWA chưa có (giai đoạn 6, `vite-plugin-pwa`); các PNG đã sẵn.

### 27/11/2026 – Thử quét mã bằng camera, chốt thư viện (ADR-0003)

Nhánh `feature/frontend-scan-spike`.

**Thư mục mới:** `frontend/src/features/sales/components/`, chứa component camera của trang thử.

**File mới**

| Đường dẫn | Vai trò |
| --- | --- |
| `docs/adr/0003-barcode-scanning.md` | Quyết định: bộ đọc, đèn/zoom, phản hồi, chuẩn hóa UPC-A, nguồn dự phòng, trình duyệt tối thiểu |
| `frontend/src/features/sales/pages/ScanTestPage.tsx` | Trang tạm `/thu-quet`: chọn bộ đọc, thẻ kết quả, đo thời gian quét và thời gian tra mã, chụp ảnh để đọc, nhập tay |
| `frontend/src/features/sales/components/ScanTestCamera.tsx` | Camera + vòng quét `BarcodeDetector`, lấy nét liên tục, zoom, đèn, khung chẩn đoán |
| `frontend/src/lib/barcodeDetector.ts` | zxing-cpp WASM (`.wasm` tự phục vụ), bộ đọc có sẵn, đọc mã từ ảnh |
| `frontend/src/lib/timing.ts` | Đo thời gian một việc bất đồng bộ |
| `frontend/src/api/auth.ts`, `api/catalog.ts` | `login()`, `getProductByBarcode()` và kiểu DTO |

**File sửa**

| Đường dẫn | Thay đổi |
| --- | --- |
| `frontend/src/api/client.ts` | Bỏ `AbortSignal.timeout`/`any` (điện thoại cũ không có), dùng `AbortController` + `setTimeout`; thêm `accessToken` tạm thời |
| `frontend/src/index.css` | Animation `scan-flash` (tôn trọng `prefers-reduced-motion`) |
| `frontend/src/app/router.tsx`, `SalePage.tsx` | Route và lối vào `/thu-quet` |
| `frontend/package.json` | Thêm `barcode-detector` 3.2.3. Đã thử rồi gỡ `@zxing/browser`, `@zxing/library`, `html5-qrcode` |
| `CLAUDE.md` | Dòng công nghệ, danh sách ADR, quy tắc trình duyệt tối thiểu |

**Ghi chú**

- **Diễn biến buổi thử trên điện thoại thật:**
  1. Lỗi `AbortSignal` làm hỏng mọi lệnh gọi API.
  2. Mã trên màn hình đọc được, chai Aquafina thì không, nên thêm khung chẩn đoán và nút chụp ảnh.
  3. Thêm bộ đọc zxing-cpp, zoom, đèn.
  4. Người dùng không biết đã quét được chưa, nên thêm viền chớp và thẻ kết quả.
  5. Bật thêm đèn thì quét tốt.
- **API không phải nguyên nhân chậm:** khoảng 6 ms gọi thẳng, 25 ms qua proxy (đo trên máy dev).
- **Bundle:** JS 131 KB gzip, cộng `zxing_reader.wasm` 414 KB gzip tách riêng. Lúc thử đủ 4 bộ đọc là 363 KB.
- **Hạn chế:**
  - Mới thử trên một điện thoại.
  - Chưa ghi loại máy và trình duyệt, chưa có số liệu so sánh từng bộ đọc.
  - Trang `/thu-quet` vẫn còn để đo trên máy khác.
- **Việc còn lại theo ADR-0003, làm cùng màn hình bán hàng:**
  - component quét chính thức, tải lười;
  - bộ nghe máy quét bàn phím;
  - chuẩn hóa UPC-A → EAN-13 ở backend và frontend.
