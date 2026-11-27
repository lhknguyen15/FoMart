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
