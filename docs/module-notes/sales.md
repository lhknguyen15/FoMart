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
