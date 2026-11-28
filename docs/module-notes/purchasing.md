# Mảng Purchasing – ghi chú

Đặt hàng, nhập hàng. UC-03, UC-04 · BR-02, BR-03, BR-08, BR-09, BR-11.

### 27/11/2026 – API phiếu nhập (UC-04)

Nhánh `feature/purchasing-goods-receipt`. Phiếu đặt hàng (UC-03) chưa làm: phiếu nhập tạo được không cần phiếu đặt, `purchase_order_id` luôn NULL.

**Thư mục mới**

| Thư mục | Vì sao |
| --- | --- |
| `backend/FoMart.Application/DTO/Purchasing/`, `Services/Purchasing/`, `Api/Controllers/Purchasing/` | DTO, service, controller của mảng |

**File mới**

| Đường dẫn | Vai trò |
| --- | --- |
| `Domain/Enums/GoodsReceiptStatus.cs` | `draft` / `completed` / `cancelled` (trùng CHECK) |
| `Domain/Rules/ExpiryRules.cs` | Mức cảnh báo hạn dùng (đã hết hạn / còn dưới N ngày), nhận "hôm nay" làm tham số để test được |
| `Application/DTO/Purchasing/GoodsReceiptModels.cs` | Request/response phiếu nhập, dòng nhập, giá lần trước |
| `Application/DTO/Purchasing/PurchasingResponseMessageDTO.cs` | Thông báo tiếng Việt |
| `Application/Interfaces/Repositories/IGoodsReceiptRepository.cs` | Interface repository phiếu nhập |
| `Application/Services/Purchasing/GoodsReceiptService.cs` | Nháp → hoàn tất → hủy |
| `Infrastructure/Repositories/GoodsReceiptRepository.cs` | Tìm phiếu, lấy phiếu kèm dòng, giá nhập lần trước |
| `Api/Controllers/Purchasing/GoodsReceiptController.cs` | Endpoint `api/goods-receipts` |

Dùng chung với mảng Inventory (ghi ở `inventory.md`): `Domain/Enums/DocType.cs`, `LotKind.cs`, `Domain/Rules/UnitConverter.cs`, `StockRepository`.

**File sửa**

| Đường dẫn | Thay đổi |
| --- | --- |
| `Application/Interfaces/Repositories/IProductRepository.cs`, `Infrastructure/Repositories/ProductRepository.cs` | `GetUnitsWithProductAsync`: lấy đơn vị kèm sản phẩm theo danh sách id (AsNoTracking) |
| `Application/Interfaces/Repositories/ILocationRepository.cs`, `Infrastructure/Repositories/LocationRepository.cs` | `GetDefaultReceivingAsync`: kho sau đang dùng, không có thì vị trí bán mặc định |
| `Application/Interfaces/Repositories/IRepositoryBase.cs` | Comment `Delete`: được xóa chứng từ còn nháp |
| `Interfaces/UnitOfWork/IUnitOfWork.cs`, `UnitOfWork/UnitOfWork.cs` | Thêm `GoodsReceiptRepository`, `StockRepository` |
| `Api/Program.cs` | Đăng ký `GoodsReceiptService`, `StockService` |
| `CLAUDE.md` | Mục trạng thái |

**API**

| Endpoint | Ai gọi | Chức năng |
| --- | --- | --- |
| `GET /api/goods-receipts` | đã đăng nhập | Tìm theo mã, trạng thái, nhà cung cấp, khoảng ngày tạo (giờ VN); phân trang |
| `GET /api/goods-receipts/{id}` | đã đăng nhập | Chi tiết + dòng: base qty, giá vốn/đơn vị cơ sở, cảnh báo hạn dùng |
| `GET /api/goods-receipts/last-price?unitId=` | người thấy giá | Giá nhập lần trước (bỏ phiếu đã hủy) |
| `POST /api/goods-receipts` | đã đăng nhập | Tạo nháp, có thể kèm luôn các dòng; bỏ trống vị trí = kho sau |
| `PUT /api/goods-receipts/{id}`, `DELETE /api/goods-receipts/{id}` | đã đăng nhập | Sửa đầu phiếu / xóa phiếu nháp |
| `POST/PUT/DELETE /api/goods-receipts/{id}/lines[/{lineId}]` | đã đăng nhập | Thêm/sửa/xóa dòng của phiếu nháp |
| `POST /api/goods-receipts/{id}/complete` | đã đăng nhập | Hoàn tất: tạo lô, ghi sổ kho, cộng tồn |
| `POST /api/goods-receipts/{id}/cancel` | chủ tiệm | Hủy bằng chứng từ đảo ngược `receipt_cancel` |

**Ghi chú**

- **Hoàn tất:** chạy trong một transaction (`BeginWriteAsync`).
  - Mỗi dòng tạo một lô `kind = receipt` với giá vốn = giá nhập ÷ hệ số, làm tròn 4 chữ số (BR-11).
  - Mỗi dòng cũng thêm một dòng sổ `receipt` với `qty_change = qty × factor`; trigger tự cộng vào `stock` (BR-02).
  - Hai máy cùng bấm hoàn tất thì `row_version` của `goods_receipts` chặn máy thứ hai, máy đó trả 409 và rollback.
- **Hủy:** chỉ khi các lô của phiếu chưa có biến động nào khác `receipt` (chưa bán, chuyển, hủy, kiểm kê).
  - Trước khi kiểm, khóa dòng tồn của các lô bằng `UPDLOCK, ROWLOCK` để không bán chen vào giữa.
  - Phiếu gốc và dòng sổ cũ giữ nguyên, chỉ thêm dòng sổ âm (BR-03).
- **Ẩn giá (BR-08):** nhân viên khi `stores.staff_can_see_cost = 0` sẽ:
  - không thấy giá, thành tiền, giá vốn trên phản hồi (null, `pricesHidden = true`);
  - nhận 403 nếu gửi giá lên;
  - sửa dòng thì giá chủ tiệm đã điền được giữ nguyên (đổi sang đơn vị khác thì giá bị xóa vì không còn đúng);
  - không hoàn tất được khi còn dòng thiếu giá (409), chủ tiệm điền giá trước.
- **Hạn dùng:**
  - Sản phẩm có quản lý hạn dùng bắt buộc nhập hạn dùng (400). Lúc hoàn tất kiểm lại, vì chủ tiệm có thể vừa bật quản lý hạn dùng.
  - Sản phẩm không quản lý thì bỏ qua hạn dùng gửi lên.
  - Hạn đã qua hoặc còn dưới `stores.expiry_warn_days` ngày: cảnh báo trên dòng (`expiryWarning`), hoàn tất trả 409 cho tới khi gửi `confirmExpiryWarning: true`. Ngày tính theo giờ Việt Nam.
- **Xóa:** phiếu nháp và dòng nháp được xóa vì chưa đổi tồn. Schema không cho trạng thái `cancelled` mà không có `completed_by`, nên "bỏ nháp" là xóa.
- **Từ chối dòng nhập:** đơn vị đã ngừng dùng và sản phẩm `inactive` (400). Sản phẩm `incomplete` (tạo nhanh) vẫn nhận.
- **Lỗi phát hiện khi test:**
  1. PowerShell 5.1 đọc sai script test không có BOM: lưu UTF-8 có BOM (bài học cũ).
  2. `sqlcmd` mặc định `QUOTED_IDENTIFIER OFF` nên INSERT vào bảng có filtered index bị lỗi 1934: thêm `-I` khi chạy SQL tay.
- **Hạn chế:**
  - Chưa nhận theo phiếu đặt (UC-03, so số đặt/nhận).
  - Chưa tạo nhanh sản phẩm khi quét mã mới (UC-01, làm cùng màn hình).
  - Ai đăng nhập cũng sửa/xóa được phiếu nháp của người khác.
  - Kịch bản "hai máy hoàn tất cùng lúc" khi chạy thử thì hai request đến lệch nhau khoảng 20 ms nên đi tuần tự. Nhánh `row_version` đúng theo thiết kế nhưng chưa được ép xảy ra thật; cần integration test khi có `FoMart.Tests`.
- **Test:** 64 kịch bản HTTP trên database tạm có seed (owner + staff), tất cả qua. Trong đó có kiểm đối soát sổ kho rỗng sau mỗi bước và mô phỏng chuyển kho bằng SQL để thử chặn hủy.
  - Swagger: login owner → `POST /api/goods-receipts` `{ "lines": [{ "unitId": 3, "qty": 2, "unitPrice": 190000, "expiryDate": "2027-05-01" }] }` → `POST /api/goods-receipts/{id}/complete` `{}` → `GET /api/stock?productId=1`.

### 28/11/2026 – Màn hình nhập hàng (UC-04, SC-10)

Nhánh `feature/purchasing-receipt-screen`. UC-04, UC-01 (nhánh tạo nhanh), BR-03, BR-08, BR-09. Backend không đổi, dùng API phiếu nhập ở mục 27/11.

**Thư mục mới**

| Thư mục | Vì sao |
| --- | --- |
| `frontend/src/features/purchasing/` (`pages/`, `components/`, `hooks/`, `schemas/`) | Mảng Purchasing phía frontend |
| `frontend/src/features/catalog/components/`, `features/catalog/schemas/` | Component và schema của mảng Catalog, dùng ở cả bán và nhập |
| `frontend/src/components/ui/` | Component dùng chung không gọi API (khối quét mã) |

**File mới**

| Đường dẫn | Vai trò |
| --- | --- |
| `frontend/src/api/purchasing.ts` | Kiểu DTO phiếu nhập + hàm gọi `api/goods-receipts` (tìm, chi tiết, giá lần trước, tạo/sửa/xóa, dòng, hoàn tất, hủy) |
| `features/purchasing/hooks/useGoodsReceipts.ts` | `receiptKeys`; query danh sách / chi tiết / giá lần trước; mutation ghi thẳng phiếu trả về vào cache chi tiết; xóa dòng cập nhật lạc quan; hoàn tất / hủy làm mới tồn và sản phẩm |
| `features/purchasing/pages/ReceiptListPage.tsx` | `/nhap-hang`: phiếu nháp đang làm dở, phiếu gần đây, nút "Phiếu nhập mới" |
| `features/purchasing/pages/ReceiptPage.tsx` | `/nhap-hang/:id`: nháp thì `DraftReceipt`, còn lại `ReceiptSummary`; 404 / id sai báo rõ |
| `features/purchasing/components/DraftReceipt.tsx` | Phiếu nháp: chọn nhà cung cấp, quét / tìm hàng, form dòng, danh sách dòng, thanh hoàn tất (xác nhận cảnh báo hạn dùng), xóa phiếu nháp |
| `features/purchasing/components/ReceiptLineForm.tsx` | Một dòng: đơn vị, số lượng, giá nhập (gợi ý "Dùng giá này" theo giá lần trước), hạn dùng + số lô, quy ra đơn vị cơ sở, thành tiền tạm |
| `features/purchasing/components/ReceiptLineList.tsx` | Danh sách dòng: số lượng → đơn vị cơ sở, giá, HSD, cảnh báo hạn dùng có chữ (không chỉ màu) |
| `features/purchasing/components/ReceiptSummary.tsx` | Phiếu đã hoàn tất / đã hủy: chỉ xem (BR-03) |
| `features/purchasing/components/CancelReceiptForm.tsx` | Chủ tiệm hủy phiếu kèm lý do |
| `features/purchasing/schemas/receiptLineSchema.ts` | Schema dòng nhập, dạng hàm theo `trackExpiry` (hạn dùng bắt buộc khi có quản lý); giá nhận "190.000" |
| `features/purchasing/schemas/cancelReceiptSchema.ts` | Lý do hủy 1–300 ký tự |
| `features/catalog/components/QuickProductForm.tsx`, `schemas/quickProductSchema.ts` | Tạo nhanh sản phẩm khi quét ra mã chưa có: tên, đơn vị, giá bán, có hạn dùng không (chỉ chủ tiệm) |
| `features/catalog/hooks/useSuppliers.ts` | `supplierKeys`, `useSuppliers` (tươi 5 phút) |
| `frontend/src/components/ui/ScanPanel.tsx` | Khối quét dùng chung: camera (tải lười, nhớ bật/tắt), chụp ảnh, máy quét bàn phím |

**File sửa / dời**

| Đường dẫn | Thay đổi |
| --- | --- |
| `features/sales/components/BarcodeScanner.tsx`, `KeyboardScanner.tsx` → `components/ui/` | Dời để bán và nhập dùng chung (nội dung giữ nguyên, sửa đường dẫn import) |
| `features/sales/components/ProductSearch.tsx` → `features/catalog/components/ProductPicker.tsx` | Tổng quát hóa: trả về `(product, unit)`, dòng phụ và cách chọn đơn vị mặc định do nơi dùng truyền vào (bán: đơn vị duy nhất; nhập: đơn vị nhập mặc định) |
| `features/sales/pages/SalePage.tsx` | Dùng `ScanPanel` + `ProductPicker`; bỏ code camera / chụp ảnh lặp lại |
| `frontend/src/api/catalog.ts` | `createProduct`, `searchSuppliers`, kiểu `CreateProductRequest`, `SupplierResponse` |
| `features/catalog/hooks/useProducts.ts` | `useCreateProduct` |
| `frontend/src/app/router.tsx` | Route `/nhap-hang`, `/nhap-hang/:id` tải lười (`lazy`) |
| `frontend/src/app/AppLayout.tsx` | Thanh chuyển màn hình "Bán hàng" / "Nhập hàng" (`NavLink`, gạch chân vàng = đang mở) |
| `CLAUDE.md` | Quy ước component dùng chung giữa các mảng, `components/ui` liệt kê khối quét, trạng thái |

**Ghi chú**

- **Quyết định:**
  - **Phiếu nháp nằm trên server**, không giữ trong Zustand như giỏ bán hàng. Nhập hàng thường kéo dài (đếm thùng, đối chiếu hóa đơn giấy), có thể đổi máy giữa chừng; nháp trên server thì máy nào cũng mở tiếp được.
  - **Mọi thao tác dòng trả lại cả phiếu**, nên hook ghi thẳng vào cache chi tiết bằng `setQueryData`, không tải lại. Danh sách thì chỉ đánh dấu cũ. Xóa dòng cập nhật lạc quan (dòng biến mất ngay, lỗi thì trả lại).
  - **Quét mã luôn mở form dòng mới**, không cộng dồn như bán hàng: mỗi lần giao có thể khác lô / hạn dùng / giá.
  - Camera còn thấy lại đúng mã đang nhập thì bỏ qua, không mở lại form. Cơ chế chống lặp của `BarcodeScanner` vẫn giữ: mã còn trong khung hình thì không phát lại.
  - **Đơn vị chọn sẵn** là đơn vị nhập mặc định (`is_default_purchase`, thường là thùng), đổi được ngay trên form.
  - **Tạo nhanh sản phẩm** chỉ hiện với chủ tiệm (API `POST /api/products` chỉ cho chủ tiệm). Nhân viên thấy hướng dẫn nhờ chủ tiệm khai báo.
  - **Thông báo 409** của backend có phần hướng dẫn kỹ thuật ("Gửi lại với ConfirmExpiryWarning = true…"); màn hình chỉ hiện phần đầu.
- **Bundle:** chunk chính 162 KB gzip (giảm từ 172 KB vì nhập hàng tách theo route); `ReceiptPage` 7 KB, `ReceiptListPage` 1,4 KB.
- **Lỗi phát hiện khi làm:**
  1. Hai lỗi TypeScript do `noUncheckedIndexedAccess` (`split(...)[0]`, `units[0]` có thể `undefined`): thêm giá trị dự phòng / kiểm tra.
  2. Tên trường trong `fieldErrors` đã là camelCase (`client.ts` đổi); lúc đầu đọc `Name` nên không gắn lỗi vào ô.
  3. Một lệnh `node -e` dài trong Git Bash hỏng do quoting `${}`; file không bị đổi, làm lại bằng Edit.
- **Hạn chế:**
  - Chưa đổi vị trí nhận trên giao diện (mặc định kho sau; API có hỗ trợ).
  - Chưa nhận theo phiếu đặt (UC-03).
  - Sản phẩm tạo nhanh có trạng thái `active`, chưa đánh dấu "cần hoàn thiện thông tin" như spec UC-01 (backend chưa có cờ này khi tạo).
  - Xóa dòng không hỏi lại.
  - Chưa xem giao diện thật bằng mắt trên điện thoại.
- **Test:** `npm run build` + `npm run lint` sạch. Kịch bản HTTP gửi đúng request của frontend qua proxy Vite, trên database tạm `FoMart_RcptTest` có seed (đã xóa sau khi test): 31/32 qua.
  - Kịch bản không qua là kỳ vọng sai của script: seed đã có phiếu PN00000001 nên đơn vị thùng có sẵn giá lần trước (200 chứ không phải 404).
  - Đã kiểm: tạo nháp `{}` → nhận vào Kho sau; chọn nhà cung cấp; quét mã thùng ra đúng đơn vị (24); thêm / sửa / xóa dòng, tổng tiền tính lại; thiếu hạn dùng 400; hạn còn 3 ngày → `near_expiry`, hoàn tất 409, xác nhận thì xong và có lô; giá lần trước 190.000đ; phiếu đã xong không thêm dòng (409).
  - Mã mới 404 → nhân viên tạo sản phẩm 403, chủ tiệm tạo được → quét lại ra đúng đơn vị.
  - Nhân viên: phiếu ẩn giá, gửi giá 403, xem giá lần trước 403, hoàn tất khi thiếu giá 409; chủ tiệm điền giá → nhân viên hoàn tất được.
  - Nhân viên hủy phiếu 403, chủ tiệm hủy được; xóa nháp → chi tiết 404; đối soát sổ kho rỗng.
  - Kiểm schema giá: "190.000", "190 000" hợp lệ; "19a" lỗi; thiếu hạn dùng khi có quản lý thì lỗi.
