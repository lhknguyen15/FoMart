# Mảng Sales – ghi chú

Bán hàng, hủy hóa đơn. UC-05, UC-06 · BR-01, BR-03, BR-04, BR-05, BR-06, BR-07, BR-08 · NFR-05, NFR-06.

### 27/11/2026 – API bán hàng FEFO và hủy hóa đơn

Nhánh `feature/sales-checkout`. Đã chốt với người dùng: BR-18 (kệ thiếu mà kho sau còn thì tự tạo phiếu chuyển kho) để làm cùng module chuyển kho (UC-07). Bản này kệ thiếu thì trừ âm ở kệ và đánh dấu cần kiểm kê.

**Thư mục mới**

| Thư mục | Vì sao |
| --- | --- |
| `backend/FoMart.Application/DTO/Sales/`, `Services/Sales/`, `Api/Controllers/Sales/` | DTO, service, controller của mảng |

**File mới**

| Đường dẫn | Vai trò |
| --- | --- |
| `Domain/Enums/SaleStatus.cs`, `PaymentMethod.cs` | `pending_payment` / `paid` / `cancelled`; `cash` / `transfer` (trùng CHECK) |
| `Domain/Rules/FefoAllocator.cs` | Chia số lượng cần trừ vào các lô (`LotStock`, `LotAllocation`, `AllocationResult`): BR-01, BR-04, BR-05 |
| `Application/DTO/Sales/SaleModels.cs` | Request/response hóa đơn, dòng, lô đã trừ |
| `Application/DTO/Sales/SalesResponseMessageDTO.cs` | Thông báo và cảnh báo tiếng Việt |
| `Application/Interfaces/Repositories/ISaleRepository.cs` | Interface repository hóa đơn |
| `Application/Services/Sales/SaleService.cs` | Lưu hóa đơn, xác nhận chuyển khoản, hủy; trừ kho FEFO |
| `Infrastructure/Repositories/SaleRepository.cs` | Tìm hóa đơn, lấy hóa đơn kèm dòng |
| `Api/Controllers/Sales/SaleController.cs` | Endpoint `api/sales` |

**File sửa**

| Đường dẫn | Thay đổi |
| --- | --- |
| `Application/Interfaces/Repositories/IStockRepository.cs`, `Infrastructure/Repositories/StockRepository.cs` | Thêm phần trừ kho cho bán hàng (chi tiết ở `inventory.md`) |
| `Interfaces/UnitOfWork/IUnitOfWork.cs`, `UnitOfWork/UnitOfWork.cs` | Thêm `SaleRepository` |
| `Api/Program.cs` | Đăng ký `SaleService` |
| `CLAUDE.md` | Mục trạng thái |

**API**

| Endpoint | Ai gọi | Chức năng |
| --- | --- | --- |
| `GET /api/sales` | đã đăng nhập | Tìm theo mã, trạng thái, hình thức, người bán, khoảng ngày (giờ VN); phân trang |
| `GET /api/sales/{id}` | đã đăng nhập | Chi tiết: dòng, các lô đã trừ; giá vốn và lãi gộp chỉ chủ tiệm thấy |
| `POST /api/sales` | đã đăng nhập | Lưu hóa đơn (Id UUID v7 từ máy bán). Tiền mặt: trừ kho ngay. Chuyển khoản: chờ thanh toán |
| `POST /api/sales/{id}/confirm-payment` | đã đăng nhập | "Đã nhận" tiền chuyển khoản: thanh toán và trừ kho |
| `POST /api/sales/{id}/cancel` | ai cũng được với hóa đơn chờ; chủ tiệm với hóa đơn đã thanh toán | Bỏ hóa đơn chờ / hủy hóa đơn bằng `sale_cancel` |

**Ghi chú**

- **Trừ kho:** chạy trong một transaction, tại vị trí bán mặc định.
  1. Khóa dòng `products` (`UPDLOCK, ROWLOCK`, id tăng dần), để hai lượt bán cùng sản phẩm phải chờ nhau, kể cả khi kệ chưa có dòng tồn nào để khóa (không tạo trùng lô tạm).
  2. Đọc tồn theo lô tại kệ (khóa dòng tồn).
  3. `FefoAllocator` chia lô.
  4. Thêm dòng sổ `sale`; trigger trừ `stock`.
- **Thứ tự chia lô:**
  1. Lô còn hạn: hạn gần nhất trước, lô không hạn để cuối, rồi theo ngày nhận.
  2. Lô hết hạn: chỉ khi người bán xác nhận (`confirmExpired`). Nếu không thì 409 và không trừ gì (BR-05).
  3. Lô tạm còn tồn dương.
  4. Còn thiếu: trừ âm vào lô mới nhất tại kệ. Kệ chưa từng có lô thì trừ vào lô tạm của sản phẩm (tạo nếu chưa có, dùng lại cho các lần sau).
  5. Trường hợp 4 thì bật `needs_count` (BR-01).
- Hai dòng cùng sản phẩm (lon + thùng) chia trên cùng bảng tồn đang trừ dở, nên không trừ trùng một lô.
- **Giá vốn (BR-06):** `cost_total` = tổng (số lượng × giá vốn lô) của các lô bị trừ. Bằng NULL nếu có phần trừ vào lô tạm. Lãi gộp chỉ tính khi mọi dòng biết giá vốn.
  - Nhân viên không thấy giá vốn và lãi trên hóa đơn (BR-08), không phụ thuộc `staff_can_see_cost` (cài đặt đó chỉ áp cho giá nhập).
- **Không sửa giá (BR-07):**
  - Hàng đã khai báo lấy giá theo đơn vị. Máy bán gửi kèm giá đang hiện thì chỉ bị từ chối khi giá khác giá đơn vị.
  - `name_snapshot` = "tên sản phẩm (đơn vị)".
- **Hàng chưa khai báo:** gửi `name` + `unitPrice`, không có `unitId`. Không trừ kho, không có giá vốn.
- **Không ghi trùng (NFR-05):**
  - Id UUID v7 do máy bán sinh (từ chối version khác).
  - Cùng người gửi lại cùng Id: trả hóa đơn đã ghi (200). Người khác dùng Id đó: 409.
  - `createdAt` là giờ máy bán, không được sớm hơn giờ server quá 5 phút về phía tương lai.
- **Hủy:**
  - Hóa đơn chờ thanh toán: ai cũng bỏ được, không đụng kho.
  - Hóa đơn đã thanh toán: chỉ chủ tiệm. Thêm `sale_cancel` cộng lại đúng từng lô và vị trí đã trừ (kể cả phần âm). Hóa đơn gốc giữ nguyên (BR-03).
- **Lỗi phát hiện khi test (đều ở script test PowerShell 5.1):**
  1. Biến `$pId` trùng biến hệ thống chỉ đọc `$PID`, vì tên biến không phân biệt hoa thường.
  2. `.Count` trên một `PSCustomObject` đơn lẻ trả rỗng: phải bọc `@(...)`.
- **Hạn chế:**
  - Chưa có BR-18 (tự chuyển kho).
  - Chưa có tạo nhanh sản phẩm khi quét mã mới.
  - Chưa có màn hình VietQR; frontend tạo mã từ `stores.bank_*` + mã hóa đơn.
  - Hai request cùng Id gửi đồng thời thì request thứ hai nhận 409 (trùng khóa) thay vì 200.
  - Nhân viên xem được mọi hóa đơn trong danh sách. Báo cáo "doanh thu ca của mình" thuộc mảng Report.
- **Test:** 54 kịch bản HTTP trên database tạm. Kệ được nhập 2 lô cola (hạn +60 và +30 ngày) và 1 lô mì đã hết hạn, rồi chạy:
  - FEFO;
  - giá vốn và lãi;
  - gửi lại cùng Id;
  - bán vượt tồn;
  - lô tạm;
  - lô hết hạn;
  - hàng chưa khai báo;
  - chuyển khoản;
  - hủy;
  - 6 lượt bán song song.

  Đối soát sổ kho rỗng ở cuối. Tất cả qua.
  - Swagger: login → `POST /api/sales` `{ "id": "<uuid v7>", "lines": [{ "unitId": 1, "qty": 2 }] }`. Id v7 tạo bằng `Guid.CreateVersion7()` hoặc trang uuidgenerator; bản `uuid` v7 của frontend sẽ tự sinh.

### 28/11/2026 – Màn hình bán hàng (frontend)

Nhánh `feature/sales-pos-screen`. UC-05, UC-06 (bỏ hóa đơn chờ), SC-03 · BR-01, BR-05, BR-07 · NFR-02, NFR-03, NFR-04, NFR-05, NFR-07 · ADR-0003.

**Thư mục mới:** `frontend/src/features/sales/{hooks,schemas}/`, `frontend/src/features/catalog/hooks/`

**File mới**

| Đường dẫn | Vai trò |
| --- | --- |
| `frontend/src/stores/useCartStore.ts` | Giỏ hàng (Zustand + `persist`): `saleId` UUID v7 sinh một lần mỗi giỏ, thêm hàng đã khai báo / chưa khai báo, đổi số lượng, `reset` |
| `frontend/src/api/sales.ts` | Kiểu DTO hóa đơn + `createSale`, `confirmSalePayment`, `cancelSale`, `searchSales` |
| `frontend/src/features/sales/hooks/useSales.ts` | `saleKeys`, `usePendingSales` (làm mới mỗi 30 s), `useCreateSale` / `useConfirmSalePayment` / `useCancelSale` (xong thì làm mới `sales`, `products`, `stock`) |
| `frontend/src/features/catalog/hooks/useProducts.ts` | `productKeys`, `useProductSearch` (từ 2 ký tự, `keepPreviousData`), `useBarcodeLookup` và `useProductLoader` (gọi `fetchQuery` trong handler, có cache) |
| `features/sales/components/BarcodeScanner.tsx` | Camera quét liên tục bằng zxing-cpp; tải lười (`React.lazy`); zoom, đèn, lấy nét liên tục, viền chớp; cùng mã trong 1,5 s chỉ tính một lần |
| `features/sales/components/KeyboardScanner.tsx` | Máy quét cầm tay: ký tự cách nhau < 50 ms rồi Enter; bỏ qua khi đang gõ trong ô nhập |
| `features/sales/components/ProductSearch.tsx` | Tìm theo tên không dấu; chạm để thêm; nhiều đơn vị thì chọn đơn vị; gõ mã số rồi Enter thì xử lý như quét |
| `features/sales/components/CartList.tsx` | Giỏ: +/− (− ở 1 là bỏ món), thành tiền; trạng thái rỗng |
| `features/sales/components/CheckoutBar.tsx` | Tổng tiền 2.5rem; gợi ý tiền khách đưa và tiền thối; "Xong · Tiền mặt" một chạm; "Chuyển khoản" |
| `features/sales/components/PendingSales.tsx` | Hóa đơn chờ chuyển khoản: "Đã nhận tiền" (hỏi lại khi chỉ còn lô hết hạn), "Bỏ" |
| `features/sales/components/UndeclaredItemForm.tsx`, `schemas/undeclaredItemSchema.ts` | Hàng chưa khai báo: tên + giá nhập tay (React Hook Form + Zod) |
| `frontend/src/lib/id.ts`, `lib/barcode.ts`, `lib/feedback.ts` | UUID v7; chuẩn hóa UPC-A → EAN-13; bíp + rung (mở khóa âm thanh trong lần chạm) |
| `backend/FoMart.Domain/Rules/BarcodeRules.cs` | Chuẩn hóa mã ở backend (xem `catalog.md`) |

**File sửa**

| Đường dẫn | Thay đổi |
| --- | --- |
| `frontend/src/features/sales/pages/SalePage.tsx` | Màn hình bán hàng: bốn nguồn mã chung một `handleCode`; thẻ kết quả quét; thanh thanh toán dính đáy trên điện thoại, cột phải trên máy tính; xác nhận lô hết hạn; nhớ lựa chọn bật camera |
| `frontend/src/api/catalog.ts` | Thêm `searchProducts`, `getProduct`, kiểu `ProductResponse`, `ProductListItemResponse` |
| `frontend/src/app/router.tsx` | Bỏ route `/thu-quet` |
| `frontend/src/app/AppLayout.tsx` | Khung rộng tới `max-w-5xl` (máy tính chia hai cột) |
| `frontend/package.json` | `uuid` 14.0.3 |

**File xóa:** `ScanTestPage.tsx`, `ScanTestCamera.tsx`, `lib/timing.ts`. Đây là trang thử quét, bị thay bằng `BarcodeScanner` (đúng phần "Hệ quả" của ADR-0003).

**Ghi chú**
- **Quyết định:**
  - **Tiền mặt một chạm:** bấm "Xong" là lưu (UC-05: ≤ 3 lần chạm). Chọn tiền khách đưa chỉ để xem tiền thối.
  - **Chuyển khoản:** giỏ thành hóa đơn chờ, giỏ trống để bán tiếp khách khác; xác nhận sau ở "Chờ chuyển khoản".
  - **Lỗi mạng:** giữ giỏ và `saleId`, nên bấm lại an toàn.
- **Bundle:** chunk chính 172 KB gzip. `BarcodeScanner` (2,5 KB), `barcodeDetector` (15 KB) và `.wasm` (414 KB) tải riêng khi bật camera hoặc chụp ảnh.
- **Lỗi phát hiện:**
  1. Một lệnh PowerShell (`Get-Content -Raw` rồi `Set-Content`) đọc `AppLayout.tsx` bằng ANSI làm hỏng chữ tiếng Việt; lint phát hiện, đã viết lại file. Bài học: chỉ sửa file bằng Edit/Write.
  2. Script test truyền tiếng Việt qua `curl -d` bị sai mã hóa (400). Lỗi của script; gửi body từ file UTF-8 thì 200.
- **Impeccable (chấm trên code):** sửa "phải bấm bật camera mỗi lần" thành nhớ lựa chọn trên máy (UC-05 bước 1).
- **Hạn chế, làm tiếp:**
  - Chưa vẽ mã VietQR: cần API trả thông tin ngân hàng cho người đã đăng nhập (`GET /api/stores` công khai chỉ trả `hasBankAccount`) + thư viện tạo QR.
  - Chưa có tự khóa (NFR-09), hàng đợi ngoại tuyến (NFR-05, giai đoạn 6), nút bấm nhanh hàng không mã (`quick_button_order`).
  - Chưa xem giao diện thật bằng mắt.
- **Test:** build + lint sạch. Chạy các request y như frontend gửi qua proxy Vite trên database tạm `FoMart_PosTest`:
  - mã UPC-A 12 số và 13 số cùng ra một đơn vị; mã trùng ở dạng khác bị chặn 409;
  - tìm "mi tom" không dấu; chi tiết sản phẩm có 3 đơn vị;
  - bán tiền mặt có hàng chưa khai báo: tổng 26.000đ, thối 24.000đ, có cảnh báo;
  - gửi lại cùng id được 200, không ghi trùng;
  - chuyển khoản chờ → có trong danh sách chờ → "Đã nhận"; "Bỏ" chuyển sang cancelled.

### 28/11/2026 – Mã VietQR cho hóa đơn chuyển khoản

Nhánh `feature/sales-pos-screen` (cùng PR với màn hình bán hàng). UC-05 bước 5, quyết định số 4 (khách chuyển khoản, nhân viên tự xác nhận, không tích hợp ngân hàng), NFR-09.

**File mới**

| Đường dẫn | Vai trò |
| --- | --- |
| `frontend/src/lib/vietqr.ts` | Dựng nội dung VietQR theo EMVCo/NAPAS 247: trường 38 (`A000000727` + BIN + số tài khoản + `QRIBFTTA`), 53 = 704, 54 = số tiền, 62/08 = nội dung (mã hóa đơn, bỏ dấu, ≤ 25 ký tự), 63 = CRC-16/CCITT-FALSE; bảng tên ngân hàng theo BIN |
| `frontend/src/features/sales/components/VietQrPanel.tsx` | Mã QR (SVG vẽ từ ma trận của `qrcode`), số tiền, ngân hàng, số tài khoản, chủ tài khoản, nội dung; báo khi tiệm chưa nhập tài khoản |

**File sửa**

| Đường dẫn | Thay đổi |
| --- | --- |
| `backend/FoMart.Application/DTO/Store/StoreModels.cs` | Thêm `StorePaymentResponse(BankBin, AccountNo, AccountName)` |
| `backend/FoMart.Application/DTO/Store/StoreResponseMessageDTO.cs` | Thêm `GetPaymentSuccess`, `PaymentNotConfigured` |
| `backend/FoMart.Application/Services/Store/StoreService.cs` | `GetPaymentAsync`: 404 khi thiếu BIN / số tài khoản / tên chủ tài khoản |
| `backend/FoMart.Api/Controllers/Store/StoreController.cs` | `GET /api/stores/payment`, cần đăng nhập (cả nhân viên) |
| `frontend/src/api/store.ts` | `getStorePayment`, kiểu `StorePaymentResponse` |
| `frontend/src/features/store/hooks/useStore.ts` | `storeKeys.payment()`, `useStorePayment` (tươi 5 phút) |
| `frontend/src/features/sales/components/PendingSales.tsx` | Nút "Mã QR" mở/đóng từng hóa đơn; hóa đơn vừa tạo ở máy này mở sẵn; `VietQrPanel` tải lười |
| `frontend/src/features/sales/pages/SalePage.tsx` | Truyền `focusId` khi hóa đơn vừa tạo là chờ chuyển khoản; nhắc "Khách quét mã QR bên dưới" |
| `frontend/package.json` | `qrcode` 1.5.4, `@types/qrcode` 1.5.6 (dev) |
| `CLAUDE.md` | Thêm `vietqr.ts` vào danh sách `lib/`; cập nhật trạng thái |

**Ghi chú**
- **Quyết định:**
  - Số tài khoản không nằm trong `GET /api/stores`, vì endpoint đó công khai cho màn hình đăng nhập. Endpoint riêng `/api/stores/payment` thì bắt buộc đăng nhập.
  - Tự dựng nội dung VietQR ngay trên máy thay vì gọi ảnh từ dịch vụ ngoài (img.vietqr.io). Lý do: chạy được khi ngoại tuyến (giai đoạn 6) và không gửi số tiền, số tài khoản ra bên thứ ba.
  - Nội dung chuyển khoản là mã hóa đơn, để người bán đối chiếu khi tiền về.
  - Vẽ QR bằng `QRCode.create` (chạy đồng bộ) + `useMemo` rồi render SVG: không cần effect, không cần state ảnh, và mã nét ở mọi cỡ.
- **Bundle:** `VietQrPanel` + `qrcode` tách riêng thành chunk 10 KB gzip, chỉ tải khi có hóa đơn chờ chuyển khoản. Chunk chính giữ 172 KB gzip.
- **Lỗi phát hiện:** khi gọi `sqlcmd` qua `docker exec` trong Git Bash, đường dẫn `/opt/...` bị đổi thành `C:/Program Files/Git/opt/...`. Cách sửa: đặt `MSYS_NO_PATHCONV=1`.
- **Hạn chế:**
  - Chưa có màn hình để chủ tiệm nhập tài khoản ngân hàng; hiện chỉ nhập trong DB / seed (để làm cùng cài đặt cửa hàng).
  - Bảng tên ngân hàng mới có 13 ngân hàng phổ biến; BIN khác thì hiện "Ngân hàng (BIN …)".
  - Chưa thử quét QR bằng app ngân hàng thật.
- **Test:**
  - CRC-16 cho chuỗi `123456789` ra `29B1`, đúng giá trị kiểm tra chuẩn.
  - Payload mẫu (970436, 26.000đ, HD00000012) tự kiểm CRC đúng. Giải ngược mã QR bằng zxing-wasm ra đúng payload.
  - API: không token → 401; có token → 200 (dữ liệu seed). Xóa tạm `bank_bin` → 404 kèm thông báo, rồi khôi phục.
  - `npm run build` + `npm run lint` sạch; backend build 0 cảnh báo.

### 28/11/2026 – Màn hình Hóa đơn (SC-04, UC-06)

Nhánh `feature/sales-invoice-list`. UC-05, UC-06, BR-03, BR-04, BR-07, BR-08. Backend không đổi, dùng `GET /api/sales`, `GET /api/sales/{id}`, `POST /api/sales/{id}/cancel`.

**File mới**

| Đường dẫn | Vai trò |
| --- | --- |
| `features/sales/pages/SaleListPage.tsx` | `/hoa-don`: lọc theo ngày (mặc định hôm nay, giờ VN), trạng thái, hình thức, mã hóa đơn (gõ mã thì tìm mọi ngày); "Xem thêm" theo trang |
| `features/sales/pages/SaleDetailPage.tsx` | `/hoa-don/:id`: tổng, hình thức, tiền khách đưa / thối, người bán, các dòng (giá lúc bán, lô đã trừ, HSD), giá vốn + lãi cho người được xem; chờ CK thì hiện mã QR + "Đã nhận tiền"; chủ tiệm hủy hóa đơn đã thanh toán |
| `features/sales/components/CancelSaleForm.tsx`, `schemas/cancelSaleSchema.ts` | Hủy hóa đơn kèm lý do (1–300 ký tự) |
| `features/sales/components/SaleStatusBadge.tsx` | Nhãn trạng thái có chữ, không chỉ màu |

**File sửa**

| Đường dẫn | Thay đổi |
| --- | --- |
| `frontend/src/api/sales.ts` | `getSale`; `SaleSearchParams` thêm `keyword`, `from`, `to` |
| `features/sales/hooks/useSales.ts` | `saleKeys.history`, `saleKeys.detail`; `useSaleHistory` (`useInfiniteQuery`), `useSale` |
| `features/sales/components/PendingSales.tsx` | Prop `onlyId`: trang chi tiết chỉ hiện đúng hóa đơn đang xem |
| `features/sales/pages/SalePage.tsx` | Kết quả vừa bán có link "Xem hóa đơn" |
| `frontend/src/lib/format.ts` | `todayInVietnam()` (yyyy-mm-dd theo giờ VN), `formatPaymentMethod()` |
| `frontend/src/app/router.tsx`, `AppLayout.tsx` | Route `/hoa-don`, `/hoa-don/:id` tải lười; tab "Hóa đơn" |
| `CLAUDE.md` | Trạng thái |

**Ghi chú**

- **Quyết định:**
  - **"Xem thêm" thay cho phân trang số:** bấm một chạm trên điện thoại. `useInfiniteQuery` giữ các trang đã tải; đổi bộ lọc thì bắt đầu lại từ trang 1.
  - **Gõ mã thì tìm mọi ngày:** khách quay lại hỏi hóa đơn hôm trước thường đọc mã in trên hóa đơn.
  - **Mọi mutation bán hàng làm mới `['sales']`**, nên danh sách và chi tiết tự cập nhật sau khi hủy / xác nhận.
  - **Chưa tính được lãi:** chủ tiệm thấy dòng giải thích (lô tạm / hàng chưa khai báo) thay vì ô trống.
- **Lỗi phát hiện:**
  1. `react-refresh/only-export-components`: file component không được export hằng số; nhãn hình thức thanh toán chuyển sang `lib/format.ts`.
  2. Một lệnh `sed` thay `paymentText[...]` không khớp vì dấu chấm trong `sale.paymentMethod`; tsc báo lỗi, đã sửa bằng Edit.
- **Hạn chế:**
  - Chưa lọc theo nhân viên (`GET /api/users` chỉ cho chủ tiệm; làm cùng báo cáo).
  - Chưa có tổng doanh thu theo ngày trên màn hình (thuộc báo cáo UC-10).
  - Trang chi tiết của hóa đơn chờ CK chỉ hiện QR nếu hóa đơn nằm trong 20 hóa đơn chờ mới nhất.
- **Test:** `npm run build` + `npm run lint` sạch. Kịch bản HTTP qua proxy Vite trên database tạm `FoMart_SaleTest` (đã xóa): 20/22 qua.
  - Hai kịch bản không qua là kỳ vọng sai của script, đúng nghiệp vụ:
    - hóa đơn chuyển khoản chưa trừ kho trước khi "Đã nhận";
    - hàng seed nằm ở Kho sau nên bán trừ vào lô tạm, giá vốn null (BR-01, BR-18 để sau).
  - Đã kiểm: lọc hôm nay / hôm qua / chờ CK / tiền mặt / đã hủy; hai trang không trùng nhau, mới nhất trước; tìm theo mã (cả chữ thường); chi tiết có lô đã trừ; nhân viên không thấy giá vốn; id không có → 404.
  - Hủy: nhân viên hủy hóa đơn đã thanh toán 403; chủ tiệm hủy → tồn cộng lại đúng 2 lon; hủy lần 2 → 409; lý do trống → 400.
  - Xác nhận chuyển khoản từ trang chi tiết; đối soát sổ kho rỗng.

### 28/11/2026 – Giá vốn / lãi làm tròn về đồng, lô hiện bằng số lô + HSD

Nhánh `fix/receipt-location-and-codes`, chi tiết ở `purchasing.md` cùng ngày. `SaleResponse.CostTotal`, `GrossProfit`, `SaleLineResponse.CostTotal` đổi sang `long?` (đồng, `Domain/Rules/MoneyRules.cs`, BR-11); chi tiết hóa đơn hiện "lô FX01 HSD 30/06/2027" thay cho id lô nội bộ.
