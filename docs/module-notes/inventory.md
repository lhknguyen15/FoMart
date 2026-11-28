# Mảng Inventory – ghi chú

Lô, tồn, sổ kho, chuyển kho, xuất hủy, kiểm kê. UC-07, UC-08, UC-09 · BR-02, BR-04, BR-08, NFR-06, NFR-10.

### 27/11/2026 – Lõi kho và xem tồn (làm cùng nhập hàng)

Nhánh `feature/purchasing-goods-receipt`. Chưa có chuyển kho, xuất hủy, kiểm kê; phần này dựng lõi để các chứng từ đó dùng lại.

**Thư mục mới**

| Thư mục | Vì sao |
| --- | --- |
| `backend/FoMart.Application/DTO/Inventory/`, `Services/Inventory/`, `Api/Controllers/Inventory/` | DTO, service, controller của mảng |

**File mới**

| Đường dẫn | Vai trò |
| --- | --- |
| `Domain/Enums/DocType.cs` | 8 loại chứng từ trên sổ kho (trùng `CK_stock_moves_doc_type`) |
| `Domain/Enums/LotKind.cs` | `receipt` / `count_adjust` / `provisional` |
| `Domain/Rules/UnitConverter.cs` | Quy đổi ra đơn vị cơ sở, giá vốn/đơn vị cơ sở (BR-11), thành tiền; chặn tràn số |
| `Application/DTO/Inventory/StockModels.cs` | Tồn theo lô, thẻ kho, đối soát |
| `Application/DTO/Inventory/InventoryResponseMessageDTO.cs` | Thông báo tiếng Việt |
| `Application/Interfaces/Repositories/IStockRepository.cs` | Interface lõi kho |
| `Application/Services/Inventory/StockService.cs` | Xem tồn, thẻ kho, đối soát (chỉ đọc) |
| `Infrastructure/Repositories/StockRepository.cs` | Thêm lô / dòng sổ, khóa tồn theo lô, truy vấn tồn và thẻ kho |
| `Api/Controllers/Inventory/StockController.cs` | Endpoint `api/stock` |

**File sửa:** xem `purchasing.md` (cùng commit).

**API**

| Endpoint | Ai gọi | Chức năng |
| --- | --- | --- |
| `GET /api/stock?productId=&locationId=&includeZero=` | đã đăng nhập | Tồn theo lô và vị trí, xếp FEFO rồi FIFO |
| `GET /api/stock/moves?productId=&locationId=&from=&to=` | đã đăng nhập | Thẻ kho: chứng từ (mã PN/HD/CK/XH/KK), biến động, tồn sau biến động |
| `GET /api/stock/ledger-check` | chủ tiệm | Đối soát `stock` với tổng `stock_moves` (view `v_stock_ledger_mismatch`), rỗng là khớp |

**Ghi chú**

- **`StockRepository` là cửa duy nhất đổi tồn:**
  - Chỉ thêm `lots` và `stock_moves`; không có method ghi `stock` (trigger lo).
  - Sửa hay xóa dòng sổ bị trigger chặn (lỗi 50001, trả 409).
  - `LockLotsAsync` khóa `UPDLOCK, ROWLOCK` theo `lot_id` tăng dần để tránh deadlock, và bắt buộc chạy trong `BeginWriteAsync` (ném lỗi nếu không có transaction).
  - Trừ kho FEFO cho bán hàng (`FefoAllocator`) sẽ thêm vào đây ở mảng Sales.
- **Thẻ kho:** `Balance` = tổng `qty_change` của các dòng có id ≤ dòng hiện tại (cùng sản phẩm, cùng vị trí nếu lọc), nên vẫn đúng khi lọc theo ngày hoặc phân trang.
- **BR-08:** nhân viên bị ẩn giá thì `unitCost` trả null ở cả tồn theo lô và thẻ kho.
- **Hạn chế:**
  - Thẻ kho tính số dư bằng subquery theo từng dòng; đủ nhanh với quy mô tiệm, dữ liệu lớn thì chuyển sang `SUM() OVER`.
  - Chưa đặt `needs_count`, chưa có lô tạm (làm cùng bán hàng).
- **Test:** chạy chung 64 kịch bản của `purchasing.md`. Sau mỗi bước nhập, hủy, chuyển kho mô phỏng, đối soát sổ kho đều rỗng.

### 27/11/2026 – Trừ kho cho bán hàng

Nhánh `feature/sales-checkout` (cùng `sales.md`).

**File mới:** `Domain/Rules/FefoAllocator.cs` (thứ tự chia lô xem `sales.md`).

**File sửa**

| Đường dẫn | Thay đổi |
| --- | --- |
| `Application/Interfaces/Repositories/IStockRepository.cs`, `Infrastructure/Repositories/StockRepository.cs` | Thêm `LockProductsAsync`, `GetLotStocksAsync`, `GetProvisionalLotIdAsync`, `MarkNeedsCountAsync`, `GetSaleMovesAsync`; lớp nội bộ `LotStockRow` đọc kết quả SQL |

**Ghi chú**

- **Khóa:** khóa `products` trước rồi mới khóa `stock`, luôn theo id tăng dần. Hủy phiếu nhập chỉ khóa `stock` theo lô và không khóa `products`, nên hai chiều không chờ vòng lẫn nhau.
- **Kiểm tra transaction:** `LockLotsAsync`, `LockProductsAsync` và `GetLotStocksAsync` đều ném lỗi nếu không có transaction, vì khóa ngoài transaction thì nhả ngay.
- **`needs_count`:** bật bằng `ExecuteUpdate` (một câu UPDATE), không nạp entity sản phẩm. `row_version` của sản phẩm vẫn đổi, nên nếu chủ tiệm đang mở form sửa đúng sản phẩm đó thì lần lưu ấy nhận 409 "đã bị thay đổi", phải tải lại.

### 28/11/2026 – Màn hình Tồn kho và thẻ kho

Nhánh `feature/inventory-stock-screen`. SC-06/SC-07 (phần tồn), UC-10 (thẻ kho), BR-01, BR-04, BR-08; UC-08 (cảnh báo hạn dùng, phần xem).

**Thư mục mới**

| Thư mục | Vì sao |
| --- | --- |
| `frontend/src/features/inventory/` (`pages/`, `components/`, `hooks/`) | Mảng Inventory phía frontend |

**File mới**

| Đường dẫn | Vai trò |
| --- | --- |
| `frontend/src/api/inventory.ts` | Kiểu `StockLotResponse`, `StockMoveResponse`, `DocType`; `getProductStock`, `getStockMoves` |
| `features/inventory/hooks/useStock.ts` | `stockKeys` (gốc `['stock']`, bán / nhập / hủy đã làm mới key này), `useProductStock`, `useStockMoves` (`useInfiniteQuery`) |
| `features/inventory/pages/StockListPage.tsx` | `/ton-kho`: tìm không dấu / mã vạch, lọc Tất cả / Sắp hết / Tồn âm / Cần kiểm kê, bảng trên máy tính, thẻ trên điện thoại, "Xem thêm" |
| `features/inventory/pages/StockDetailPage.tsx` | `/ton-kho/:productId`: tổng tồn + nhãn, tồn theo vị trí, đơn vị bán; bảng lô đang còn (số lô, HSD, vị trí, ngày nhập, cảnh báo, giá vốn / đơn vị cơ sở nếu được xem); thẻ kho (thời gian, chứng từ, loại, vị trí, lô, +/−, tồn sau, người làm), bấm dòng bán / nhập để mở chứng từ |
| `features/inventory/components/StockBadges.tsx` | Nhãn chữ: Tồn âm, Sắp hết, Cần kiểm kê, Ngừng bán, Cần bổ sung |
| `frontend/src/components/ui/ChoiceChips.tsx` | Hàng nút chọn một, tách từ trang Hóa đơn để dùng chung |

**File sửa**

| Đường dẫn | Thay đổi |
| --- | --- |
| `backend/FoMart.Domain/Rules/ExpiryRules.cs` | `ToCode(level)` → `null` / `near_expiry` / `expired`, dùng chung cho phiếu nhập và tồn kho |
| `backend/FoMart.Application/DTO/Inventory/StockModels.cs` | `StockLotResponse.ExpiryWarning`; `StockMoveResponse.SaleId`, `ReceiptId` |
| `backend/FoMart.Application/Services/Inventory/StockService.cs` | Điền cảnh báo hạn dùng theo ngày VN + `stores.expiry_warn_days`; ẩn giá vốn như cũ (BR-08) |
| `backend/FoMart.Infrastructure/Repositories/StockRepository.cs` | Thẻ kho trả id hóa đơn / phiếu nhập gốc |
| `backend/FoMart.Application/DTO/Catalog/ProductModels.cs`, `CatalogResponseMessageDTO.cs` | `ProductSearchRequest.StockFilter` + `ProductStockFilter` (`needs_count`, `negative`, `low`); `ProductListItemResponse.MinStock`; thông báo bộ lọc sai |
| `backend/FoMart.Infrastructure/Repositories/ProductRepository.cs`, `Services/Catalog/ProductService.cs` | Lọc theo tổng tồn / `min_stock` / `needs_count`; 400 khi bộ lọc sai |
| `backend/FoMart.Application/Services/Purchasing/GoodsReceiptService.cs` | Dùng `ExpiryRules.ToCode` thay cho switch riêng |
| `frontend/src/api/catalog.ts`, `features/catalog/hooks/useProducts.ts` | `stockFilter`, `minStock`; `useProductBrowse` (`useInfiniteQuery`), `useProduct` |
| `frontend/src/components/ui/DataTable.tsx` | `renderCard`: dưới 1024px hiện danh sách thẻ, từ 1024px hiện bảng; `rowHref` được trả `undefined` (dòng không mở được) |
| `features/sales/pages/SaleListPage.tsx` | Dùng `ChoiceChips` |
| `frontend/src/app/AppNav.tsx`, `router.tsx` | Mục "Tồn kho" (icon Warehouse); route tải lười; dưới 640px tab chỉ có chữ để 4 tab vừa một hàng |
| `DESIGN.md`, `CLAUDE.md` | Quy tắc tab điện thoại; trạng thái |

**Ghi chú**

- **Quyết định:**
  - Danh sách tồn dựa trên `GET /api/products` (đã có tổng tồn, `needs_count`) thêm bộ lọc ở server, không lọc trên một trang ở máy (sai khi có phân trang).
  - "Sắp hết" = `min_stock > 0` và tổng tồn < `min_stock`, gồm cả tồn âm; "Tồn âm" là tổng tồn mọi vị trí < 0 (âm ở kệ nhưng kho sau còn thì không âm).
  - Cảnh báo hạn dùng tính ở server (một quy tắc, một số ngày cảnh báo của cửa hàng) thay vì ở máy.
  - Trang chi tiết: ba khối tóm tắt một hàng, hai bảng dùng hết chiều ngang; cột phụ ẩn dần (Ngày nhập, Vị trí từ 1280px; Lô, Người làm của thẻ kho từ 1536px).
- **Impeccable critique + audit** (đánh giá trên code và ảnh chụp ở 412 / 1024 / 1912px): critique lần đầu thấy hai bảng trang chi tiết phải cuộn ngang ở mọi cỡ máy tính, 4 tab tràn trên điện thoại, nhãn cảnh báo kéo dài hết thẻ. Đã sửa cả ba. Audit: A11y 3, Performance 4, Theming 3, Responsive 3, Integrity 4 = 17/20.
- **Lỗi phát hiện khi làm:**
  1. `sed` thêm `minStock` khớp cả `ProductResponse` (đã có trường này), tạo dòng trùng: xóa dòng thừa.
  2. `rowHref` của `DataTable` chỉ nhận `string`; thẻ kho có dòng không có chứng từ mở được: nới kiểu thành `string | undefined`.
  3. Heredoc của Git Bash nuốt `\` trong regex của script test: dùng `pathToFileURL`.
- **Hạn chế:** chuyển kho, xuất hủy, kiểm kê chưa có màn hình nên dòng thẻ kho của các chứng từ đó không mở được; chưa lọc thẻ kho theo vị trí / khoảng ngày trên giao diện (API có); chưa quy đổi tồn ra thùng / lốc.
- **Test:** backend build 0 cảnh báo; `npm run build` + `npm run lint` sạch. Database tạm `FoMart_StockTest` (đã xóa):
  - 20/20 kịch bản HTTP qua proxy Vite + 2 kịch bản tồn âm thật (bán nước mắm khi tồn 0 → −2, có trong "Tồn âm" và "Sắp hết").
  - Đã kiểm: lọc sắp hết / cần kiểm kê / bộ lọc sai 400; lô còn 3 ngày → `near_expiry`, hết hạn hôm qua → `expired`, lô xa hạn không cảnh báo; nhân viên không thấy giá vốn nhưng vẫn thấy cảnh báo; lô tạm −5 ở kệ; thẻ kho có `saleId` / `receiptId`, mới nhất trước, tồn sau đúng; hủy hóa đơn tạo dòng `sale_cancel`; đối soát sổ kho rỗng.
  - FEFO bỏ qua lô đã hết hạn: 2 chai bán ra trừ vào lô NEAR3, không trừ OLD1 (BR-04, BR-05).
  - Ảnh chụp danh sách, lọc "Sắp hết", chi tiết ở 412px, 1024px, 1912px.

### 28/11/2026 – Màn hình Hạn dùng (UC-08, SC-12)

Nhánh `feature/inventory-expiry-screen`. UC-08 (phần "Màn hình hạn dùng"), BR-04, BR-08, BR-11. Giai đoạn 3.

**File mới**

| Đường dẫn | Vai trò |
| --- | --- |
| `features/inventory/pages/StockExpiryPage.tsx` | `/han-dung`: hai thẻ tổng hợp (đã hết hạn / sắp hết hạn: số lô, số lượng, giá trị theo giá vốn) bấm để lọc; lọc Tất cả cần xử lý / Đã hết hạn / Sắp hết hạn; bảng (sản phẩm, lô, HSD, "Quá hạn n ngày" / "Hết hạn hôm nay" / "Còn n ngày", vị trí, tồn, giá trị) hoặc thẻ trên điện thoại; bấm dòng mở chi tiết tồn của sản phẩm |

**File sửa**

| Đường dẫn | Thay đổi |
| --- | --- |
| `backend/FoMart.Application/DTO/Inventory/StockModels.cs` | `StockSearchRequest.Expiry` + `StockExpiryFilter` (`warning`, `expired`, `near_expiry`); `StockExpirySummaryResponse` |
| `backend/FoMart.Application/Interfaces/Repositories/IStockRepository.cs`, `Infrastructure/Repositories/StockRepository.cs` | `SearchStockAsync` nhận khoảng hạn dùng `[expiryFrom, expiryBefore)`: chỉ lô còn tồn > 0, hạn sớm nhất đứng đầu; `GetExpirySummaryAsync` cộng số lô, số lượng, giá trị (làm tròn một lần qua `MoneyRules`) |
| `backend/FoMart.Application/Services/Inventory/StockService.cs` | Đổi bộ lọc thành khoảng ngày theo hôm nay (giờ VN) + `stores.expiry_warn_days`, cùng mốc với `ExpiryRules.Check`; 400 khi bộ lọc sai; `GetExpirySummaryAsync` ẩn giá trị với người bị ẩn giá vốn (BR-08) |
| `backend/FoMart.Api/Controllers/Inventory/StockController.cs`, `InventoryResponseMessageDTO.cs` | `GET /api/stock/expiry-summary`; khai báo 400 cho `GET /api/stock`; thông báo mới |
| `frontend/src/api/inventory.ts`, `features/inventory/hooks/useStock.ts` | `getExpiringLots`, `getExpirySummary`; `stockKeys.expiring`, `expirySummary`; `useExpiringLots` (`useInfiniteQuery`), `useExpirySummary` |
| `features/inventory/pages/StockListPage.tsx` | Dòng nhắc "n lô đã hết hạn, m lô sắp hết hạn" dẫn sang `/han-dung` (cách vào trang trên điện thoại) |
| `frontend/src/app/AppNav.tsx`, `router.tsx` | Mục "Hạn dùng" (icon CalendarX) chỉ ở menu bên (`sidebarOnly`); route tải lười |
| `DESIGN.md`, `CLAUDE.md` | Quy tắc màn hình phụ chỉ ở menu bên; trạng thái |

**Ghi chú**

- **Quyết định:**
  - Lọc hạn dùng ở server theo khoảng ngày, không tải hết lô rồi lọc ở máy; mốc "sắp hết hạn" = hôm nay ≤ HSD < hôm nay + N (N = `stores.expiry_warn_days`, mặc định 7), trùng quy tắc cảnh báo khi nhập hàng.
  - Lô hết hạn đúng hôm nay thuộc nhóm "sắp hết hạn" (vẫn bán được hết ngày), hiện nhãn "Hết hạn hôm nay" màu lỗi.
  - Chỉ tính lô còn tồn > 0; lô tạm không có hạn dùng nên không xuất hiện.
  - Trên điện thoại không thêm tab thứ 5 (tràn hàng): vào từ dòng nhắc ở trang Tồn kho.
  - Nút "Lập phiếu xuất hủy" làm cùng module xuất hủy (tiếp theo); hiện chỉ ghi "(sắp có)".
- **Impeccable critique + audit** (code + ảnh chụp 412 / 1024 / 1912px): trạng thái bằng chữ + màu, thẻ tổng hợp có `aria-pressed`, cột phụ không vỡ ở 1024px. Audit 17/20 (Theming 3: chưa có chế độ tối; Responsive 3: thẻ tổng hợp ghi "đơn vị" chung vì gộp nhiều sản phẩm khác đơn vị cơ sở).
- **Lỗi phát hiện khi làm:** thay chữ nhiều dòng bằng `node -e` trong `AppNav.tsx` không khớp (chỉ dòng import đổi) nên tsc báo import thừa; sửa bằng Edit. Bài học: thay đổi nhiều dòng dùng Edit.
- **Hạn chế:** chưa lập phiếu xuất hủy từ danh sách; chưa đổi số ngày cảnh báo trên giao diện (cài đặt cửa hàng, giai đoạn 4).
- **Test:** backend build 0 cảnh báo; `npm run build` + `npm run lint` sạch. Database tạm `FoMart_ExpiryTest` (đã xóa): 16/16 kịch bản HTTP qua proxy Vite:
  - 5 lô: quá hạn 3 ngày, hôm nay, còn 2 / 6 / 7 ngày → "cần xử lý" = EXP3, TODAY, D2, D6 theo thứ tự hạn; không có D7 (đúng ranh giới N = 7); "đã hết hạn" = EXP3; "sắp hết" = TODAY, D2, D6.
  - Bộ lọc sai → 400; không lọc vẫn trả mọi lô.
  - Tổng hợp 1 lô / 10 chai / 40.000đ và 3 lô / 11 chai / 44.000đ; nhân viên thấy số lô nhưng không thấy giá trị.
  - Bán 6 chai: FEFO bỏ lô quá hạn, trừ hết lô TODAY rồi 1 chai lô D2; danh sách và tổng hợp cập nhật; phân trang; đối soát sổ kho rỗng.
  - Ảnh chụp trang Hạn dùng, lọc "Đã hết hạn", dòng nhắc trang Tồn kho ở 412 / 1024 / 1912px.

### 28/11/2026 – Xuất hủy (UC-08, SC-12)

Nhánh `feature/inventory-disposal`. UC-08, BR-02, BR-03, BR-08, BR-11, BR-15, BR-21. Giai đoạn 3.

**File mới**

| Đường dẫn | Vai trò |
| --- | --- |
| `backend/FoMart.Domain/Enums/DisposalStatus.cs`, `DisposalReason.cs` | Hằng số trùng CHECK: `pending` / `approved` / `rejected`; `expired` / `broken` / `damaged` / `other` |
| `backend/FoMart.Domain/Rules/DisposalAllocator.cs` | Chọn lô khi hủy: đúng lô đã chọn, hoặc hạn dùng sớm nhất trước (lô hết hạn tự đứng đầu), lô không hạn sau cùng, rồi FIFO; **không cho tồn âm** (thiếu thì trả `Available`); `LossTotal` theo giá vốn |
| `backend/FoMart.Application/DTO/Inventory/DisposalModels.cs` | Request / response phiếu hủy, dòng, lô đã trừ, từ chối |
| `backend/FoMart.Application/Interfaces/Repositories/IDisposalRepository.cs`, `Infrastructure/Repositories/DisposalRepository.cs` | Tìm phiếu (tổn thất cộng từ sổ kho), lấy phiếu kèm dòng, sổ kho của phiếu |
| `backend/FoMart.Application/Services/Inventory/DisposalService.cs` | Lập (chủ tiệm duyệt ngay, nhân viên chờ), duyệt, từ chối; trừ tồn trong một transaction: khóa sản phẩm, khóa dòng tồn theo lô, chia lô, ghi `stock_moves` loại `disposal` |
| `backend/FoMart.Api/Controllers/Inventory/DisposalController.cs` | `GET /api/disposals`, `GET /{id}`, `POST`, `POST /{id}/approve` và `/{id}/reject` (chỉ chủ tiệm) |
| `frontend/src/features/inventory/hooks/useDisposals.ts` | `disposalKeys`; danh sách (`useInfiniteQuery`), số phiếu chờ duyệt, chi tiết; lập / duyệt / từ chối làm mới phiếu, tồn, sản phẩm |
| `features/inventory/schemas/disposalSchema.ts` | Schema dòng đang soạn, dòng đã thêm, cả phiếu (ít nhất 1 dòng), lý do từ chối |
| `features/inventory/components/DisposalLineEditor.tsx` | Soạn một dòng: vị trí, lô (hoặc để hệ thống chọn), đơn vị + số lượng (quy ra đơn vị cơ sở), lý do, ghi chú; hiện số còn trong hệ thống, báo trước khi không đủ |
| `features/inventory/components/DisposalStatusBadge.tsx` | Nhãn chữ Chờ duyệt / Đã hủy / Từ chối |
| `features/inventory/pages/DisposalListPage.tsx` | `/xuat-huy`: lọc trạng thái, bảng / thẻ, "Lập phiếu hủy" |
| `features/inventory/pages/DisposalCreatePage.tsx` | `/xuat-huy/moi`: quét / tìm hàng, nhiều dòng (`useFieldArray`), điền sẵn từ trang Hạn dùng qua `?productId=&locationId=&lotId=&qty=&reason=`; nút "Lưu và trừ tồn" (chủ tiệm) / "Gửi chủ tiệm duyệt" (nhân viên) |
| `features/inventory/pages/DisposalDetailPage.tsx` | `/xuat-huy/:id`: tổn thất, người lập / duyệt, các dòng và lô đã trừ; chủ tiệm duyệt hoặc từ chối kèm lý do |

**File sửa**

| Đường dẫn | Thay đổi |
| --- | --- |
| `IUnitOfWork.cs`, `UnitOfWork.cs`, `Program.cs` | `DisposalRepository`, `DisposalService` |
| `InventoryResponseMessageDTO.cs` | Thông báo xuất hủy (có số còn khi thiếu hàng) |
| `frontend/src/api/inventory.ts`, `lib/format.ts` | Kiểu + hàm gọi `api/disposals`; `formatDisposalReason` |
| `components/ui/DataTable.tsx` | `cardAction`: nút đặt cạnh thẻ trên điện thoại (không lồng liên kết trong liên kết) |
| `features/inventory/pages/StockExpiryPage.tsx` | Cột / nút "Hủy" mỗi lô mở form điền sẵn (lý do "Hết hạn" nếu đã quá hạn); link "Phiếu xuất hủy · n chờ duyệt" |
| `features/inventory/pages/StockListPage.tsx` | Link "Hạn dùng", "Xuất hủy · n chờ duyệt" cạnh tiêu đề (cách vào trên điện thoại) |
| `frontend/src/app/AppNav.tsx`, `router.tsx` | Mục "Xuất hủy" ở menu bên; 3 route tải lười |
| `CLAUDE.md` | Trạng thái |

**Ghi chú**

- **Quyết định:**
  - **Hủy không cho tồn âm** (khác bán hàng BR-01, giống chuyển kho): thiếu thì 409 kèm số còn, gợi ý kiểm kê trước. Lập phiếu của chủ tiệm mà thiếu thì không lưu gì (rollback); phiếu chờ mà lúc duyệt đã thiếu thì vẫn giữ trạng thái chờ.
  - **Không chọn lô** thì lấy hạn sớm nhất (lô đã hết hạn tự đứng trước); chọn lô (từ trang Hạn dùng) thì chỉ trừ đúng lô đó.
  - **Nhập theo đơn vị bất kỳ** (chai, thùng), gửi API theo đơn vị cơ sở.
  - **Hai chủ tiệm duyệt cùng lúc:** khóa sản phẩm + `row_version` của phiếu → người sau nhận 409.
  - **Tổn thất** = số lượng × giá vốn lô, làm tròn một lần về đồng (`MoneyRules`); ẩn với nhân viên khi tiệm ẩn giá vốn (BR-08).
  - Phiếu đã duyệt / từ chối không sửa (BR-03); chưa có "hủy phiếu hủy" (đảo ngược) vì chưa cần.
- **Lỗi phát hiện khi làm:**
  1. Đọc lại service: lúc lập, entity mới chưa nạp sản phẩm / vị trí nên thông báo thiếu hàng sẽ lỗi null; lúc duyệt thiếu tên người duyệt. Sửa: nạp lại phiếu trước khi trừ tồn và sau khi commit.
  2. Form soạn dòng là `<form>` lồng trong form của phiếu (HTML không cho lồng): tách, chỉ cột phải là form của phiếu.
  3. Ô chọn lô điền sẵn hiện "Để hệ thống chọn" vì danh sách lô tải sau khi ô đã vẽ (giá trị gửi đi vẫn đúng): chuyển sang điều khiển bằng `value`.
  4. Lệnh `node -e` có dấu backtick bị shell hiểu sai: sửa bằng Edit.
  5. **Lỗi có từ trước, chưa sửa trong nhánh này:** thời điểm đọc từ DB (`DATETIME2` UTC) được trả JSON không có `Z`, trình duyệt hiểu là giờ máy nên hiện sớm 7 tiếng (thấy rõ ở phiếu: "Lập 11:07", "Duyệt 18:07" cùng một lúc vì `DecidedAt` gán trong code còn mang `Kind = Utc`). Ảnh hưởng mọi màn hình; sửa ở nhánh `fix/*` tiếp theo.
- **Hạn chế:** chưa có màn hình tổng tổn thất theo kỳ (báo cáo UC-10, giai đoạn 4); chưa đính ảnh hàng bể.
- **Test:** backend build 0 cảnh báo; `npm run build` + `npm run lint` sạch. Database tạm `FoMart_DisposalTest` (đã xóa):
  - 31/31 kịch bản HTTP qua proxy Vite: chủ tiệm lập → duyệt ngay, tổn thất 16.000đ, lô EXP còn 6; nhân viên lập → chờ, tồn không đổi, không thấy giá vốn; nhân viên tự duyệt 403; chủ tiệm duyệt → tổn thất 24.000đ; duyệt / từ chối lần 2 → 409; từ chối giữ tồn; không chọn lô → trừ lô hạn sớm nhất; hủy 50 khi còn 9 → 409 "chỉ còn 9", hai dòng cộng lại vượt tồn → 409 ở dòng 2, không lưu phiếu; phiếu chờ mà tồn bị bán bớt → duyệt 409, vẫn chờ; lý do sai / lô không thuộc sản phẩm / không có dòng / lọc sai → 400; thẻ kho có dòng `XH…`; đối soát sổ kho rỗng.
  - Thao tác giao diện một lần bằng Edge (Hạn dùng → Hủy → lưu; nhân viên lập trên điện thoại → gửi duyệt; chủ tiệm từ chối; duyệt khi thiếu hàng hiện thông báo); sau đó theo yêu cầu người dùng chỉ chạy kiểm tra giao diện khi được yêu cầu.
