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
